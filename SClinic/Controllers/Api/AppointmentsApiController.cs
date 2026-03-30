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
    public async Task<IActionResult> GetAll()
    {
        var list = await db.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Schedule)
                .ThenInclude(s => s!.Doctor)
            .Where(a => a.Schedule != null)
            .OrderByDescending(a => a.Schedule!.WorkDate)
            .Select(a => new
            {
                a.AppointmentId,
                PatientName  = a.Patient.FullName,
                PatientPhone = a.Patient.Phone,
                DoctorName   = a.Schedule!.Doctor.FullName,
                Time         = a.Schedule.TimeSlot.ToString(@"hh\:mm"),
                Date         = a.Schedule.WorkDate.ToString("dd/MM/yyyy"), // Display format
                DateIso      = a.Schedule.WorkDate.ToString("yyyy-MM-dd"), // Sort/filter format
                Status       = a.Status.ToString(),
            })
            .ToListAsync();

        return Ok(list);
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

    // PATCH api/appointmentsapi/{id}/status — advance status
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] StatusDto dto)
    {
        var appt = await db.Appointments.FindAsync(id);
        if (appt is null) return NotFound();

        if (!Enum.TryParse<AppointmentStatus>(dto.Status, out var newStatus))
            return BadRequest("Invalid status value.");

        appt.Status = newStatus;
        await db.SaveChangesAsync();
        return Ok(new { id, status = newStatus.ToString() });
    }
}

public record StatusDto(string Status);

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
