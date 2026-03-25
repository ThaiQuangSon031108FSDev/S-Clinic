using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SClinic.Data;
using SClinic.Models;
using System.Security.Claims;

namespace SClinic.Controllers.Api;

[ApiController, Route("api/doctor")]
[Authorize(Roles = "Doctor,Admin")]
public class DoctorApiController(ApplicationDbContext db, IWebHostEnvironment env) : ControllerBase
{
    // ── GET /api/doctor/today-appointments ──────────────────────────────
    [HttpGet("today-appointments")]
    public async Task<IActionResult> TodayAppointments()
    {
        var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var doctor    = await db.Doctors.FirstOrDefaultAsync(d => d.AccountId == accountId);
        if (doctor is null) return Ok(new List<object>());

        var today = DateOnly.FromDateTime(DateTime.Today);
        var list  = await db.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Schedule)
            .Include(a => a.PatientTreatment).ThenInclude(pt => pt!.Package)
            .Include(a => a.MedicalRecord)
            .Where(a => a.Schedule.DoctorId == doctor.DoctorId
                     && a.Schedule.WorkDate == today
                     && a.Status != AppointmentStatus.Cancelled)
            .OrderBy(a => a.Schedule.TimeSlot)
            .Select(a => new
            {
                appointmentId  = a.AppointmentId,
                patientName    = a.Patient.FullName,
                patientId      = a.Patient.PatientId,
                phone          = a.Patient.Phone,
                age            = a.Patient.DateOfBirth.HasValue
                    ? DateTime.Today.Year - a.Patient.DateOfBirth.Value.Year : (int?)null,
                allergy        = a.Patient.BaseMedicalHistory,
                packageName    = a.PatientTreatment != null ? a.PatientTreatment.Package.PackageName : null,
                usedSessions   = a.PatientTreatment != null ? a.PatientTreatment.UsedSessions : (int?)null,
                totalSessions  = a.PatientTreatment != null ? a.PatientTreatment.TotalSessions : (int?)null,
                status         = a.Status.ToString(),
                timeSlot       = a.Schedule.TimeSlot.ToString("HH:mm"),
                hasRecord      = a.MedicalRecord != null,
                recordId       = a.MedicalRecord != null ? a.MedicalRecord.RecordId : (int?)null,
            })
            .ToListAsync();

        return Ok(list);
    }

    // ── GET /api/doctor/week-appointments ────────────────────────────────
    [HttpGet("week-appointments")]
    public async Task<IActionResult> WeekAppointments()
    {
        var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var doctor    = await db.Doctors.FirstOrDefaultAsync(d => d.AccountId == accountId);
        if (doctor is null) return Ok(new List<object>());

        // Current week Mon–Sun
        var today    = DateOnly.FromDateTime(DateTime.Today);
        var dayOfWeek = (int)today.DayOfWeek;  // 0=Sun
        var monday   = today.AddDays(dayOfWeek == 0 ? -6 : 1 - dayOfWeek);
        var sunday   = monday.AddDays(6);

        var list = await db.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Schedule)
            .Include(a => a.PatientTreatment).ThenInclude(pt => pt!.Package)
            .Where(a => a.Schedule.DoctorId == doctor.DoctorId
                     && a.Schedule.WorkDate >= monday
                     && a.Schedule.WorkDate <= sunday
                     && a.Status != AppointmentStatus.Cancelled)
            .OrderBy(a => a.Schedule.WorkDate)
            .ThenBy(a => a.Schedule.TimeSlot)
            .Select(a => new
            {
                appointmentId = a.AppointmentId,
                patientName   = a.Patient.FullName,
                date          = a.Schedule.WorkDate.ToString("yyyy-MM-dd"),
                timeSlot      = a.Schedule.TimeSlot.ToString("HH:mm"),
                packageName   = a.PatientTreatment != null ? a.PatientTreatment.Package.PackageName : null,
                status        = a.Status.ToString()
            })
            .ToListAsync();

        return Ok(list);
    }

    // ── GET /api/doctor/inventory ───────────────────────────────────────
    [HttpGet("inventory")]
    public async Task<IActionResult> GetInventory()
    {
        var services = await db.Services
            .Select(s => new { Id = s.ServiceId, Name = s.ServiceName, Type = "service" })
            .ToListAsync();

        var medicines = await db.Medicines
            .Include(m => m.Category)
            .Select(m => new { Id = m.MedicineId, Name = m.MedicineName, Type = "medicine" })
            .ToListAsync();

        return Ok(services.Concat(medicines));
    }

    // ── POST /api/doctor/complete-exam ──────────────────────────────────
    [HttpPost("complete-exam")]
    public async Task<IActionResult> CompleteExam([FromForm] CompleteExamRequest req)
    {
        var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var doctor    = await db.Doctors.FirstOrDefaultAsync(d => d.AccountId == accountId);
        if (doctor is null) return BadRequest(new { success = false, message = "Không tìm thấy bác sĩ." });

        var appt = await db.Appointments
            .Include(a => a.PatientTreatment)
            .FirstOrDefaultAsync(a => a.AppointmentId == req.AppointmentId);

        if (appt is null)
            return BadRequest(new { success = false, message = "Không tìm thấy lịch hẹn." });

        // ── 1. Save medical record ───────────────────────────────────────
        var record = await db.MedicalRecords
            .FirstOrDefaultAsync(r => r.AppointmentId == req.AppointmentId);

        if (record is null)
        {
            record = new MedicalRecord
            {
                AppointmentId = req.AppointmentId,
                DoctorId      = doctor.DoctorId,
            };
            db.MedicalRecords.Add(record);
        }
        record.SkinCondition = req.SkinCondition;
        record.Diagnosis     = req.Diagnosis;
        record.RecordDate    = DateTime.Now;

        // ── 2. Upload photo (save to disk only) ─────────────────────────
        if (req.Photo is { Length: > 0 })
        {
            var uploads = Path.Combine(env.WebRootPath, "uploads", "session-images");
            Directory.CreateDirectory(uploads);
            var ext      = Path.GetExtension(req.Photo.FileName);
            var fileName = $"appt-{req.AppointmentId}-{DateTime.Now:yyyyMMddHHmmss}{ext}";
            await using var fs = new FileStream(Path.Combine(uploads, fileName), FileMode.Create);
            await req.Photo.CopyToAsync(fs);
            // Full SessionImage save requires a TreatmentSessionLog FK — skip for now
        }

        // ── 3. Mark appointment completed ────────────────────────────────
        appt.Status = AppointmentStatus.Completed;

        // ── 4. Increment PatientTreatment.UsedSessions if linked ────────
        if (appt.PatientTreatment is not null)
            appt.PatientTreatment.UsedSessions++;

        await db.SaveChangesAsync();

        // ── 5. Build invoice if items prescribed ─────────────────────────
        if (req.Items is { Count: > 0 })
        {
            var invoice = new Invoice
            {
                RecordId      = record.RecordId,
                PaymentStatus = PaymentStatus.Pending,
                CreatedDate   = DateTime.Now,
                TotalAmount   = 0,
            };
            db.Invoices.Add(invoice);
            await db.SaveChangesAsync();

            decimal total = 0;
            foreach (var item in req.Items)
            {
                decimal unitPrice = 0;

                if (item.Type == "medicine" && item.MedicineId.HasValue)
                {
                    var med = await db.Medicines.FindAsync(item.MedicineId);
                    unitPrice = med?.Price ?? 0;
                }
                else if (item.Type == "service" && item.ServiceId.HasValue)
                {
                    var svc = await db.Services.FindAsync(item.ServiceId);
                    unitPrice = svc?.Price ?? 0;
                }

                var qty      = item.Qty > 0 ? item.Qty : 1;
                var subTotal = unitPrice * qty;
                total       += subTotal;

                db.InvoiceDetails.Add(new InvoiceDetail
                {
                    InvoiceId  = invoice.InvoiceId,
                    ItemType   = item.Type == "medicine" ? InvoiceItemType.Medicine : InvoiceItemType.Service,
                    MedicineId = item.MedicineId,
                    ServiceId  = item.ServiceId,
                    Quantity   = qty,
                    UnitPrice  = unitPrice,
                    SubTotal   = subTotal,
                });
            }

            invoice.TotalAmount = total;
            await db.SaveChangesAsync();
        }

        return Ok(new { success = true, recordId = record.RecordId });
    }
}

// ── DTOs ─────────────────────────────────────────────────────────────────
public class CompleteExamRequest
{
    public int    AppointmentId  { get; set; }
    public string SkinCondition  { get; set; } = "";
    public string Diagnosis      { get; set; } = "";
    public IFormFile? Photo      { get; set; }
    public List<PrescriptionItem>? Items { get; set; }
}

public class PrescriptionItem
{
    public string  Type       { get; set; } = "medicine";
    public int?    MedicineId { get; set; }
    public int?    ServiceId  { get; set; }
    public int     Qty        { get; set; } = 1;
}

