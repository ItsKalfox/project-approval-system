using Xunit;
using PAS.API.Data;
using PAS.API.DTOs.Auth;
using PAS.API.Models;
using PAS.API.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;

namespace PAS.API.Tests;

public class AuthServiceTests : IAsyncLifetime
{
    private readonly DbContextOptions<PASDbContext> _dbOptions;
    private IConfiguration _config = null!;

    public AuthServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<PASDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        // Setup test configuration with valid JWT settings
        var inMemorySettings = new Dictionary<string, string>
        {
            ["JwtSettings:SecretKey"] = "TestSecretKeyWithAtLeast32CharactersForTesting",
            ["JwtSettings:Issuer"] = "TestIssuer",
            ["JwtSettings:Audience"] = "TestAudience",
            ["JwtSettings:ExpiryMinutes"] = "60"
        };
        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    private AuthService CreateService(PASDbContext context)
    {
        return new AuthService(context, _config);
    }

    #region LoginAsync Tests

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsToken()
    {
        using var context = new PASDbContext(_dbOptions);
        
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword("password123");
        var user = new User
        {
            UserId = 1,
            Name = "Test User",
            Email = "test@example.com",
            Password = hashedPassword,
            Role = "STUDENT"
        };
        var student = new Student { UserId = 1, Batch = "2024", User = user };
        
        context.Users.Add(user);
        context.Students.Add(student);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var dto = new LoginRequestDto
        {
            Email = "test@example.com",
            Password = "password123"
        };

        var result = await service.LoginAsync(dto);

        Assert.NotNull(result);
        Assert.Equal("Test User", result.Name);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal("STUDENT", result.Role);
        Assert.Equal("2024", result.Batch);
        Assert.False(string.IsNullOrEmpty(result.Token));
        Assert.Equal("Bearer", result.TokenType);
        Assert.True(result.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task LoginAsync_WithSupervisorRole_ReturnsTokenWithoutBatch()
    {
        using var context = new PASDbContext(_dbOptions);
        
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword("password123");
        var user = new User
        {
            UserId = 1,
            Name = "Supervisor",
            Email = "sup@example.com",
            Password = hashedPassword,
            Role = "SUPERVISOR"
        };
        var supervisor = new Supervisor { UserId = 1, User = user };
        
        context.Users.Add(user);
        context.Supervisors.Add(supervisor);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var dto = new LoginRequestDto
        {
            Email = "sup@example.com",
            Password = "password123"
        };

        var result = await service.LoginAsync(dto);

        Assert.NotNull(result);
        Assert.Equal("SUPERVISOR", result.Role);
        Assert.Null(result.Batch); // Supervisor has no batch
    }

    [Fact]
    public async Task LoginAsync_WithInvalidEmail_ThrowsUnauthorizedAccessException()
    {
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);
        var dto = new LoginRequestDto
        {
            Email = "nonexistent@example.com",
            Password = "password123"
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.LoginAsync(dto));
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ThrowsUnauthorizedAccessException()
    {
        using var context = new PASDbContext(_dbOptions);
        
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword("correctpassword");
        var user = new User
        {
            UserId = 1,
            Name = "Test",
            Email = "test@example.com",
            Password = hashedPassword,
            Role = "STUDENT"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var dto = new LoginRequestDto
        {
            Email = "test@example.com",
            Password = "wrongpassword"
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.LoginAsync(dto));
    }

    [Fact]
    public async Task LoginAsync_WithEmptyEmail_ThrowsArgumentException()
    {
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);
        var dto = new LoginRequestDto
        {
            Email = "",
            Password = "password123"
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.LoginAsync(dto));
    }

    [Fact]
    public async Task LoginAsync_WithEmptyPassword_ThrowsArgumentException()
    {
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);
        var dto = new LoginRequestDto
        {
            Email = "test@example.com",
            Password = ""
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.LoginAsync(dto));
    }

    [Fact]
    public async Task LoginAsync_WithNullEmail_ThrowsArgumentException()
    {
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);
        var dto = new LoginRequestDto
        {
            Email = null!,
            Password = "password123"
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.LoginAsync(dto));
    }

    [Fact]
    public async Task LoginAsync_WithNullPassword_ThrowsArgumentException()
    {
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);
        var dto = new LoginRequestDto
        {
            Email = "test@example.com",
            Password = null!
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.LoginAsync(dto));
    }

    [Fact]
    public async Task LoginAsync_EmailCaseInsensitive()
    {
        using var context = new PASDbContext(_dbOptions);
        
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword("password123");
        var user = new User
        {
            UserId = 1,
            Name = "Test",
            Email = "test@example.com", // Stored lowercase (normalized)
            Password = hashedPassword,
            Role = "STUDENT"
        };
        var student = new Student { UserId = 1, Batch = "2024", User = user };
        
        context.Users.Add(user);
        context.Students.Add(student);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var dto = new LoginRequestDto
        {
            Email = "TEST@EXAMPLE.COM", // Uppercase input
            Password = "password123"
        };

        var result = await service.LoginAsync(dto);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task LoginAsync_WithAdminRole_ReturnsToken()
    {
        using var context = new PASDbContext(_dbOptions);
        
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword("admin123");
        var user = new User
        {
            UserId = 1,
            Name = "Admin",
            Email = "admin@example.com",
            Password = hashedPassword,
            Role = "ADMIN"
        };
        
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var dto = new LoginRequestDto
        {
            Email = "admin@example.com",
            Password = "admin123"
        };

        var result = await service.LoginAsync(dto);

        Assert.NotNull(result);
        Assert.Equal("ADMIN", result.Role);
        Assert.Null(result.Batch);
    }

    [Fact]
    public async Task LoginAsync_WithModuleLeaderRole_ReturnsToken()
    {
        using var context = new PASDbContext(_dbOptions);
        
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword("ml123");
        var user = new User
        {
            UserId = 1,
            Name = "Module Leader",
            Email = "ml@example.com",
            Password = hashedPassword,
            Role = "MODULE LEADER"
        };
        
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var dto = new LoginRequestDto
        {
            Email = "ml@example.com",
            Password = "ml123"
        };

        var result = await service.LoginAsync(dto);

        Assert.NotNull(result);
        Assert.Equal("MODULE LEADER", result.Role);
    }

    #endregion
}
