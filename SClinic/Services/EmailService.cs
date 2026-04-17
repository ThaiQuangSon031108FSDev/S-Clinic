using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace SClinic.Services;

/// <summary>Gmail SMTP email sender using MailKit.</summary>
public class EmailService(IConfiguration config, ILogger<EmailService> logger)
{
    // ── Shared SMTP helper ─────────────────────────────────────────────────────
    private (string host, int port, string user, string pass, string fromName, string fromEmail) GetSmtp()
    {
        var s = config.GetSection("EmailSettings");
        return (
            s["SmtpHost"]    ?? "smtp.gmail.com",
            int.Parse(s["SmtpPort"] ?? "587"),
            s["SmtpUser"]    ?? "",
            (s["SmtpPass"]   ?? "").Replace(" ", ""),
            s["SenderName"]  ?? "S-Clinic",
            s["SenderEmail"] ?? s["SmtpUser"] ?? ""
        );
    }

    private async Task SendAsync(MimeMessage message)
    {
        var (host, port, user, pass, _, _) = GetSmtp();
        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(host, port, SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(user, pass);
        await smtp.SendAsync(message);
        await smtp.DisconnectAsync(true);
    }

    // ── OTP Email ──────────────────────────────────────────────────────────────
    public async Task SendOtpAsync(string toEmail, string toName, string otpCode)
    {
        var (_, _, _, _, fromName, fromEmail) = GetSmtp();

        var body = $"""
            <div style="font-family:Inter,Arial,sans-serif;max-width:480px;margin:0 auto;background:#f8fafc;padding:32px;border-radius:16px;">
              <div style="background:#0d9488;padding:24px 32px;border-radius:12px;text-align:center;margin-bottom:24px;">
                <h1 style="color:#fff;margin:0;font-size:24px;font-weight:900;">S-Clinic</h1>
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
                © 2026 S-Clinic
              </p>
            </div>
            """;

        var msg = new MimeMessage();
        msg.From.Add(new MailboxAddress(fromName, fromEmail));
        msg.To.Add(new MailboxAddress(toName, toEmail));
        msg.Subject = $"[S-Clinic] Mã OTP của bạn: {otpCode}";
        msg.Body    = new TextPart("html") { Text = body };

        try
        {
            await SendAsync(msg);
            logger.LogInformation("OTP email sent to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send OTP email to {Email}", toEmail);
            throw;
        }
    }

    // ── Welcome Staff Email ────────────────────────────────────────────────────
    /// <summary>Gửi email chào mừng nhân sự mới kèm link đặt mật khẩu (hiệu lực 24h).</summary>
    public async Task SendWelcomeStaffAsync(string toEmail, string staffName, string role, string setPasswordUrl)
    {
        var (_, _, _, _, fromName, fromEmail) = GetSmtp();

        var roleVi = role switch {
            "Doctor"       => "Bác sĩ",
            "Receptionist" => "Lễ tân",
            "Cashier"      => "Thu ngân",
            _              => role
        };

        var body = $"""
            <div style="font-family:Inter,Arial,sans-serif;max-width:520px;margin:0 auto;background:#f8fafc;padding:32px;border-radius:16px;">
              <div style="background:linear-gradient(135deg,#0d9488,#0f766e);padding:28px 32px;border-radius:12px;text-align:center;margin-bottom:28px;">
                <h1 style="color:#fff;margin:0;font-size:26px;font-weight:900;">S-CLINIC</h1>
                <p style="color:#ccfbf1;margin:6px 0 0;font-size:13px;">Phòng khám Da liễu &amp; Thẩm mỹ</p>
              </div>

              <p style="color:#0f172a;font-size:16px;font-weight:700;margin-bottom:4px;">Chào mừng {staffName}! 👋</p>
              <p style="color:#475569;font-size:14px;line-height:1.6;margin-bottom:20px;">
                Bạn đã được thêm vào hệ thống <b>S-Clinic</b> với vai trò <b>{roleVi}</b>.<br/>
                Hãy đặt mật khẩu để bắt đầu làm việc.
              </p>

              <div style="background:#fff;border:1px solid #e2e8f0;border-radius:12px;padding:20px;margin-bottom:20px;">
                <p style="color:#64748b;font-size:13px;margin:0 0 6px;">📧 Tài khoản đăng nhập</p>
                <p style="color:#0f172a;font-size:15px;font-weight:700;margin:0;">{toEmail}</p>
              </div>

              <div style="text-align:center;margin:28px 0;">
                <a href="{setPasswordUrl}"
                   style="display:inline-block;background:#0d9488;color:#fff;font-size:15px;font-weight:700;
                          padding:14px 36px;border-radius:12px;text-decoration:none;">
                  🔑 Đặt mật khẩu ngay →
                </a>
              </div>

              <div style="background:#fef3c7;border:1px solid #fcd34d;border-radius:10px;padding:14px 18px;margin-bottom:20px;">
                <p style="color:#92400e;font-size:13px;margin:0;">
                  ⚠️ Link chỉ có hiệu lực trong <b>24 giờ</b>. Liên hệ Admin nếu hết hạn.
                </p>
              </div>

              <p style="color:#94a3b8;font-size:12px;margin-top:24px;border-top:1px solid #e2e8f0;padding-top:16px;text-align:center;">
                Nếu bạn không biết về email này, hãy bỏ qua hoặc liên hệ quản trị viên.<br/>
                © 2026 S-Clinic
              </p>
            </div>
            """;

        var msg = new MimeMessage();
        msg.From.Add(new MailboxAddress(fromName, fromEmail));
        msg.To.Add(new MailboxAddress(staffName, toEmail));
        msg.Subject = $"[S-Clinic] Chào mừng {staffName} — Đặt mật khẩu tài khoản";
        msg.Body    = new TextPart("html") { Text = body };

        try
        {
            await SendAsync(msg);
            logger.LogInformation("Welcome email sent to staff {Email}", toEmail);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send welcome email to {Email}", toEmail);
            throw;
        }
    }
}
