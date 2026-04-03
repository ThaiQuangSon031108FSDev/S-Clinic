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
    public IActionResult Book([FromQuery] int treatmentId = 0)
    {
        ViewBag.TreatmentId = treatmentId > 0 ? treatmentId : (int?)null;
        return View();
    }

    // POST /Patient/Book — create appointment
    [HttpPost]
    public async Task<IActionResult> Book([FromBody] BookRequest? req)
    {
        if (req is null)
            return Json(new { success = false, message = "Dữ liệu đặt lịch không hợp lệ." });

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

        // ── Auto-assign: tìm bác sĩ có lịch + còn chỗ trống ─────────────────
        var schedule = await db.DoctorSchedules
            .Where(s => s.WorkDate == workDate
                     && s.TimeSlot  == timeSlot
                     && s.CurrentBooked < s.MaxPatients)
            .OrderBy(s => s.CurrentBooked)   // ưu tiên bác sĩ ít khách nhất
            .FirstOrDefaultAsync();

        if (schedule is null)
            return Json(new { success = false,
                message = "Không có bác sĩ nào có lịch trống vào khung giờ này Vui lòng chọn ngày/giờ khác." });

        // ── Kiểm tra chưa đặt trùng ──────────────────────────────────────────
        var alreadyBooked = await db.Appointments.AnyAsync(a =>
            a.PatientId  == patient.PatientId &&
            a.ScheduleId == schedule.ScheduleId &&
            a.Status     != AppointmentStatus.Cancelled);
        if (alreadyBooked)
            return Json(new { success = false, message = "Bạn đã đặt lịch vào khung giờ này rồi." });

        // ── Tạo lịch hẹn ─────────────────────────────────────────────────────
        var appt = new Appointment
        {
            PatientId           = patient.PatientId,
            ScheduleId          = schedule.ScheduleId,
            ServiceId           = req.ServiceId is > 0 ? req.ServiceId : null,
            Notes               = req.Note?.Trim(),
            PatientTreatmentId  = req.PatientTreatmentId > 0 ? req.PatientTreatmentId : null,
            Status              = AppointmentStatus.Pending,
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

    // GET /api/patient/invoices — returns ALL invoices for the logged-in patient
    [HttpGet("/api/patient/invoices")]
    public async Task<IActionResult> MyInvoices()
    {
        var accountId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var patient = await db.Patients.FirstOrDefaultAsync(p => p.AccountId == accountId);
        if (patient is null) return Ok(Array.Empty<object>());

        // Collect all PatientTreatmentIds belonging to this patient
        var patientTreatmentIds = await db.PatientTreatments
            .Where(pt => pt.PatientId == patient.PatientId)
            .Select(pt => pt.PackageId)
            .ToListAsync();

        var invoices = await db.Invoices
            .Include(i => i.Record)
                .ThenInclude(r => r!.Appointment)
            .Include(i => i.Appointment)
            .Include(i => i.InvoiceDetails)
                .ThenInclude(d => d.Medicine)
            .Include(i => i.InvoiceDetails)
                .ThenInclude(d => d.Service)
            .Include(i => i.InvoiceDetails)
                .ThenInclude(d => d.Package)  // for treatment package invoices
            .Where(i =>
                // Type 1: From completed appointment (doctor filled medical record)
                (i.Record != null && i.Record.Appointment != null && i.Record.Appointment.PatientId == patient.PatientId)
                // Type 2: Direct appointment invoice (no record yet)
                || (i.Appointment != null && i.Appointment.PatientId == patient.PatientId)
                // Type 3: Treatment package purchase (RecordId=null, AppointmentId=null, but PackageId in details)
                || i.InvoiceDetails.Any(d => d.ItemType == InvoiceItemType.Package && d.PackageId != null && patientTreatmentIds.Contains(d.PackageId!.Value))
            )
            .OrderByDescending(i => i.CreatedDate)
            .ToListAsync();

        // Deduplicate: same invoice can match multiple clauses
        invoices = invoices.DistinctBy(i => i.InvoiceId).ToList();

        var result = invoices.Select(i => new {
            i.InvoiceId,
            i.TotalAmount,
            Status      = i.PaymentStatus.ToString(),
            CreatedDate = i.CreatedDate.ToString("dd/MM/yyyy HH:mm"),
            Details     = i.InvoiceDetails.Select((d, idx) => new {
                Line  = idx,
                Name  = d.ItemType == InvoiceItemType.Package
                        ? (d.Package?.PackageName ?? "Gói liệu trình")
                        : d.Medicine?.MedicineName ?? d.Service?.ServiceName ?? "—",
                Qty   = d.Quantity,
                Total = d.Quantity * d.UnitPrice
            }).ToList()
        });

        return Ok(result);
    }
}

public record BookRequest(int DoctorId, int? ServiceId, string Date, string Time, string? Note, int PatientTreatmentId = 0);
