using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace SClinic.Services;

/// <summary>Gmail SMTP email sender using MailKit (replaces deprecated System.Net.Mail).</summary>
public class EmailService(IConfiguration config, ILogger<EmailService> logger)
{
    public async Task SendOtpAsync(string toEmail, string toName, string otpCode)
    {
        var settings  = config.GetSection("EmailSettings");
        var host      = settings["SmtpHost"]    ?? "smtp.gmail.com";
        var port      = int.Parse(settings["SmtpPort"] ?? "587");
        var user      = settings["SmtpUser"]    ?? "";
        var pass      = (settings["SmtpPass"]   ?? "").Replace(" ", ""); // strip spaces from App Password
        var fromName  = settings["SenderName"]  ?? "S-Clinic";
        var fromEmail = settings["SenderEmail"] ?? user;

        var body = $"""
            <div style="font-family:Inter,Arial,sans-serif;max-width:480px;margin:0 auto;background:#f8fafc;padding:32px;border-radius:16px;">
              <div style="background:#0d9488;padding:24px 32px;border-radius:12px;text-align:center;margin-bottom:24px;">
                <h1 style="color:#fff;margin:0;font-size:24px;font-weight:900;letter-spacing:-0.5px;">S-Clinic</h1>
                <p style="color:#ccfbf1;margin:4px 0 0;font-size:13px;">Phòng khám Da liễu &amp; Thẩm mỹ</p>
              </div>
              <p style="color:#334155;font-size:15px;">Xin chào <b>{toName}</b>,</p>
              <p style="color:#334155;font-size:15px;">Mã OTP xác thực tài khoản S-Clinic của bạn là:</p>
              <div style="background:#fff;border:2px solid #0d9488;border-radius:12px;padding:24px;text-align:center;margin:24px 0;">
                <span style="font-size:42px;font-weight:900;letter-spacing:12px;color:#0d9488;">{otpCode}</span>
              </div>
              <p style="color:#64748b;font-size:13px;">⏰ Mã có hiệu lực trong <b>5 phút</b>. Không chia sẻ mã này cho bất kỳ ai.</p>
              <p style="color:#94a3b8;font-size:12px;margin-top:24px;border-top:1px solid #e2e8f0;padding-top:16px;">
                Nếu bạn không yêu cầu đăng ký, hãy bỏ qua email này.<br/>
                © 2026 S-Clinic · Phòng khám Da liễu &amp; Thẩm mỹ
              </p>
            </div>
            """;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = $"[S-Clinic] Mã OTP của bạn: {otpCode}";
        message.Body    = new TextPart("html") { Text = body };

        using var smtp = new SmtpClient();
        try
        {
            // Port 587 → STARTTLS; Port 465 → SSL
            await smtp.ConnectAsync(host, port, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(user, pass);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);

            logger.LogInformation("OTP email sent to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send OTP email to {Email}", toEmail);
            throw;
        }
    }
}
