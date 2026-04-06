using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SClinic.Data;
using SClinic.Models;
using System.Security.Claims;

namespace SClinic.Controllers.Api;

[ApiController, Route("api/doctor/schedules")]
[Authorize(Roles = "Doctor,Admin")]
public class DoctorSchedulesApiController(ApplicationDbContext db) : ControllerBase
{
    // ── GET /api/doctor/schedules — lịch cá nhân doctor ─────────────────────
    [HttpGet]
    public async Task<IActionResult> GetMySchedules([FromQuery] string? from, [FromQuery] string? to)
    {
        var doctorId = await GetDoctorIdAsync();
        if (doctorId is null) return Unauthorized();

        var start = DateOnly.TryParse(from, out var f) ? f : DateOnly.FromDateTime(DateTime.Today);
        var end   = DateOnly.TryParse(to,   out var t) ? t : start.AddDays(13);

        var list = await db.DoctorSchedules
            .Where(s => s.DoctorId == doctorId && s.WorkDate >= start && s.WorkDate <= end)
            .OrderBy(s => s.WorkDate).ThenBy(s => s.TimeSlot)
            .Select(s => new
            {
                s.ScheduleId,
                Date         = s.WorkDate.ToString("yyyy-MM-dd"),
                TimeSlot     = s.TimeSlot.ToString("HH:mm"),
                s.MaxPatients,
                s.CurrentBooked,
                Remaining    = s.MaxPatients - s.CurrentBooked
            })
            .ToListAsync();

        return Ok(list);
    }

    // ── POST /api/doctor/schedules — tạo ca làm việc ─────────────────────────
    [HttpPost]
    public async Task<IActionResult> CreateSchedule([FromBody] CreateScheduleDto dto)
    {
        var doctorId = await GetDoctorIdAsync();
        if (doctorId is null) return Unauthorized();

        if (!DateOnly.TryParse(dto.Date, out var workDate))
            return Ok(new { success = false, message = "Ngày không hợp lệ." });

        if (workDate < DateOnly.FromDateTime(DateTime.Today))
            return Ok(new { success = false, message = "Không thể tạo ca trong quá khứ." });

        if (!TimeOnly.TryParse(dto.TimeSlot, out var ts))
            return Ok(new { success = false, message = "Khung giờ không hợp lệ." });

        if (dto.MaxPatients < 1 || dto.MaxPatients > 10)
            return Ok(new { success = false, message = "Số bệnh nhân tối đa phải từ 1–10." });

        // Check duplicate overlap — same doctor, same date, same timeslot
        var exists = await db.DoctorSchedules.AnyAsync(s =>
            s.DoctorId  == doctorId &&
            s.WorkDate  == workDate &&
            s.TimeSlot  == ts);

        if (exists)
            return Ok(new { success = false, message = "Ca làm việc này đã tồn tại." });

        var schedule = new DoctorSchedule
        {
            DoctorId      = doctorId.Value,
            WorkDate      = workDate,
            TimeSlot      = ts,
            MaxPatients   = dto.MaxPatients,
            CurrentBooked = 0
        };
        db.DoctorSchedules.Add(schedule);
        await db.SaveChangesAsync();

        return Ok(new { success = true, scheduleId = schedule.ScheduleId });
    }

    // ── DELETE /api/doctor/schedules/{id} — xóa ca (chỉ khi chưa có đặt lịch)
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteSchedule(int id)
    {
        var doctorId = await GetDoctorIdAsync();
        if (doctorId is null) return Unauthorized();

        var schedule = await db.DoctorSchedules
            .FirstOrDefaultAsync(s => s.ScheduleId == id && s.DoctorId == doctorId);

        if (schedule is null)
            return Ok(new { success = false, message = "Không tìm thấy ca làm việc." });

        if (schedule.CurrentBooked > 0)
            return Ok(new { success = false, message = "Ca đã có bệnh nhân đặt, không thể xóa." });

        db.DoctorSchedules.Remove(schedule);
        await db.SaveChangesAsync();
        return Ok(new { success = true });
    }

    // ── POST /api/doctor/schedules/bulk — tạo hàng loạt cho 1 tuần ──────────
    [HttpPost("bulk")]
    public async Task<IActionResult> BulkCreate([FromBody] BulkScheduleDto dto)
    {
        var doctorId = await GetDoctorIdAsync();
        if (doctorId is null) return Unauthorized();

        if (!DateOnly.TryParse(dto.StartDate, out var startDate))
            return Ok(new { success = false, message = "Ngày bắt đầu không hợp lệ." });

        var timeSlots = dto.TimeSlots
            .Select(ts => TimeOnly.TryParse(ts, out var t) ? t : (TimeOnly?)null)
            .Where(t => t.HasValue)
            .Select(t => t!.Value)
            .ToList();

        if (!timeSlots.Any())
            return Ok(new { success = false, message = "Chọn ít nhất 1 khung giờ." });

        var created = 0;
        for (int dayOffset = 0; dayOffset < dto.DaysCount; dayOffset++)
        {
            var date = startDate.AddDays(dayOffset);
            if (dto.SkipSunday && date.DayOfWeek == DayOfWeek.Sunday) continue;

            foreach (var ts in timeSlots)
            {
                var exists = await db.DoctorSchedules.AnyAsync(s =>
                    s.DoctorId == doctorId && s.WorkDate == date && s.TimeSlot == ts);

                if (!exists)
                {
                    db.DoctorSchedules.Add(new DoctorSchedule
                    {
                        DoctorId      = doctorId.Value,
                        WorkDate      = date,
                        TimeSlot      = ts,
                        MaxPatients   = dto.MaxPatients,
                        CurrentBooked = 0
                    });
                    created++;
                }
            }
        }

        await db.SaveChangesAsync();
        return Ok(new { success = true, created });
    }

    private async Task<int?> GetDoctorIdAsync()
    {
        var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        // Admin can manage — return first doctor found if called by admin
        if (User.IsInRole("Admin"))
        {
            // Admin must pass doctorId via query param
            if (Request.Query.TryGetValue("doctorId", out var dId) && int.TryParse(dId, out var did))
                return did;
        }
        var doctor = await db.Doctors.FirstOrDefaultAsync(d => d.AccountId == accountId);
        return doctor?.DoctorId;
    }
}

public record CreateScheduleDto(string Date, string TimeSlot, int MaxPatients);
public record BulkScheduleDto(string StartDate, int DaysCount, string[] TimeSlots, int MaxPatients, bool SkipSunday);
