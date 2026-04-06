using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SClinic.Data;
using SClinic.Models;

namespace SClinic.Controllers.Api;

[ApiController, Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminApiController(ApplicationDbContext db) : ControllerBase
{
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

        if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6)
            return Ok(new { success = false, message = "Mật khẩu phải ít nhất 6 ký tự." });

        await using var tx = await db.Database.BeginTransactionAsync();

        // 1. Create Account
        var account = new Account
        {
            Email        = dto.Email.Trim().ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            RoleId       = role.RoleId,
            IsActive     = true
        };
        db.Accounts.Add(account);
        await db.SaveChangesAsync();

        // 2. If Doctor → create Doctor profile
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
        return Ok(new { success = true, accountId = account.AccountId });
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
