using Moq;
using Xunit;
using PAS.API.Data;
using PAS.API.DTOs.Auth;
using PAS.API.Models;
using PAS.API.Services;
using Microsoft.EntityFrameworkCore;

namespace PAS.API.Tests;

/// <summary>
/// Unit tests for PasswordResetService covering OTP generation, validation, and password reset flows.
/// Uses in-memory database for testing.
/// </summary>
public class PasswordResetServiceTests : IAsyncLifetime
{
    private readonly DbContextOptions<PASDbContext> _dbOptions;
    private Mock<IEmailService> _mockEmailService = null!;

    public PasswordResetServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<PASDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    private PasswordResetService CreateService(PASDbContext context)
    {
        _mockEmailService = new Mock<IEmailService>();
        return new PasswordResetService(context, _mockEmailService.Object);
    }

    #region SendOtpAsync Tests

    [Fact]
    public async Task SendOtpAsync_WithValidEmail_CreatesOtpAndSendsEmail()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var user = new User 
        { 
            Email = "test@example.com", 
            Name = "Test User",
            Password = "hashed",
            Role = "STUDENT",
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var dto = new ForgotPasswordDto { Email = "test@example.com" };

        _mockEmailService
            .Setup(s => s.SendOtpEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await service.SendOtpAsync(dto);

        // Assert
        var otp = await context.PasswordResetOtps.FirstOrDefaultAsync();
        Assert.NotNull(otp);
        Assert.Equal("test@example.com", otp.Email);
        Assert.False(otp.IsUsed);
        Assert.NotEmpty(otp.OtpCode);
        Assert.Equal(6, otp.OtpCode.Length); // OTP should be 6 digits
        
        _mockEmailService.Verify(
            s => s.SendOtpEmailAsync("test@example.com", "Test User", It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task SendOtpAsync_WithNonexistentEmail_SilentlySucceeds()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);
        var dto = new ForgotPasswordDto { Email = "nonexistent@example.com" };

        // Act - should not throw
        await service.SendOtpAsync(dto);

        // Assert - no OTP created
        var otpCount = await context.PasswordResetOtps.CountAsync();
        Assert.Equal(0, otpCount);
        
        // Email service should not be called (prevents email enumeration)
        _mockEmailService.Verify(
            s => s.SendOtpEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task SendOtpAsync_WithEmptyEmail_ThrowsArgumentException()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);
        var dto = new ForgotPasswordDto { Email = "" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.SendOtpAsync(dto));
    }

    [Fact]
    public async Task SendOtpAsync_WithWhitespaceEmail_ThrowsArgumentException()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);
        var dto = new ForgotPasswordDto { Email = "   " };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.SendOtpAsync(dto));
    }

    [Fact]
    public async Task SendOtpAsync_InvalidatePreviousOtps_KeepsOnlyLatest()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var user = new User
        {
            Email = "test@example.com",
            Name = "Test User",
            Password = "hashed",
            Role = "STUDENT",
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);

        // Add multiple unused OTPs
        context.PasswordResetOtps.AddRange(
            new PasswordResetOtp { Email = "test@example.com", OtpCode = "111111", IsUsed = false, CreatedAt = DateTime.UtcNow.AddMinutes(-20) },
            new PasswordResetOtp { Email = "test@example.com", OtpCode = "222222", IsUsed = false, CreatedAt = DateTime.UtcNow.AddMinutes(-10) }
        );
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var dto = new ForgotPasswordDto { Email = "test@example.com" };

        _mockEmailService.Setup(s => s.SendOtpEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        // Act
        await service.SendOtpAsync(dto);

        // Assert - old OTPs removed, only new one exists
        var otps = await context.PasswordResetOtps.ToListAsync();
        Assert.Single(otps);
        Assert.NotEqual("111111", otps[0].OtpCode);
        Assert.NotEqual("222222", otps[0].OtpCode);
    }

    [Fact]
    public async Task SendOtpAsync_TrimsAndLowercasesEmail()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var user = new User
        {
            Email = "test@example.com",
            Name = "Test User",
            Password = "hashed",
            Role = "STUDENT",
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var dto = new ForgotPasswordDto { Email = "  TEST@EXAMPLE.COM  " };

        _mockEmailService.Setup(s => s.SendOtpEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        // Act
        await service.SendOtpAsync(dto);

        // Assert
        var otp = await context.PasswordResetOtps.FirstOrDefaultAsync();
        Assert.NotNull(otp);
        Assert.Equal("test@example.com", otp.Email);
    }

    #endregion

    #region VerifyOtpAsync Tests

    [Fact]
    public async Task VerifyOtpAsync_WithValidOtp_Succeeds()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var otp = new PasswordResetOtp
        {
            Email = "test@example.com",
            OtpCode = "123456",
            IsUsed = false,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };
        context.PasswordResetOtps.Add(otp);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var dto = new VerifyOtpDto { Email = "test@example.com", Otp = "123456" };

        // Act & Assert - should not throw
        await service.VerifyOtpAsync(dto);
    }

    [Fact]
    public async Task VerifyOtpAsync_WithExpiredOtp_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var otp = new PasswordResetOtp
        {
            Email = "test@example.com",
            OtpCode = "123456",
            IsUsed = false,
            CreatedAt = DateTime.UtcNow.AddMinutes(-15),
            ExpiresAt = DateTime.UtcNow.AddMinutes(-5) // Expired
        };
        context.PasswordResetOtps.Add(otp);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var dto = new VerifyOtpDto { Email = "test@example.com", Otp = "123456" };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.VerifyOtpAsync(dto));
    }

    [Fact]
    public async Task VerifyOtpAsync_WithInvalidOtp_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var otp = new PasswordResetOtp
        {
            Email = "test@example.com",
            OtpCode = "123456",
            IsUsed = false,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };
        context.PasswordResetOtps.Add(otp);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var dto = new VerifyOtpDto { Email = "test@example.com", Otp = "999999" };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.VerifyOtpAsync(dto));
    }

    [Fact]
    public async Task VerifyOtpAsync_WithUsedOtp_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var otp = new PasswordResetOtp
        {
            Email = "test@example.com",
            OtpCode = "123456",
            IsUsed = true, // Already used
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };
        context.PasswordResetOtps.Add(otp);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var dto = new VerifyOtpDto { Email = "test@example.com", Otp = "123456" };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.VerifyOtpAsync(dto));
    }

    [Fact]
    public async Task VerifyOtpAsync_WithEmptyEmail_ThrowsArgumentException()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);
        var dto = new VerifyOtpDto { Email = "", Otp = "123456" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.VerifyOtpAsync(dto));
    }

    [Fact]
    public async Task VerifyOtpAsync_WithEmptyOtp_ThrowsArgumentException()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);
        var dto = new VerifyOtpDto { Email = "test@example.com", Otp = "" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.VerifyOtpAsync(dto));
    }

    #endregion

    #region ResetPasswordAsync Tests

    [Fact]
    public async Task ResetPasswordAsync_WithValidData_UpdatesPasswordAndMarksOtpUsed()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var user = new User
        {
            Email = "test@example.com",
            Name = "Test User",
            Password = BCrypt.Net.BCrypt.HashPassword("oldpassword", 12),
            Role = "STUDENT",
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);

        var otp = new PasswordResetOtp
        {
            Email = "test@example.com",
            OtpCode = "123456",
            IsUsed = false,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };
        context.PasswordResetOtps.Add(otp);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var dto = new ResetPasswordDto
        {
            Email = "test@example.com",
            Otp = "123456",
            NewPassword = "newpassword123"
        };

        // Act
        await service.ResetPasswordAsync(dto);

        // Assert
        var updatedUser = await context.Users.FirstAsync(u => u.Email == "test@example.com");
        Assert.True(BCrypt.Net.BCrypt.Verify("newpassword123", updatedUser.Password));

        var usedOtp = await context.PasswordResetOtps.FirstAsync(o => o.OtpCode == "123456");
        Assert.True(usedOtp.IsUsed);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithExpiredOtp_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var user = new User
        {
            Email = "test@example.com",
            Name = "Test User",
            Password = "hashed",
            Role = "STUDENT",
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);

        var otp = new PasswordResetOtp
        {
            Email = "test@example.com",
            OtpCode = "123456",
            IsUsed = false,
            CreatedAt = DateTime.UtcNow.AddMinutes(-15),
            ExpiresAt = DateTime.UtcNow.AddMinutes(-5)
        };
        context.PasswordResetOtps.Add(otp);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var dto = new ResetPasswordDto
        {
            Email = "test@example.com",
            Otp = "123456",
            NewPassword = "newpassword123"
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ResetPasswordAsync(dto));
    }

    [Fact]
    public async Task ResetPasswordAsync_WithNonexistentUser_ThrowsKeyNotFoundException()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var otp = new PasswordResetOtp
        {
            Email = "nonexistent@example.com",
            OtpCode = "123456",
            IsUsed = false,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };
        context.PasswordResetOtps.Add(otp);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var dto = new ResetPasswordDto
        {
            Email = "nonexistent@example.com",
            Otp = "123456",
            NewPassword = "newpassword123"
        };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.ResetPasswordAsync(dto));
    }

    [Fact]
    public async Task ResetPasswordAsync_WithEmptyEmail_ThrowsArgumentException()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);
        var dto = new ResetPasswordDto { Email = "", Otp = "123456", NewPassword = "newpass" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.ResetPasswordAsync(dto));
    }

    [Fact]
    public async Task ResetPasswordAsync_WithEmptyOtp_ThrowsArgumentException()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);
        var dto = new ResetPasswordDto { Email = "test@example.com", Otp = "", NewPassword = "newpass" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.ResetPasswordAsync(dto));
    }

    [Fact]
    public async Task ResetPasswordAsync_WithEmptyPassword_ThrowsArgumentException()
    {
        // Arrange
        using var context = new PASDbContext(_dbOptions);
        var service = CreateService(context);
        var dto = new ResetPasswordDto { Email = "test@example.com", Otp = "123456", NewPassword = "" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.ResetPasswordAsync(dto));
    }

    #endregion
}
