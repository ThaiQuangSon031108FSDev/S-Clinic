using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SClinic.Data;
using SClinic.Models;

namespace SClinic.Controllers.Api;

/// <summary>API for Receptionist queue management.</summary>
[ApiController, Route("api/[controller]")]
[Authorize(Roles = "Receptionist,Admin,Doctor,Cashier")]
public class AppointmentsApiController(ApplicationDbContext db) : ControllerBase
{
    // GET api/appointmentsapi/all — all appointments (for history/filtering)
    [HttpGet("all")]
    public async Task<IActionResult> GetAll([FromQuery] string? date, [FromQuery] int? doctorId)
    {
        var query = db.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Schedule)
                .ThenInclude(s => s!.Doctor)
            .Include(a => a.Service)
            .Where(a => a.Schedule != null)
            .AsQueryable();

        if (DateOnly.TryParse(date, out var d))
            query = query.Where(a => a.Schedule!.WorkDate == d);

        if (doctorId.HasValue && doctorId > 0)
            query = query.Where(a => a.Schedule!.DoctorId == doctorId);

        var list = await query
            .OrderByDescending(a => a.Schedule!.WorkDate)
            .ThenBy(a => a.Schedule!.TimeSlot)
            .Select(a => new
            {
                a.AppointmentId,
                PatientName  = a.Patient.FullName,
                PatientPhone = a.Patient.Phone,
                DoctorName   = a.Schedule!.Doctor.FullName,
                DoctorId     = a.Schedule.DoctorId,
                Time         = a.Schedule.TimeSlot.ToString(@"hh\:mm"),
                Date         = a.Schedule.WorkDate.ToString("dd/MM/yyyy"),
                DateIso      = a.Schedule.WorkDate.ToString("yyyy-MM-dd"),
                Status       = a.Status.ToString(),
                Notes        = a.Notes,
                ServiceName  = a.Service != null ? a.Service.ServiceName : null,
            })
            .ToListAsync();

        return Ok(list);
    }

    // GET api/appointmentsapi/doctors — danh sách bác sĩ cho modal
    [HttpGet("doctors")]
    public async Task<IActionResult> GetDoctors()
    {
        var doctors = await db.Doctors
            .OrderBy(d => d.FullName)
            .Select(d => new { d.DoctorId, d.FullName, d.Specialty })
            .ToListAsync();
        return Ok(doctors);
    }

    // GET api/appointmentsapi/slots?doctorId=&date= — khung giờ còn trống
    [HttpGet("slots")]
    public async Task<IActionResult> GetSlots([FromQuery] int doctorId, [FromQuery] string date)
    {
        if (!DateOnly.TryParse(date, out var d)) return BadRequest("Invalid date.");

        var existing = await db.DoctorSchedules
            .Where(s => s.DoctorId == doctorId && s.WorkDate == d)
            .ToListAsync();

        // Standard time slots for the clinic
        var allSlots = new[]
        {
            "08:00","08:30","09:00","09:30","10:00","10:30","11:00","11:30",
            "13:30","14:00","14:30","15:00","15:30","16:00","16:30","17:00"
        };

        var result = allSlots.Select(t =>
        {
            var slot = existing.FirstOrDefault(s => s.TimeSlot.ToString(@"HH\:mm") == t);
            return new
            {
                Time      = t,
                Available = slot == null || slot.CurrentBooked < slot.MaxPatients,
                ScheduleId = slot?.ScheduleId
            };
        }).Where(s => s.Available).ToList();

        return Ok(result);
    }

    // GET api/appointmentsapi/today — all today's appointments for Kanban
    [HttpGet("today")]
    public async Task<IActionResult> Today()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var list  = await db.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Schedule)
                .ThenInclude(s => s!.Doctor)
            .Where(a => a.Schedule != null && a.Schedule.WorkDate == today)
            .OrderBy(a => a.Schedule!.TimeSlot)
            .Select(a => new
            {
                a.AppointmentId,
                PatientName = a.Patient.FullName,
                PatientPhone = a.Patient.Phone,
                DoctorName  = a.Schedule!.Doctor.FullName,
                Time        = a.Schedule.TimeSlot.ToString(@"hh\:mm"),
                Date        = a.Schedule.WorkDate.ToString("dd/MM/yyyy"),
                Status      = a.Status.ToString(),
            })
            .ToListAsync();

        return Ok(list);
    }

    // POST api/appointmentsapi
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] RegisterPatientDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FullName) || dto.DoctorId <= 0 || !dto.Date.HasValue)
            return BadRequest("Thiếu thông tin bắt buộc.");

        // Find or create patient by Phone
        var patient = await db.Patients.FirstOrDefaultAsync(p => p.Phone == dto.Phone);
        if (patient == null)
        {
            patient = new Patient
            {
                FullName = dto.FullName,
                Phone = dto.Phone,
                DateOfBirth = dto.Dob.HasValue ? DateOnly.FromDateTime(dto.Dob.Value) : null,
                BaseMedicalHistory = dto.MedHistory
            };
            db.Patients.Add(patient);
            await db.SaveChangesAsync();
        }

        // Find or create schedule
        var targetTime = TimeOnly.FromTimeSpan(dto.Time);
        var schedule = await db.DoctorSchedules.FirstOrDefaultAsync(s => 
            s.DoctorId == dto.DoctorId && s.WorkDate == DateOnly.FromDateTime(dto.Date.Value) && s.TimeSlot == targetTime);
        if (schedule == null)
        {
            schedule = new DoctorSchedule
            {
                DoctorId = dto.DoctorId,
                WorkDate = DateOnly.FromDateTime(dto.Date.Value),
                TimeSlot = targetTime,
                MaxPatients = 1,
                CurrentBooked = 1
            };
            db.DoctorSchedules.Add(schedule);
        }
        else
        {
            schedule.CurrentBooked++;
        }

        await db.SaveChangesAsync();

        // Create appointment
        var appt = new Appointment
        {
            PatientId = patient.PatientId,
            ScheduleId = schedule.ScheduleId,
            Status = AppointmentStatus.Confirmed // Receptionist books = Confirmed
        };
        db.Appointments.Add(appt);
        await db.SaveChangesAsync();

        return Ok(new { Success = true, AppointmentId = appt.AppointmentId });
    }

    // PATCH api/appointmentsapi/{id}/status — advance status (with CurrentBooked fix)
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] StatusDto dto)
    {
        var appt = await db.Appointments
            .Include(a => a.Schedule)
            .FirstOrDefaultAsync(a => a.AppointmentId == id);
        if (appt is null) return NotFound();

        if (!Enum.TryParse<AppointmentStatus>(dto.Status, out var newStatus))
            return BadRequest("Invalid status value.");

        // BUG FIX: Decrement CurrentBooked when cancelling
        if (newStatus == AppointmentStatus.Cancelled
            && appt.Status != AppointmentStatus.Cancelled
            && appt.Schedule != null)
        {
            appt.Schedule.CurrentBooked = Math.Max(0, appt.Schedule.CurrentBooked - 1);
        }

        appt.Status = newStatus;
        await db.SaveChangesAsync();
        return Ok(new { id, status = newStatus.ToString() });
    }

    // PATCH api/appointmentsapi/{id}/checkin — Check-in: đẩy vào hàng đợi bác sĩ
    [HttpPatch("{id}/checkin")]
    public async Task<IActionResult> CheckIn(int id)
    {
        var appt = await db.Appointments
            .Include(a => a.Schedule)
            .FirstOrDefaultAsync(a => a.AppointmentId == id);
        if (appt is null) return NotFound(new { success = false, message = "Không tìm thấy lịch hẹn." });

        if (appt.Status == AppointmentStatus.Cancelled)
            return BadRequest(new { success = false, message = "Lịch hẹn đã bị huỷ." });

        // Check-in: set to Confirmed → Doctor Queue picks up Confirmed status
        appt.Status = AppointmentStatus.Confirmed;
        await db.SaveChangesAsync();

        return Ok(new { success = true, message = "Check-in thành công. Bệnh nhân đã vào hàng đợi." });
    }

    // PATCH api/appointmentsapi/{id}/reschedule — Dời lịch (đổi ngày/giờ)
    [HttpPatch("{id}/reschedule")]
    public async Task<IActionResult> Reschedule(int id, [FromBody] RescheduleDto dto)
    {
        var appt = await db.Appointments
            .Include(a => a.Schedule)
            .FirstOrDefaultAsync(a => a.AppointmentId == id);
        if (appt is null) return NotFound(new { success = false, message = "Không tìm thấy lịch hẹn." });
        if (appt.Status == AppointmentStatus.Cancelled)
            return BadRequest(new { success = false, message = "Không thể dời lịch đã huỷ." });
        if (appt.Status == AppointmentStatus.Completed)
            return BadRequest(new { success = false, message = "Không thể dời lịch đã hoàn tất." });

        var newDate = DateOnly.FromDateTime(dto.NewDate);
        var newTime = TimeOnly.Parse(dto.NewTime);
        var doctorId = appt.Schedule?.DoctorId ?? 0;

        // Find or create the new schedule slot
        var newSchedule = await db.DoctorSchedules.FirstOrDefaultAsync(s =>
            s.DoctorId == doctorId && s.WorkDate == newDate && s.TimeSlot == newTime);

        if (newSchedule == null)
        {
            newSchedule = new DoctorSchedule
            {
                DoctorId = doctorId, WorkDate = newDate, TimeSlot = newTime,
                MaxPatients = 1, CurrentBooked = 0
            };
            db.DoctorSchedules.Add(newSchedule);
            await db.SaveChangesAsync();
        }
        else if (newSchedule.CurrentBooked >= newSchedule.MaxPatients)
        {
            return BadRequest(new { success = false, message = "Khung giờ mới đã đủ bệnh nhân." });
        }

        // Decrement old slot
        if (appt.Schedule != null)
            appt.Schedule.CurrentBooked = Math.Max(0, appt.Schedule.CurrentBooked - 1);

        // Assign new slot
        newSchedule.CurrentBooked++;
        appt.ScheduleId = newSchedule.ScheduleId;
        appt.Status     = AppointmentStatus.Confirmed;

        await db.SaveChangesAsync();
        return Ok(new
        {
            success  = true,
            newDate  = newDate.ToString("dd/MM/yyyy"),
            newTime  = newTime.ToString(@"HH\:mm")
        });
    }

    // PATCH api/appointmentsapi/{id}/reassign — Chuyển giao bác sĩ
    [HttpPatch("{id}/reassign")]
    public async Task<IActionResult> Reassign(int id, [FromBody] ReassignDto dto)
    {
        var appt = await db.Appointments
            .Include(a => a.Schedule)
            .FirstOrDefaultAsync(a => a.AppointmentId == id);
        if (appt is null) return NotFound(new { success = false, message = "Không tìm thấy lịch hẹn." });
        if (appt.Status is AppointmentStatus.Completed or AppointmentStatus.Cancelled)
            return BadRequest(new { success = false, message = "Không thể chuyển giao lịch đã đóng." });

        var oldSchedule = appt.Schedule;
        if (oldSchedule == null)
            return BadRequest(new { success = false, message = "Lịch hẹn không có khung giờ." });

        // Find or create schedule for new doctor at same date+time
        var newSchedule = await db.DoctorSchedules.FirstOrDefaultAsync(s =>
            s.DoctorId  == dto.NewDoctorId &&
            s.WorkDate  == oldSchedule.WorkDate &&
            s.TimeSlot  == oldSchedule.TimeSlot);

        if (newSchedule == null)
        {
            newSchedule = new DoctorSchedule
            {
                DoctorId = dto.NewDoctorId, WorkDate = oldSchedule.WorkDate,
                TimeSlot = oldSchedule.TimeSlot, MaxPatients = 1, CurrentBooked = 0
            };
            db.DoctorSchedules.Add(newSchedule);
            await db.SaveChangesAsync();
        }
        else if (newSchedule.CurrentBooked >= newSchedule.MaxPatients)
        {
            return BadRequest(new { success = false,
                message = "Bác sĩ được chuyển giao đã đủ bệnh nhân trong khung giờ này." });
        }

        // Transfer: decrement old, increment new
        oldSchedule.CurrentBooked = Math.Max(0, oldSchedule.CurrentBooked - 1);
        newSchedule.CurrentBooked++;
        appt.ScheduleId = newSchedule.ScheduleId;

        await db.SaveChangesAsync();

        var newDoctor = await db.Doctors.FindAsync(dto.NewDoctorId);
        return Ok(new { success = true, newDoctorName = newDoctor?.FullName });
    }
}

public record StatusDto(string Status);
public record RescheduleDto(DateTime NewDate, string NewTime);
public record ReassignDto(int NewDoctorId);

public class RegisterPatientDto
{
    [System.ComponentModel.DataAnnotations.Required]
    public string FullName { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required]
    [SClinic.Validation.ValidPhoneFormat]
    public string Phone { get; set; } = string.Empty;

    public DateTime? Dob { get; set; }
    public string MedHistory { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required]
    public int DoctorId { get; set; }

    [System.ComponentModel.DataAnnotations.Required]
    public DateTime? Date { get; set; }

    [System.ComponentModel.DataAnnotations.Required]
    public TimeSpan Time { get; set; }
}
