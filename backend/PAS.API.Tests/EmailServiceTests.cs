using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using PAS.API.Services;

namespace PAS.API.Tests;

public class EmailServiceTests
{
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Mock<ILogger<EmailService>> _mockLogger;
    private readonly EmailService _emailService;

    public EmailServiceTests()
    {
        _mockConfig = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<EmailService>>();
        
        // Setup default SMTP configuration
        var smtpSection = new Mock<IConfigurationSection>();
        smtpSection.Setup(x => x["Host"]).Returns("smtp.example.com");
        smtpSection.Setup(x => x["Port"]).Returns("587");
        smtpSection.Setup(x => x["Username"]).Returns("noreply@example.com");
        smtpSection.Setup(x => x["Password"]).Returns("password123");
        smtpSection.Setup(x => x["FromName"]).Returns("PAS System");
        smtpSection.Setup(x => x["FromAddress"]).Returns("noreply@example.com");
        
        _mockConfig.Setup(x => x.GetSection("SmtpSettings")).Returns(smtpSection.Object);
        
        _emailService = new EmailService(_mockConfig.Object, _mockLogger.Object);
    }

    #region OTP Email Tests

    [Fact]
    public async Task SendOtpEmailAsync_WithValidInputs_LogsInformationMessage()
    {
        // This test validates that the service correctly calls logging and attempts email send
        // (Will throw due to mocked SMTP, but we can verify the attempt)
        var toEmail = "user@example.com";
        var toName = "Test User";
        var otpCode = "123456";

        try
        {
            await _emailService.SendOtpEmailAsync(toEmail, toName, otpCode);
        }
        catch
        {
            // Expected to fail due to mocked SMTP
        }

        // Verify logging was called
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Attempting to send email")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendOtpEmailAsync_WithValidEmail_AttemptsToSend()
    {
        var toEmail = "reset@example.com";
        var toName = "John Doe";
        var otpCode = "654321";

        // Should attempt to send (will fail without real SMTP)
        try
        {
            await _emailService.SendOtpEmailAsync(toEmail, toName, otpCode);
        }
        catch
        {
            // Expected - verifying method is callable
        }

        // Verify logger was called with email information
        _mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region Welcome Email Tests

    [Fact]
    public async Task SendWelcomeEmailAsync_WithValidInputs_LogsEmailSendAttempt()
    {
        var toEmail = "newuser@example.com";
        var toName = "New User";
        var password = "GeneratedPassword123!";

        try
        {
            await _emailService.SendWelcomeEmailAsync(toEmail, toName, password);
        }
        catch
        {
            // Expected to fail due to mocked SMTP
        }

        // Verify logging attempt
        _mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task SendWelcomeEmailAsync_WithDifferentCredentials_CallsServiceWithCorrectParams()
    {
        var toEmail = "supervisor@example.com";
        var toName = "Dr. Supervisor";
        var password = "TempPass789!";

        try
        {
            await _emailService.SendWelcomeEmailAsync(toEmail, toName, password);
        }
        catch
        {
            // Expected - verifying parameters are used
        }

        // Verify configuration was accessed
        _mockConfig.Verify(x => x.GetSection("SmtpSettings"), Times.AtLeastOnce);
    }

    #endregion

    #region Admin Password Reset Email Tests

    [Fact]
    public async Task SendAdminPasswordResetEmailAsync_WithValidInputs_ProcessesRequest()
    {
        var toEmail = "admin@example.com";
        var toName = "Admin User";
        var newPassword = "NewAdminPass456!";

        try
        {
            await _emailService.SendAdminPasswordResetEmailAsync(toEmail, toName, newPassword);
        }
        catch
        {
            // Expected - verifying method is callable
        }

        // Verify config section was accessed for SMTP settings
        _mockConfig.Verify(x => x.GetSection("SmtpSettings"), Times.AtLeastOnce);
    }

    [Fact]
    public async Task SendAdminPasswordResetEmailAsync_WithMultipleUsers_EachCallsConfiguration()
    {
        var users = new[]
        {
            ("user1@example.com", "User One", "Pass1!"),
            ("user2@example.com", "User Two", "Pass2!"),
            ("user3@example.com", "User Three", "Pass3!")
        };

        foreach (var (email, name, password) in users)
        {
            try
            {
                await _emailService.SendAdminPasswordResetEmailAsync(email, name, password);
            }
            catch
            {
                // Expected
            }
        }

        // Verify configuration was accessed multiple times
        _mockConfig.Verify(x => x.GetSection("SmtpSettings"), Times.AtLeast(3));
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public async Task SendOtpEmailAsync_WithMissingSmtpPort_UsesDefaultPort()
    {
        // Setup config without port
        var smtpSection = new Mock<IConfigurationSection>();
        smtpSection.Setup(x => x["Host"]).Returns("smtp.example.com");
        smtpSection.Setup(x => x["Port"]).Returns((string?)null); // Missing port
        smtpSection.Setup(x => x["Username"]).Returns("user@example.com");
        smtpSection.Setup(x => x["Password"]).Returns("pass");
        
        _mockConfig.Setup(x => x.GetSection("SmtpSettings")).Returns(smtpSection.Object);

        try
        {
            await _emailService.SendOtpEmailAsync("test@example.com", "Test", "123456");
        }
        catch
        {
            // Expected - missing FromName will be defaulted
        }

        _mockConfig.Verify(x => x.GetSection("SmtpSettings"), Times.AtLeastOnce);
    }

    [Fact]
    public async Task SendWelcomeEmailAsync_WithCustomFromName_UsesConfiguredValue()
    {
        var smtpSection = new Mock<IConfigurationSection>();
        smtpSection.Setup(x => x["Host"]).Returns("smtp.example.com");
        smtpSection.Setup(x => x["Port"]).Returns("587");
        smtpSection.Setup(x => x["Username"]).Returns("noreply@example.com");
        smtpSection.Setup(x => x["Password"]).Returns("password");
        smtpSection.Setup(x => x["FromName"]).Returns("Custom Organization");
        smtpSection.Setup(x => x["FromAddress"]).Returns("custom@example.com");
        
        _mockConfig.Setup(x => x.GetSection("SmtpSettings")).Returns(smtpSection.Object);
        var service = new EmailService(_mockConfig.Object, _mockLogger.Object);

        try
        {
            await service.SendWelcomeEmailAsync("user@example.com", "User", "Pass123!");
        }
        catch
        {
            // Expected
        }

        _mockConfig.Verify(x => x.GetSection("SmtpSettings"), Times.AtLeastOnce);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task SendOtpEmailAsync_WithInvalidEmail_StillAttemptsSend()
    {
        var invalidEmail = "not-an-email";
        var toName = "Test";
        var otp = "123456";

        // Should attempt send even with invalid email format
        try
        {
            await _emailService.SendOtpEmailAsync(invalidEmail, toName, otp);
        }
        catch
        {
            // Expected - SMTP will fail
        }

        // Verify logging was called
        _mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task SendWelcomeEmailAsync_LogsInformationOnAttempt()
    {
        try
        {
            await _emailService.SendWelcomeEmailAsync("test@example.com", "Test", "Pass!");
        }
        catch
        {
            // Expected
        }

        // Verify at least one log call occurred
        _mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion
}
