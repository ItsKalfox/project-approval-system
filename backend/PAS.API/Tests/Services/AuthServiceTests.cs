using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using PAS.API.Data;
using PAS.API.DTOs.Auth;
using PAS.API.Models;
using PAS.API.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xunit;

namespace PAS.API.Tests.Services;

public class AuthServiceTests
{
    private readonly PASDbContext _dbContext;
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<PASDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _dbContext = new PASDbContext(options);
        _mockConfig = new Mock<IConfiguration>();
        
        var jwtSection = new Mock<IConfigurationSection>();
        jwtSection.Setup(x => x["SecretKey"]).Returns("TestSecretKey12345678901234567890");
        jwtSection.Setup(x => x["Issuer"]).Returns("TestIssuer");
        jwtSection.Setup(x => x["Audience"]).Returns("TestAudience");
        jwtSection.Setup(x => x["ExpiryMinutes"]).Returns("60");
        _mockConfig.Setup(x => x.GetSection("JwtSettings")).Returns(jwtSection.Object);

        _authService = new AuthService(_dbContext, _mockConfig.Object);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsTokenAndUserInfo()
    {
        var user = new User
        {
            UserId = 1,
            Name = "Test User",
            Email = "test@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword("password123"),
            Role = "STUDENT",
            Student = new Student { Batch = "2024" }
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var dto = new LoginRequestDto { Email = "test@example.com", Password = "password123" };

        var result = await _authService.LoginAsync(dto);

        Assert.NotNull(result);
        Assert.NotNull(result.Token);
        Assert.Equal("Bearer", result.TokenType);
        Assert.Equal(user.UserId, result.UserId);
        Assert.Equal("Test User", result.Name);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal("STUDENT", result.Role);
    }

    [Fact]
    public async Task LoginAsync_InvalidEmail_ThrowsUnauthorizedAccessException()
    {
        var user = new User
        {
            UserId = 1,
            Name = "Test User",
            Email = "test@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword("password123"),
            Role = "STUDENT"
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var dto = new LoginRequestDto { Email = "wrong@example.com", Password = "password123" };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _authService.LoginAsync(dto));
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ThrowsUnauthorizedAccessException()
    {
        var user = new User
        {
            UserId = 1,
            Name = "Test User",
            Email = "test@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword("password123"),
            Role = "STUDENT"
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var dto = new LoginRequestDto { Email = "test@example.com", Password = "wrongpassword" };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _authService.LoginAsync(dto));
    }

    [Fact]
    public async Task LoginAsync_EmptyEmail_ThrowsArgumentException()
    {
        var dto = new LoginRequestDto { Email = "", Password = "password123" };

        await Assert.ThrowsAsync<ArgumentException>(() => _authService.LoginAsync(dto));
    }

    [Fact]
    public async Task LoginAsync_EmptyPassword_ThrowsArgumentException()
    {
        var dto = new LoginRequestDto { Email = "test@example.com", Password = "" };

        await Assert.ThrowsAsync<ArgumentException>(() => _authService.LoginAsync(dto));
    }

    [Fact]
    public async Task LoginAsync_StudentWithBatch_IncludesBatchInToken()
    {
        var user = new User
        {
            UserId = 1,
            Name = "Student User",
            Email = "student@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword("password123"),
            Role = "STUDENT",
            Student = new Student { Batch = "2024" }
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var dto = new LoginRequestDto { Email = "student@example.com", Password = "password123" };

        var result = await _authService.LoginAsync(dto);

        Assert.Equal("2024", result.Batch);
    }

    [Fact]
    public async Task LoginAsync_NonStudentRole_DoesNotIncludeBatch()
    {
        var user = new User
        {
            UserId = 1,
            Name = "Supervisor User",
            Email = "supervisor@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword("password123"),
            Role = "SUPERVISOR"
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var dto = new LoginRequestDto { Email = "supervisor@example.com", Password = "password123" };

        var result = await _authService.LoginAsync(dto);

        Assert.Null(result.Batch);
    }

    [Fact]
    public async Task LoginAsync_ValidToken_CanBeParsed()
    {
        var user = new User
        {
            UserId = 1,
            Name = "Test User",
            Email = "test@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword("password123"),
            Role = "SUPERVISOR"
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var dto = new LoginRequestDto { Email = "test@example.com", Password = "password123" };

        var result = await _authService.LoginAsync(dto);

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.ReadJwtToken(result.Token);

        Assert.Equal(user.UserId.ToString(), token.Subject);
        Assert.Equal(user.Email, token.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal(user.Role, token.Claims.First(c => c.Type == ClaimTypes.Role).Value);
    }

    [Fact]
    public async Task LoginAsync_CaseInsensitiveEmail_MatchesUser()
    {
        var user = new User
        {
            UserId = 1,
            Name = "Test User",
            Email = "test@example.com",
            Password = BCrypt.Net.BCrypt.HashPassword("password123"),
            Role = "STUDENT"
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var dto = new LoginRequestDto { Email = "TEST@EXAMPLE.COM", Password = "password123" };

        var result = await _authService.LoginAsync(dto);

        Assert.NotNull(result);
        Assert.Equal(user.UserId, result.UserId);
    }
}