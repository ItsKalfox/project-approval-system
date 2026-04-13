namespace PAS.API.Services;

public interface IEmailService
{
    Task SendOtpEmailAsync(string toEmail, string toName, string otpCode);
    Task SendWelcomeEmailAsync(string toEmail, string toName, string generatedPassword);
    Task SendAdminPasswordResetEmailAsync(string toEmail, string toName, string newPassword);
}
