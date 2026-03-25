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

        // Parse date & time
        if (!DateOnly.TryParse(req.Date, out var workDate))
            return Json(new { success = false, message = "Ngày không hợp lệ." });
        if (!TimeOnly.TryParse(req.Time, out var timeSlot))
            return Json(new { success = false, message = "Giờ không hợp lệ." });

        // Auto-assign doctor if doctorId = 0
        var doctorId = req.DoctorId;
        if (doctorId <= 0)
        {
            var ids = await db.Doctors.Select(d => d.DoctorId).ToListAsync();
            if (!ids.Any())
                return Json(new { success = false, message = "Hiện chưa có bác sĩ. Vui lòng liên hệ phòng khám." });
            doctorId = ids[new Random().Next(ids.Count)];
        }

        // Find or create doctor schedule slot
        var schedule = await db.DoctorSchedules
            .FirstOrDefaultAsync(s => s.DoctorId == doctorId
                                   && s.WorkDate  == workDate
                                   && s.TimeSlot  == timeSlot);

        if (schedule is null)
        {
            schedule = new DoctorSchedule
            {
                DoctorId = doctorId,
                WorkDate = workDate,
                TimeSlot = timeSlot,
            };
            db.DoctorSchedules.Add(schedule);
            await db.SaveChangesAsync();
        }
        else
        {
            // Check if slot already booked
            var taken = await db.Appointments.AnyAsync(a =>
                a.ScheduleId == schedule.ScheduleId &&
                a.Status != AppointmentStatus.Cancelled);
            if (taken)
                return Json(new { success = false, message = "Khung giờ này đã có người đặt. Vui lòng chọn giờ khác." });
        }

        var appt = new Appointment
        {
            PatientId  = patient.PatientId,
            ScheduleId = schedule.ScheduleId,
            Status     = AppointmentStatus.Pending,
            // Notes field not in model — store note in MedicalRecord if needed
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
