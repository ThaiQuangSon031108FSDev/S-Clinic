using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SClinic.Data;
using SClinic.Models;
using SClinic.Services.Interfaces;

namespace SClinic.Controllers;

[Authorize(Roles = "Patient,Admin")]
public class PatientController(ApplicationDbContext db, IBookingService booking, ITreatmentService treatment) : Controller
{
    // GET /Patient/Dashboard
    public async Task<IActionResult> Dashboard()
    {
        var accountId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var patient = await db.Patients.FirstOrDefaultAsync(p => p.AccountId == accountId);
        if (patient is null) return NotFound();

        var appointments = await booking.GetPatientAppointmentsAsync(patient.PatientId);
        var treatments = await treatment.GetPatientTreatmentsAsync(patient.PatientId);

        ViewBag.Appointments = appointments;
        ViewBag.Treatments = treatments;
        return View(patient);
    }

    // GET /Patient/Book
    public IActionResult Book() => View();

    // POST /Patient/Book — create appointment
    [HttpPost]
    public async Task<IActionResult> Book([FromBody] BookRequest req)
    {
        var accountId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var patient   = await db.Patients.FirstOrDefaultAsync(p => p.AccountId == accountId);
        if (patient is null)
            return Json(new { success = false, message = "Không tìm thấy hồ sơ bệnh nhân." });

        // Validate past date
        if (!DateOnly.TryParse(req.Date, out var workDate))
            return Json(new { success = false, message = "Ngày không hợp lệ." });
        if (workDate < DateOnly.FromDateTime(DateTime.Today))
            return Json(new { success = false, message = "Không thể đặt lịch ngày đã qua." });
        if (!TimeOnly.TryParse(req.Time, out var timeSlot))
            return Json(new { success = false, message = "Giờ không hợp lệ." });

        int doctorId = req.DoctorId;

        // Bug #2 FIX: Auto-assign — find first doctor with an OPEN slot at this time
        if (doctorId <= 0)
        {
            var busyDoctorIds = await db.Appointments
                .Where(a => a.Schedule.WorkDate == workDate
                         && a.Schedule.TimeSlot == timeSlot
                         && a.Status != AppointmentStatus.Cancelled)
                .Select(a => a.Schedule.DoctorId)
                .Distinct()
                .ToListAsync();

            var availableDoctor = await db.DoctorSchedules
                .Where(s => s.WorkDate == workDate
                         && s.TimeSlot == timeSlot
                         && !busyDoctorIds.Contains(s.DoctorId))
                .Select(s => s.DoctorId)
                .FirstOrDefaultAsync();

            if (availableDoctor == 0)
                return Json(new { success = false, message = "Tất cả bác sĩ đã kín lịch cho khung giờ này. Vui lòng chọn giờ khác." });

            doctorId = availableDoctor;
        }

        // Bug #3 FIX: Do NOT auto-create schedule — only use existing open slots
        var schedule = await db.DoctorSchedules
            .FirstOrDefaultAsync(s => s.DoctorId == doctorId
                                   && s.WorkDate  == workDate
                                   && s.TimeSlot  == timeSlot);

        if (schedule is null)
            return Json(new { success = false, message = "Bác sĩ không có ca làm việc vào khung giờ này. Vui lòng chọn giờ khác." });

        // Check slot not already taken
        var taken = await db.Appointments.AnyAsync(a =>
            a.ScheduleId == schedule.ScheduleId &&
            a.Status != AppointmentStatus.Cancelled);
        if (taken)
            return Json(new { success = false, message = "Khung giờ này đã có người đặt. Vui lòng chọn giờ khác." });

        // Bug #1 + #9 FIX: Save ServiceId and Notes
        var appt = new Appointment
        {
            PatientId  = patient.PatientId,
            ScheduleId = schedule.ScheduleId,
            ServiceId  = req.ServiceId > 0 ? req.ServiceId : null,
            Notes      = req.Note?.Trim(),
            Status     = AppointmentStatus.Pending,
        };
        db.Appointments.Add(appt);
        await db.SaveChangesAsync();

        return Json(new { success = true, appointmentId = appt.AppointmentId });
    }

    // GET /Patient/Treatments
    public async Task<IActionResult> Treatments()
    {
        var accountId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var patient = await db.Patients.FirstOrDefaultAsync(p => p.AccountId == accountId);
        if (patient is null) return NotFound();

        var treatments = await treatment.GetPatientTreatmentsAsync(patient.PatientId);
        return View(treatments);
    }

    // GET /Patient/TreatmentDetail/{id}
    public async Task<IActionResult> TreatmentDetail(int id)
    {
        var detail = await treatment.GetTreatmentDetailAsync(id);
        return detail is null ? NotFound() : View(detail);
    }
}

public record BookRequest(int DoctorId, int ServiceId, string Date, string Time, string? Note);
