using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace PAS.API.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Public methods
    // ─────────────────────────────────────────────────────────────────────

    public async Task SendOtpEmailAsync(string toEmail, string toName, string otpCode)
    {
        await SendAsync(toEmail, toName,
            subject: "Your Password Reset OTP — PAS",
            html:    BuildOtpEmailHtml(toName, otpCode));
    }

    public async Task SendWelcomeEmailAsync(string toEmail, string toName, string generatedPassword)
    {
        await SendAsync(toEmail, toName,
            subject: "Welcome to PAS — Your Account Details",
            html:    BuildCredentialEmailHtml(
                        toName,
                        toEmail,
                        generatedPassword,
                        isWelcome: true));
    }

    public async Task SendAdminPasswordResetEmailAsync(string toEmail, string toName, string newPassword)
    {
        await SendAsync(toEmail, toName,
            subject: "Your PAS Password Has Been Reset",
            html:    BuildCredentialEmailHtml(
                        toName,
                        toEmail,
                        newPassword,
                        isWelcome: false));
    }

    // ─────────────────────────────────────────────────────────────────────
    // Shared SMTP sender
    // ─────────────────────────────────────────────────────────────────────

    private async Task SendAsync(string toEmail, string toName, string subject, string html)
    {
        var smtp     = _config.GetSection("SmtpSettings");
        var host     = smtp["Host"]!;
        var port     = int.Parse(smtp["Port"] ?? "587");
        var username = smtp["Username"]!;
        var password = smtp["Password"]!;
        var fromName = smtp["FromName"] ?? "PAS System";
        var fromAddr = smtp["FromAddress"] ?? username;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromAddr));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = subject;
        message.Body    = new TextPart("html") { Text = html };

        using var client = new SmtpClient();
        await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(username, password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    // ─────────────────────────────────────────────────────────────────────
    // HTML Templates
    // ─────────────────────────────────────────────────────────────────────

    private static string BuildOtpEmailHtml(string name, string otp)
    {
        var digits = string.Join("", otp.Select(c =>
            $"<span style=\"display:inline-block;width:44px;height:52px;line-height:52px;" +
            $"text-align:center;font-size:28px;font-weight:700;color:#1e1b4b;" +
            $"background:#f0eeff;border-radius:10px;margin:0 4px;\">{c}</span>"
        ));

        return $"""
        <!DOCTYPE html>
        <html lang="en">
        <head><meta charset="UTF-8"/><title>OTP</title></head>
        <body style="margin:0;padding:0;background:#f4f4f9;font-family:'Segoe UI',Arial,sans-serif;">
          <table width="100%" cellpadding="0" cellspacing="0" style="background:#f4f4f9;padding:40px 0;">
            <tr><td align="center">
              <table width="560" cellpadding="0" cellspacing="0"
                     style="background:#fff;border-radius:16px;box-shadow:0 4px 24px rgba(0,0,0,0.08);overflow:hidden;">
                <tr>
                  <td style="background:linear-gradient(135deg,#4f46e5 0%,#7c3aed 100%);padding:36px 40px;text-align:center;">
                    <h1 style="margin:0;font-size:24px;font-weight:700;color:#fff;">Project Approval System</h1>
                    <p style="margin:6px 0 0;font-size:13px;color:#c4b5fd;">Password Reset Request</p>
                  </td>
                </tr>
                <tr>
                  <td style="padding:40px 48px 32px;">
                    <p style="margin:0 0 8px;font-size:16px;color:#374151;font-weight:600;">Hi {System.Web.HttpUtility.HtmlEncode(name)},</p>
                    <p style="margin:0 0 28px;font-size:15px;color:#6b7280;line-height:1.6;">
                      Use the OTP below to reset your password. It expires in
                      <strong style="color:#4f46e5;">10 minutes</strong>.
                    </p>
                    <div style="text-align:center;margin:0 0 32px;">
                      <p style="margin:0 0 12px;font-size:13px;color:#9ca3af;text-transform:uppercase;letter-spacing:1px;font-weight:600;">Your OTP Code</p>
                      <div style="display:inline-block;background:#f5f3ff;border-radius:14px;padding:18px 24px;">{digits}</div>
                    </div>
                    <div style="background:#fff7ed;border-left:4px solid #f97316;border-radius:8px;padding:14px 18px;margin-bottom:28px;">
                      <p style="margin:0;font-size:13.5px;color:#92400e;">⚠️ If you did not request this, please ignore this email.</p>
                    </div>
                    <p style="margin:0;font-size:14px;color:#9ca3af;">Do not share this OTP with anyone.</p>
                  </td>
                </tr>
                <tr>
                  <td style="background:#f9fafb;padding:20px 48px;border-top:1px solid #e5e7eb;">
                    <p style="margin:0;font-size:12px;color:#9ca3af;text-align:center;">
                      © {DateTime.UtcNow.Year} Project Approval System &nbsp;·&nbsp; Do not reply to this email.
                    </p>
                  </td>
                </tr>
              </table>
            </td></tr>
          </table>
        </body>
        </html>
        """;
    }

    private static string BuildCredentialEmailHtml(
        string name, string email, string password, bool isWelcome)
    {
        var headerText  = isWelcome ? "Welcome to PAS" : "Password Reset by Administrator";
        var headerSub   = isWelcome ? "Your account has been created" : "Your password has been updated";
        var intro       = isWelcome
            ? $"Hi <strong>{System.Web.HttpUtility.HtmlEncode(name)}</strong>, your student account has been created on the <strong>Project Approval System</strong>. Below are your login credentials."
            : $"Hi <strong>{System.Web.HttpUtility.HtmlEncode(name)}</strong>, an administrator has reset your password. Use the temporary credentials below to log in.";
        var actionNote  = isWelcome
            ? "We recommend changing your password after your first login."
            : "Please change your password as soon as possible.";

        return $"""
        <!DOCTYPE html>
        <html lang="en">
        <head><meta charset="UTF-8"/><title>{headerText}</title></head>
        <body style="margin:0;padding:0;background:#f4f4f9;font-family:'Segoe UI',Arial,sans-serif;">
          <table width="100%" cellpadding="0" cellspacing="0" style="background:#f4f4f9;padding:40px 0;">
            <tr><td align="center">
              <table width="560" cellpadding="0" cellspacing="0"
                     style="background:#fff;border-radius:16px;box-shadow:0 4px 24px rgba(0,0,0,0.08);overflow:hidden;">

                <!-- Header -->
                <tr>
                  <td style="background:linear-gradient(135deg,#0f766e 0%,#0d9488 100%);padding:36px 40px;text-align:center;">
                    <h1 style="margin:0;font-size:24px;font-weight:700;color:#fff;">Project Approval System</h1>
                    <p style="margin:6px 0 0;font-size:13px;color:#99f6e4;">{headerSub}</p>
                  </td>
                </tr>

                <!-- Body -->
                <tr>
                  <td style="padding:40px 48px 32px;">
                    <p style="margin:0 0 24px;font-size:15px;color:#374151;line-height:1.7;">{intro}</p>

                    <!-- Credentials card -->
                    <table width="100%" cellpadding="0" cellspacing="0"
                           style="background:#f8fafc;border:1px solid #e2e8f0;border-radius:12px;
                                  margin:0 0 28px;overflow:hidden;">
                      <tr>
                        <td style="padding:16px 24px;border-bottom:1px solid #e2e8f0;">
                          <p style="margin:0;font-size:12px;color:#94a3b8;text-transform:uppercase;
                                    letter-spacing:0.8px;font-weight:600;">Email</p>
                          <p style="margin:4px 0 0;font-size:15px;color:#1e293b;font-weight:500;">
                            {System.Web.HttpUtility.HtmlEncode(email)}
                          </p>
                        </td>
                      </tr>
                      <tr>
                        <td style="padding:16px 24px;">
                          <p style="margin:0;font-size:12px;color:#94a3b8;text-transform:uppercase;
                                    letter-spacing:0.8px;font-weight:600;">Temporary Password</p>
                          <p style="margin:4px 0 0;font-size:18px;color:#0f766e;font-weight:700;
                                    font-family:'Courier New',monospace;letter-spacing:1px;">
                            {System.Web.HttpUtility.HtmlEncode(password)}
                          </p>
                        </td>
                      </tr>
                    </table>

                    <!-- Action note -->
                    <div style="background:#f0fdf4;border-left:4px solid #22c55e;border-radius:8px;
                                padding:14px 18px;margin-bottom:28px;">
                      <p style="margin:0;font-size:13.5px;color:#166534;">✅ {actionNote}</p>
                    </div>

                    <p style="margin:0;font-size:14px;color:#9ca3af;line-height:1.6;">
                      Keep this email secure. Do not share your password with anyone.
                    </p>
                  </td>
                </tr>

                <!-- Footer -->
                <tr>
                  <td style="background:#f9fafb;padding:20px 48px;border-top:1px solid #e5e7eb;">
                    <p style="margin:0;font-size:12px;color:#9ca3af;text-align:center;">
                      © {DateTime.UtcNow.Year} Project Approval System &nbsp;·&nbsp; Do not reply to this email.
                    </p>
                  </td>
                </tr>

              </table>
            </td></tr>
          </table>
        </body>
        </html>
        """;
    }
}
