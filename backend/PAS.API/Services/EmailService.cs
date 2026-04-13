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

    public async Task SendOtpEmailAsync(string toEmail, string toName, string otpCode)
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
        message.Subject = "Your Password Reset OTP — PAS";

        message.Body = new TextPart("html")
        {
            Text = BuildOtpEmailHtml(toName, otpCode)
        };

        using var client = new SmtpClient();
        await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(username, password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    // ─── HTML Email Template ───────────────────────────────────────────────
    private static string BuildOtpEmailHtml(string name, string otp)
    {
        // Split OTP into individual characters for the styled digit boxes
        var digits = string.Join("", otp.Select(c =>
            $"<span style=\"display:inline-block;width:44px;height:52px;line-height:52px;" +
            $"text-align:center;font-size:28px;font-weight:700;color:#1e1b4b;" +
            $"background:#f0eeff;border-radius:10px;margin:0 4px;letter-spacing:0;\">{c}</span>"
        ));

        return $"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="UTF-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1.0" />
          <title>Password Reset OTP</title>
        </head>
        <body style="margin:0;padding:0;background-color:#f4f4f9;font-family:'Segoe UI',Arial,sans-serif;">
          <table width="100%" cellpadding="0" cellspacing="0"
                 style="background:#f4f4f9;padding:40px 0;">
            <tr>
              <td align="center">
                <!-- Card -->
                <table width="560" cellpadding="0" cellspacing="0"
                       style="background:#ffffff;border-radius:16px;
                              box-shadow:0 4px 24px rgba(0,0,0,0.08);overflow:hidden;">

                  <!-- Header -->
                  <tr>
                    <td style="background:linear-gradient(135deg,#4f46e5 0%,#7c3aed 100%);
                               padding:36px 40px;text-align:center;">
                      <h1 style="margin:0;font-size:24px;font-weight:700;color:#ffffff;
                                 letter-spacing:-0.5px;">
                        Project Approval System
                      </h1>
                      <p style="margin:6px 0 0;font-size:13px;color:#c4b5fd;">
                        Password Reset Request
                      </p>
                    </td>
                  </tr>

                  <!-- Body -->
                  <tr>
                    <td style="padding:40px 48px 32px;">
                      <p style="margin:0 0 8px;font-size:16px;color:#374151;font-weight:600;">
                        Hi {System.Web.HttpUtility.HtmlEncode(name)},
                      </p>
                      <p style="margin:0 0 28px;font-size:15px;color:#6b7280;line-height:1.6;">
                        We received a request to reset your password. Use the one-time
                        password (OTP) below to proceed. It is valid for
                        <strong style="color:#4f46e5;">10 minutes</strong>.
                      </p>

                      <!-- OTP box -->
                      <div style="text-align:center;margin:0 0 32px;">
                        <p style="margin:0 0 12px;font-size:13px;color:#9ca3af;
                                  text-transform:uppercase;letter-spacing:1px;font-weight:600;">
                          Your OTP Code
                        </p>
                        <div style="display:inline-block;background:#f5f3ff;
                                    border-radius:14px;padding:18px 24px;">
                          {digits}
                        </div>
                      </div>

                      <div style="background:#fff7ed;border-left:4px solid #f97316;
                                  border-radius:8px;padding:14px 18px;margin-bottom:28px;">
                        <p style="margin:0;font-size:13.5px;color:#92400e;line-height:1.5;">
                          ⚠️ If you did not request a password reset, please ignore this
                          email. Your account is safe.
                        </p>
                      </div>

                      <p style="margin:0;font-size:14px;color:#9ca3af;line-height:1.6;">
                        For security reasons, do not share this OTP with anyone.
                      </p>
                    </td>
                  </tr>

                  <!-- Footer -->
                  <tr>
                    <td style="background:#f9fafb;padding:20px 48px;
                               border-top:1px solid #e5e7eb;">
                      <p style="margin:0;font-size:12px;color:#9ca3af;text-align:center;">
                        © {DateTime.UtcNow.Year} Project Approval System &nbsp;·&nbsp;
                        This is an automated email — please do not reply.
                      </p>
                    </td>
                  </tr>

                </table>
              </td>
            </tr>
          </table>
        </body>
        </html>
        """;
    }
}
