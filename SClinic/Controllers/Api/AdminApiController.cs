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

        var accounts = await db.Accounts
            .Include(a => a.Role)
            .Include(a => a.Doctor)
            .Where(a => staffRoles.Contains(a.Role.RoleName))
            .OrderBy(a => a.Role.RoleName)
            .ThenBy(a => a.Doctor != null ? a.Doctor.FullName : a.Email)
            .Select(a => new
            {
                a.AccountId,
                a.Email,
                a.IsActive,
                Role      = a.Role.RoleName,
                FullName  = a.Doctor != null ? a.Doctor.FullName : a.Email,
                Specialty = a.Doctor != null ? a.Doctor.Specialty : null,
            })
            .ToListAsync();

        return Ok(accounts);
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
}

public record StaffDto(
    int AccountId, string FullName, string Email, string Role,
    string? Password, string? Specialty,
    int? YearsExp, string? Qualification, string? Bio);
