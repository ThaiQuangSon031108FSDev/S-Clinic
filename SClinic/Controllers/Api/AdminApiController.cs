using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SClinic.Data;
using SClinic.Models;
using SClinic.Services;

namespace SClinic.Controllers.Api;

[ApiController, Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminApiController(
    ApplicationDbContext db,
    EmailService emailService,
    IHttpContextAccessor httpContextAccessor,
    ILogger<AdminApiController> logger) : ControllerBase
{
    // ── In-memory set-password token store (token → (accountId, expiry)) ─────
    private static readonly Dictionary<string, (int AccountId, DateTime Expiry)> _setPasswordTokens = new();

    // ── GET /api/admin/staff ──────────────────────────────────────────────────
    [HttpGet("staff")]
    public async Task<IActionResult> GetStaff()
    {
        var staffRoles = new[] { "Doctor", "Receptionist", "Cashier" };

        var raw = await db.Accounts
            .Include(a => a.Role)
            .Include(a => a.Doctor)
            .Where(a => staffRoles.Contains(a.Role.RoleName))
            .OrderBy(a => a.Role.RoleName)
            .ThenBy(a => a.Doctor != null ? a.Doctor.FullName : a.Email)
            .ToListAsync();

        var result = raw.Select(a => new
        {
            a.AccountId,
            a.Email,
            a.IsActive,
            Role      = a.Role.RoleName,
            // Doctor profile → FullName; others → part before @ in email
            FullName  = a.Doctor?.FullName ?? a.Email.Split('@')[0],
            Specialty = a.Doctor?.Specialty,
        });

        return Ok(result);
    }

    // ── POST /api/admin/staff — tạo tài khoản nhân sự ────────────────────────
    [HttpPost("staff")]
    public async Task<IActionResult> CreateStaff([FromBody] StaffDto dto)
    {
        if (await db.Accounts.AnyAsync(a => a.Email == dto.Email))
            return Ok(new { success = false, message = "Email này đã tồn tại trong hệ thống." });

        var role = await db.Roles.FirstOrDefaultAsync(r => r.RoleName == dto.Role);
        if (role is null)
            return Ok(new { success = false, message = "Vai trò không hợp lệ." });

        // Auto-generate a random temp password (never shown to admin)
        var tempPassword = Guid.NewGuid().ToString("N")[..12] + "Sc1!";

        await using var tx = await db.Database.BeginTransactionAsync();

        var account = new Account
        {
            Email        = dto.Email.Trim().ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword),
            RoleId       = role.RoleId,
            IsActive     = true
        };
        db.Accounts.Add(account);
        await db.SaveChangesAsync();

        if (dto.Role == "Doctor")
        {
            db.Doctors.Add(new Doctor
            {
                AccountId = account.AccountId,
                FullName  = dto.FullName.Trim(),
                Specialty = dto.Specialty?.Trim()
            });
            await db.SaveChangesAsync();
        }

        await tx.CommitAsync();

        // ── Generate set-password token & send welcome email ─────────────────
        var token   = Guid.NewGuid().ToString("N");
        var expiry  = DateTime.UtcNow.AddHours(24);
        lock (_setPasswordTokens) { _setPasswordTokens[token] = (account.AccountId, expiry); }

        var req     = httpContextAccessor.HttpContext!.Request;
        var baseUrl = $"{req.Scheme}://{req.Host}";
        var setPasswordUrl = $"{baseUrl}/Account/SetPassword?token={token}";

        var emailSent = false;
        try
        {
            await emailService.SendWelcomeStaffAsync(dto.Email, dto.FullName, dto.Role, setPasswordUrl);
            emailSent = true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Welcome email failed for {Email} — account created OK", dto.Email);
        }

        return Ok(new {
            success = true,
            accountId = account.AccountId,
            emailSent,
            // Trả về URL để Admin copy thủ công nếu email lỗi
            setPasswordUrl = emailSent ? null : setPasswordUrl
        });
    }

    // ── PUT /api/admin/staff/{id} — cập nhật thông tin ───────────────────────
    [HttpPut("staff/{id:int}")]
    public async Task<IActionResult> UpdateStaff(int id, [FromBody] StaffDto dto)
    {
        var account = await db.Accounts
            .Include(a => a.Role)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.AccountId == id);

        if (account is null)
            return Ok(new { success = false, message = "Không tìm thấy tài khoản." });

        // Update password if provided
        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            if (dto.Password.Length < 6)
                return Ok(new { success = false, message = "Mật khẩu phải ít nhất 6 ký tự." });
            account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        }

        // Update Doctor profile if exists
        if (account.Doctor is not null)
        {
            account.Doctor.FullName  = dto.FullName.Trim();
            account.Doctor.Specialty = dto.Specialty?.Trim();
        }

        await db.SaveChangesAsync();
        return Ok(new { success = true });
    }

    // ── PATCH /api/admin/staff/{id}/toggle — vô hiệu/kích hoạt ──────────────
    [HttpPatch("staff/{id:int}/toggle")]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var account = await db.Accounts.FindAsync(id);
        if (account is null)
            return Ok(new { success = false, message = "Không tìm thấy tài khoản." });

        account.IsActive = !account.IsActive;
        await db.SaveChangesAsync();
        return Ok(new { success = true, isActive = account.IsActive });
    }

    // ── POST /api/admin/staff/{id}/resend-welcome — gửi lại welcome email ────
    [HttpPost("staff/{id:int}/resend-welcome")]
    public async Task<IActionResult> ResendWelcome(int id)
    {
        var account = await db.Accounts
            .Include(a => a.Doctor)
            .Include(a => a.Role)
            .FirstOrDefaultAsync(a => a.AccountId == id);
        if (account is null)
            return Ok(new { success = false, message = "Không tìm thấy tài khoản." });

        var token  = Guid.NewGuid().ToString("N");
        var expiry = DateTime.UtcNow.AddHours(24);
        lock (_setPasswordTokens) { _setPasswordTokens[token] = (id, expiry); }

        var req = httpContextAccessor.HttpContext!.Request;
        var setPasswordUrl = $"{req.Scheme}://{req.Host}/Account/SetPassword?token={token}";
        var staffName = account.Doctor?.FullName ?? account.Email.Split('@')[0];

        try
        {
            await emailService.SendWelcomeStaffAsync(account.Email, staffName, account.Role.RoleName, setPasswordUrl);
            return Ok(new { success = true, message = "Đã gửi lại email mời đặt mật khẩu." });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Resend welcome failed for {Email}", account.Email);
            return Ok(new { success = false, message = "Gửi email thất bại. Kiểm tra cấu hình SMTP.", setPasswordUrl });
        }
    }

    // ── GET/POST /api/admin/staff/verify-set-password-token ──────────────────
    [AllowAnonymous]
    [HttpGet("verify-set-password-token")]
    public IActionResult VerifyToken([FromQuery] string token)
    {
        lock (_setPasswordTokens)
        {
            if (_setPasswordTokens.TryGetValue(token, out var entry) && entry.Expiry > DateTime.UtcNow)
                return Ok(new { valid = true, accountId = entry.AccountId });
        }
        return Ok(new { valid = false });
    }

    [AllowAnonymous]
    [HttpPost("set-password")]
    public async Task<IActionResult> SetPassword([FromBody] SetPasswordDto dto)
    {
        int accountId;
        lock (_setPasswordTokens)
        {
            if (!_setPasswordTokens.TryGetValue(dto.Token, out var entry) || entry.Expiry <= DateTime.UtcNow)
                return Ok(new { success = false, message = "Link đã hết hạn hoặc không hợp lệ." });
            accountId = entry.AccountId;
            _setPasswordTokens.Remove(dto.Token); // one-time use
        }

        if (dto.Password.Length < 8)
            return Ok(new { success = false, message = "Mật khẩu phải ít nhất 8 ký tự." });

        var account = await db.Accounts.FindAsync(accountId);
        if (account is null)
            return Ok(new { success = false, message = "Không tìm thấy tài khoản." });

        account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        await db.SaveChangesAsync();
        return Ok(new { success = true });
    }

    // ── GET /api/admin/schedules — xem lịch tất cả bác sĩ ───────────────────
    [HttpGet("schedules")]
    public async Task<IActionResult> GetAllSchedules([FromQuery] string? date)
    {
        var query = db.DoctorSchedules
            .Include(s => s.Doctor)
            .AsQueryable();

        if (DateOnly.TryParse(date, out var d))
            query = query.Where(s => s.WorkDate == d);
        else
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            query = query.Where(s => s.WorkDate >= today && s.WorkDate <= today.AddDays(7));
        }

        var list = await query
            .OrderBy(s => s.WorkDate).ThenBy(s => s.TimeSlot)
            .Select(s => new
            {
                s.ScheduleId,
                DoctorName = s.Doctor.FullName,
                Date       = s.WorkDate.ToString("yyyy-MM-dd"),
                TimeSlot   = s.TimeSlot.ToString("HH:mm"),
                s.MaxPatients,
                s.CurrentBooked
            })
            .ToListAsync();

        return Ok(list);
    }

    // ── GET /api/admin/top-services — dịch vụ & gói bán chạy ────────────────
    [HttpGet("top-services")]
    public async Task<IActionResult> TopServices()
    {
        // Count InvoiceDetails per Service
        var services = await db.InvoiceDetails
            .Where(d => d.ItemType == InvoiceItemType.Service && d.ServiceId != null)
            .GroupBy(d => d.ServiceId)
            .Select(g => new
            {
                Id    = g.Key,
                Count = g.Sum(x => x.Quantity),
                Type  = "Dịch vụ lẻ"
            })
            .ToListAsync();

        var serviceNames = await db.Services
            .Where(s => services.Select(x => x.Id).Contains(s.ServiceId))
            .ToDictionaryAsync(s => (int?)s.ServiceId, s => s.ServiceName);

        // Count InvoiceDetails per Package
        var packages = await db.InvoiceDetails
            .Where(d => d.ItemType == InvoiceItemType.Package && d.PackageId != null)
            .GroupBy(d => d.PackageId)
            .Select(g => new
            {
                Id    = g.Key,
                Count = g.Sum(x => x.Quantity),
                Type  = "Gói liệu trình"
            })
            .ToListAsync();

        var packageNames = await db.TreatmentPackages
            .Where(p => packages.Select(x => x.Id).Contains(p.PackageId))
            .ToDictionaryAsync(p => (int?)p.PackageId, p => p.PackageName);

        var combined = services
            .Select(s => new {
                Name  = serviceNames.GetValueOrDefault(s.Id, "—"),
                s.Type,
                s.Count
            })
            .Concat(packages.Select(p => new {
                Name  = packageNames.GetValueOrDefault(p.Id, "—"),
                p.Type,
                p.Count
            }))
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToList();

        return Ok(combined);
    }

    // ── GET /api/admin/doctor-stats — hiệu suất bác sĩ tháng này ────────────
    [HttpGet("doctor-stats")]
    public async Task<IActionResult> DoctorStats()
    {
        var firstDay  = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var firstDayO = DateOnly.FromDateTime(firstDay);

        // Count completed appointments per doctor this month
        var apptCounts = await db.Appointments
            .Where(a => a.Status == AppointmentStatus.Completed
                     && a.Schedule != null
                     && a.Schedule.WorkDate >= firstDayO)
            .GroupBy(a => a.Schedule!.DoctorId)
            .Select(g => new { DoctorId = g.Key, Count = g.Count() })
            .ToListAsync();

        // Sum invoices linked to medical records by doctor this month
        var revenues = await db.Invoices
            .Where(i => i.PaymentStatus == PaymentStatus.Paid
                     && i.Record != null
                     && i.CreatedDate >= firstDay)
            .Include(i => i.Record)
            .GroupBy(i => i.Record!.DoctorId)
            .Select(g => new { DoctorId = g.Key, Revenue = g.Sum(x => x.TotalAmount) })
            .ToListAsync();

        var doctors = await db.Doctors
            .Select(d => new { d.DoctorId, d.FullName })
            .ToListAsync();

        var result = doctors
            .Select(d => new
            {
                d.DoctorId,
                d.FullName,
                AppointmentCount = apptCounts.FirstOrDefault(a => a.DoctorId == d.DoctorId)?.Count ?? 0,
                Revenue          = revenues.FirstOrDefault(r => r.DoctorId == d.DoctorId)?.Revenue ?? 0m
            })
            .Where(d => d.AppointmentCount > 0 || d.Revenue > 0)
            .OrderByDescending(d => d.AppointmentCount)
            .Take(5)
            .ToList();

        return Ok(result);
    }

    // ── GET /api/admin/kpi-stats — KPI lễ tân & thu ngân tháng này ──────────
    [HttpGet("kpi-stats")]
    public async Task<IActionResult> KpiStats()
    {
        var firstDay  = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var firstDayO = DateOnly.FromDateTime(firstDay);

        var patientsThisMonth = await db.Patients
            .CountAsync();  // total registered patients (no CreatedAt on Patient)

        var appointmentsThisMonth = await db.Appointments
            .Where(a => a.Schedule != null && a.Schedule.WorkDate >= firstDayO)
            .CountAsync();

        var invoicesPaid = await db.Invoices
            .Where(i => i.PaymentStatus == PaymentStatus.Paid && i.CreatedDate >= firstDay)
            .CountAsync();

        var invoicesTotal = await db.Invoices
            .Where(i => i.CreatedDate >= firstDay)
            .CountAsync();

        return Ok(new
        {
            PatientsTotal      = patientsThisMonth,
            AppointmentsMonth  = appointmentsThisMonth,
            InvoicesPaid       = invoicesPaid,
            InvoicesTotal      = invoicesTotal,
            CollectionRate     = invoicesTotal > 0 ? (int)Math.Round((double)invoicesPaid / invoicesTotal * 100) : 0
        });
    }
}

public record StaffDto(
    int AccountId, string FullName, string Email, string Role,
    string? Password, string? Specialty,
    int? YearsExp, string? Qualification, string? Bio);

public record SetPasswordDto(string Token, string Password);

