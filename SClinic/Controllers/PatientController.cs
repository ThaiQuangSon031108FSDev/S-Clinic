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

        if (!DateOnly.TryParse(req.Date, out var workDate))
            return Json(new { success = false, message = "Ngày không hợp lệ." });
        if (workDate < DateOnly.FromDateTime(DateTime.Today))
            return Json(new { success = false, message = "Không thể đặt lịch ngày đã qua." });
        if (!TimeOnly.TryParse(req.Time, out var timeSlot))
            return Json(new { success = false, message = "Giờ không hợp lệ." });

        // ── Find or auto-create a schedule slot ──────────────────────────────
        // 1. Try to find an existing open schedule for this date+time
        var schedule = await db.DoctorSchedules
            .FirstOrDefaultAsync(s => s.WorkDate == workDate
                                   && s.TimeSlot  == timeSlot
                                   && s.CurrentBooked < s.MaxPatients);

        // 2. If none exists → auto-assign any doctor & create schedule on-the-fly
        if (schedule is null)
        {
            // Pick the doctor that has fewest bookings today (load balance)
            var doctor = await db.Doctors
                .OrderBy(d => db.DoctorSchedules
                    .Where(s => s.DoctorId == d.DoctorId && s.WorkDate == workDate)
                    .Sum(s => (int?)s.CurrentBooked) ?? 0)
                .FirstOrDefaultAsync();

            if (doctor is null)
                return Json(new { success = false, message = "Hiện không có bác sĩ nào trong hệ thống. Vui lòng liên hệ phòng khám." });

            schedule = new DoctorSchedule
            {
                DoctorId       = doctor.DoctorId,
                WorkDate       = workDate,
                TimeSlot       = timeSlot,
                MaxPatients    = 1,
                CurrentBooked  = 0
            };
            db.DoctorSchedules.Add(schedule);
            await db.SaveChangesAsync(); // get ScheduleId
        }

        // ── Check slot still available ────────────────────────────────────────
        var taken = await db.Appointments.AnyAsync(a =>
            a.ScheduleId == schedule.ScheduleId &&
            a.Status != AppointmentStatus.Cancelled);
        if (taken)
            return Json(new { success = false, message = "Khung giờ này vừa được đặt. Vui lòng chọn giờ khác." });

        // ── Create appointment ────────────────────────────────────────────────
        var appt = new Appointment
        {
            PatientId  = patient.PatientId,
            ScheduleId = schedule.ScheduleId,
            ServiceId  = req.ServiceId > 0 ? req.ServiceId : null,
            Notes      = req.Note?.Trim(),
            Status     = AppointmentStatus.Pending,
        };
        db.Appointments.Add(appt);

        schedule.CurrentBooked++;
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

    // GET /api/patient/invoices — returns invoices for the logged-in patient
    [HttpGet("/api/patient/invoices")]
    public async Task<IActionResult> MyInvoices()
    {
        var accountId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var patient = await db.Patients.FirstOrDefaultAsync(p => p.AccountId == accountId);
        if (patient is null) return Ok(Array.Empty<object>());

        var invoices = await db.Invoices
            .Include(i => i.Record)
                .ThenInclude(r => r!.Appointment)
            .Include(i => i.InvoiceDetails)
                .ThenInclude(d => d.Medicine)
            .Include(i => i.InvoiceDetails)
                .ThenInclude(d => d.Service)
            .Where(i => i.Record != null
                     && i.Record.Appointment != null
                     && i.Record.Appointment.PatientId == patient.PatientId)
            .OrderByDescending(i => i.CreatedDate)
            .ToListAsync();

        var result = invoices.Select(i => new {
            i.InvoiceId,
            i.TotalAmount,
            Status      = i.PaymentStatus.ToString(),
            CreatedDate = i.CreatedDate.ToString("dd/MM/yyyy HH:mm"),
            Details     = i.InvoiceDetails.Select((d, idx) => new {
                Line  = idx,
                Name  = d.Medicine?.MedicineName ?? d.Service?.ServiceName ?? "—",
                Qty   = d.Quantity,
                Total = d.Quantity * (d.UnitPrice)
            }).ToList()
        });

        return Ok(result);
    }
}

public record BookRequest(int DoctorId, int ServiceId, string Date, string Time, string? Note);
