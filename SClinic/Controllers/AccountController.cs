using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SClinic.Data;
using SClinic.Helpers;
using SClinic.Models;
using SClinic.Services;
using System.Security.Claims;

namespace SClinic.Controllers;

public class AccountController(
    ApplicationDbContext db,
    IConfiguration config,
    OtpService otp,
    EmailService email,
    IWebHostEnvironment env,
    ILogger<AccountController> logger) : Controller
{
    // ════════════════════════════════════════════════════════════════
    // LOGIN
    // ════════════════════════════════════════════════════════════════

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectByRole();
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
    {
        var account = await db.Accounts
            .Include(a => a.Role)
            .FirstOrDefaultAsync(a => a.Email == email && a.IsActive);

        if (account is null || !BCrypt.Net.BCrypt.Verify(password, account.PasswordHash))
        {
            ViewData["Error"] = "Email hoặc mật khẩu không đúng.";
            return View();
        }

        // JWT cookie (for API calls)
        var token = JwtHelper.GenerateToken(account, config);
        Response.Cookies.Append("sc_token", token, new CookieOptions
        {
            HttpOnly = true, Secure = false, SameSite = SameSiteMode.Strict,
            Expires  = DateTimeOffset.UtcNow.AddHours(8)
        });

        // Cookie-auth principal (for [Authorize] on MVC controllers)
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, account.AccountId.ToString()),
            new(ClaimTypes.Email, account.Email),
            new(ClaimTypes.Role, account.Role.RoleName),
            new(ClaimTypes.Name, account.Email),
        };
        var identity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8) });

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectByRole(account.Role.RoleName);
    }

    // ════════════════════════════════════════════════════════════════
    // REGISTER — Phone → OTP → Complete profile
    // ════════════════════════════════════════════════════════════════

    [HttpGet]
    public IActionResult Register() => View();

    /// <summary>POST /Account/SendOtp — validates email, generates OTP, sends via Gmail.</summary>
    [HttpPost]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Phone))
            return Json(new { success = false, message = "Vui lòng nhập địa chỉ email." });

        var emailAddr = req.Phone.Trim();

        // Check if email already registered
        if (await db.Accounts.AnyAsync(a => a.Email == emailAddr && a.IsActive))
            return Json(new { success = false, message = "Email này đã được đăng ký. Vui lòng đăng nhập." });

        var code = otp.Generate(emailAddr);

        string? devOtp = null;
        try
        {
            await email.SendOtpAsync(emailAddr, "Bạn", code);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Email send failed ({Msg}). Using dev-fallback OTP.", ex.Message);
            // Dev fallback: include OTP in response so UI shows it
            if (env.IsDevelopment()) devOtp = code;
            else return Json(new { success = false, message = $"Không thể gửi email: {ex.Message}" });
        }

        return Json(new { success = true, existingPatient = false, patientName = "", devOtp });
    }

    /// <summary>POST /Account/VerifyOtp — verifies 6-digit code (keyed by email).</summary>
    [HttpPost]
    public IActionResult VerifyOtp([FromBody] VerifyOtpRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Phone) || string.IsNullOrWhiteSpace(req.Code))
            return Json(new { success = false, message = "Dữ liệu không hợp lệ." });

        var ok = otp.Verify(req.Phone.Trim(), req.Code.Trim());
        return Json(ok
            ? new { success = true, message = (string)"" }
            : new { success = false, message = "Mã OTP không đúng hoặc đã hết hạn. Vui lòng thử lại." });
    }


    /// <summary>POST /Account/CompleteRegister — create account + auto-link to patient if phone matches.</summary>
    [HttpPost]
    public async Task<IActionResult> CompleteRegister([FromBody] CompleteRegisterRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.FullName) || string.IsNullOrWhiteSpace(req.Password))
            return Json(new { success = false, message = "Vui lòng điền đầy đủ thông tin." });

        if (req.Password.Length < 8)
            return Json(new { success = false, message = "Mật khẩu phải có ít nhất 8 ký tự." });

        // Decide email (use provided or phone-based placeholder)
        var email = string.IsNullOrWhiteSpace(req.Email)
            ? $"bn_{req.Phone}@sclinic.internal"
            : req.Email.Trim();

        if (await db.Accounts.AnyAsync(a => a.Email == email))
            return Json(new { success = false, message = "Email đã được sử dụng. Vui lòng chọn email khác." });

        var patientRole = await db.Roles.FirstAsync(r => r.RoleName == "Patient");
        var hash        = BCrypt.Net.BCrypt.HashPassword(req.Password, workFactor: 11);

        var account = new Account
        {
            Email        = email,
            PasswordHash = hash,
            RoleId       = patientRole.RoleId,
            IsActive     = true
        };
        db.Accounts.Add(account);
        await db.SaveChangesAsync(); // get AccountId

        // Auto-link or create patient record
        var existingPatient = await db.Patients
            .FirstOrDefaultAsync(p => p.Phone == req.Phone && p.AccountId == null);

        if (existingPatient is not null)
        {
            // Map existing walk-in record → new account
            existingPatient.AccountId = account.AccountId;
        }
        else
        {
            // Brand new patient
            db.Patients.Add(new Patient
            {
                AccountId          = account.AccountId,
                FullName           = req.FullName.Trim(),
                Phone              = req.Phone,
                BaseMedicalHistory = ""
            });
        }

        await db.SaveChangesAsync();
        return Json(new { success = true });
    }

    // ════════════════════════════════════════════════════════════════
    // LOGOUT / ACCESS DENIED
    // ════════════════════════════════════════════════════════════════

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        Response.Cookies.Delete("sc_token");
        return RedirectToAction("Index", "Home");
    }

    public IActionResult AccessDenied() => View();

    // ── helpers ────────────────────────────────────────────────────
    private IActionResult RedirectByRole(string? role = null)
    {
        role ??= User.FindFirst(ClaimTypes.Role)?.Value;
        return role switch
        {
            "Patient"      => RedirectToAction("Dashboard", "Patient"),
            "Doctor"       => RedirectToAction("Dashboard", "Doctor"),
            "Receptionist" => RedirectToAction("Dashboard", "Receptionist"),
            "Cashier"      => RedirectToAction("Finance",   "Admin"),
            "Admin"        => RedirectToAction("Dashboard", "Admin"),
            _              => RedirectToAction("Index", "Home")
        };
    }
}

// ── Request DTOs ───────────────────────────────────────────────────
public record SendOtpRequest(string Phone);
public record VerifyOtpRequest(string Phone, string Code);
public record CompleteRegisterRequest(string Phone, string FullName, string Email, string Password);
