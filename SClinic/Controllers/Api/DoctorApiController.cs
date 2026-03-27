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
    // ── GET /api/doctor/appointment-detail/{id} — Unified form data ──────────
    [HttpGet("appointment-detail/{id}")]
    public async Task<IActionResult> AppointmentDetail(int id)
    {
        var appt = await db.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Schedule)
            .Include(a => a.Service)
            .Include(a => a.PatientTreatment).ThenInclude(pt => pt!.Package)
            .Include(a => a.MedicalRecord)
            .FirstOrDefaultAsync(a => a.AppointmentId == id);

        if (appt is null) return NotFound(new { message = "Không tìm thấy lịch hẹn." });

        var isPackage = appt.PatientTreatmentId.HasValue;

        return Ok(new
        {
            appointmentId  = appt.AppointmentId,
            status         = appt.Status.ToString(),
            patientName    = appt.Patient.FullName,
            patientPhone   = appt.Patient.Phone,
            dob            = appt.Patient.DateOfBirth?.ToString("dd/MM/yyyy"),
            medHistory     = appt.Patient.BaseMedicalHistory,
            workDate       = appt.Schedule.WorkDate.ToString("dd/MM/yyyy"),
            timeSlot       = appt.Schedule.TimeSlot.ToString("HH:mm"),
            isPackage,
            // Service lẻ
            serviceId      = appt.ServiceId,
            serviceName    = appt.Service?.ServiceName,
            serviceType    = (int)(appt.Service?.ServiceType ?? ServiceType.Consultation),
            // Liệu trình
            treatmentId    = appt.PatientTreatmentId,
            packageName    = appt.PatientTreatment?.Package?.PackageName,
            currentSession = appt.PatientTreatment?.UsedSessions ?? 0,
            totalSessions  = appt.PatientTreatment?.TotalSessions ?? 0,
            // Existing notes
            skinCondition  = appt.MedicalRecord?.SkinCondition,
            diagnosis      = appt.MedicalRecord?.Diagnosis,
            hasRecord      = appt.MedicalRecord is not null,
        });
    }

    // ── POST /api/doctor/complete-session/{appointmentId} ────────────────────
    [HttpPost("complete-session/{appointmentId}")]
    public async Task<IActionResult> CompleteSession(int appointmentId,
        [FromForm] string? notes,
        [FromForm] string? prescriptionJson,
        [FromForm] IFormFile? imageBefore,
        [FromForm] IFormFile? imageAfter)
    {
        var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var doctor    = await db.Doctors.FirstOrDefaultAsync(d => d.AccountId == accountId);
        if (doctor is null)
            return BadRequest(new { success = false, message = "Không tìm thấy bác sĩ." });

        var appt = await db.Appointments
            .Include(a => a.PatientTreatment)
            .Include(a => a.MedicalRecord)
            .Include(a => a.Service)
            .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

        if (appt is null)
            return NotFound(new { success = false, message = "Không tìm thấy lịch hẹn." });
        if (appt.Status == AppointmentStatus.Completed)
            return BadRequest(new { success = false, message = "Buổi khám này đã hoàn tất." });

        //── 1. Save photos to disk ───────────────────────────────────────────
        var uploads = Path.Combine(env.WebRootPath, "uploads", "session-images");
        Directory.CreateDirectory(uploads);

        async Task<string?> SaveFile(IFormFile? file, string label)
        {
            if (file is not { Length: > 0 }) return null;
            var ext  = Path.GetExtension(file.FileName);
            var name = $"appt-{appointmentId}-{label}-{DateTime.Now:yyyyMMddHHmmss}{ext}";
            await using var fs = new FileStream(Path.Combine(uploads, name), FileMode.Create);
            await file.CopyToAsync(fs);
            return $"/uploads/session-images/{name}";
        }

        var urlBefore = await SaveFile(imageBefore, "before");
        var urlAfter  = await SaveFile(imageAfter,  "after");

        // ── 2. Create TreatmentSessionLog (works for both package & service) ─
        TreatmentSessionLog? log = null;
        if (appt.PatientTreatmentId.HasValue)
        {
            log = new TreatmentSessionLog
            {
                PatientTreatmentId = appt.PatientTreatmentId.Value,
                PerformedBy        = accountId,
                SessionNotes       = notes,
                UsedDate           = DateTime.Now,
            };
            db.TreatmentSessionLogs.Add(log);
            await db.SaveChangesAsync();

            if (urlBefore is not null)
                db.SessionImages.Add(new SessionImage { LogId = log.LogId, ImageUrl = urlBefore });
            if (urlAfter is not null)
                db.SessionImages.Add(new SessionImage { LogId = log.LogId, ImageUrl = urlAfter });

            // ── 3. Increment session count, auto-complete if done ────────────
            var pt = appt.PatientTreatment!;
            pt.UsedSessions++;
            if (pt.UsedSessions >= pt.TotalSessions)
                pt.Status = TreatmentStatus.Completed;
        }
        else
        {
            // Service lẻ: save as MedicalRecord notes
            if (appt.MedicalRecord is null)
            {
                appt.MedicalRecord = new MedicalRecord
                {
                    AppointmentId = appointmentId,
                    DoctorId      = doctor.DoctorId,
                };
                db.MedicalRecords.Add(appt.MedicalRecord);
            }
            appt.MedicalRecord.SkinCondition = notes;
            appt.MedicalRecord.RecordDate    = DateTime.Now;
        }

        // ── 4. Mark appointment Completed ────────────────────────────────────
        appt.Status = AppointmentStatus.Completed;
        await db.SaveChangesAsync();

        // ── 5. Create Invoice with prescription items ──────────────────────────
        var invoiceExists = await db.Invoices.AnyAsync(i => i.AppointmentId == appointmentId);
        if (!invoiceExists)
        {
            // Base service price from booked appointment
            decimal basePrice = 0;
            SClinic.Models.Service? bookedSvc = null;
            if (appt.ServiceId.HasValue)
            {
                bookedSvc = await db.Services.FindAsync(appt.ServiceId.Value);
                basePrice = bookedSvc?.Price ?? 0;
            }

            // Parse doctor's prescription
            List<PrescriptionItem>? items = null;
            if (!string.IsNullOrWhiteSpace(prescriptionJson))
            {
                try
                {
                    items = System.Text.Json.JsonSerializer.Deserialize<List<PrescriptionItem>>(
                        prescriptionJson,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch { /* ignore malformed JSON */ }
            }

            var details = new List<InvoiceDetail>();
            decimal prescriptionTotal = 0;

            // Add base service line
            if (bookedSvc is not null)
                details.Add(new InvoiceDetail
                {
                    ItemType  = InvoiceItemType.Service,
                    ServiceId = bookedSvc.ServiceId,
                    Quantity  = 1, UnitPrice = basePrice, SubTotal = basePrice,
                });

            // Add prescribed items
            foreach (var item in items ?? [])
            {
                decimal up = 0;
                if (item.Type == "medicine" && item.MedicineId.HasValue)
                {
                    var med = await db.Medicines.FindAsync(item.MedicineId);
                    up = med?.Price ?? 0;
                }
                else if (item.Type == "service" && item.ServiceId.HasValue)
                {
                    var svc = await db.Services.FindAsync(item.ServiceId);
                    up = svc?.Price ?? 0;
                }
                var qty = item.Qty > 0 ? item.Qty : 1;
                var sub = up * qty;
                prescriptionTotal += sub;
                details.Add(new InvoiceDetail
                {
                    ItemType   = item.Type == "medicine" ? InvoiceItemType.Medicine : InvoiceItemType.Service,
                    MedicineId = item.MedicineId,
                    ServiceId  = item.ServiceId,
                    Quantity   = qty, UnitPrice = up, SubTotal = sub,
                });
            }

            var invoice = new Invoice
            {
                RecordId      = appt.MedicalRecord?.RecordId,
                AppointmentId = appointmentId,
                PaymentStatus = PaymentStatus.Pending,
                TotalAmount   = basePrice + prescriptionTotal,
                CreatedDate   = DateTime.Now,
            };
            db.Invoices.Add(invoice);
            await db.SaveChangesAsync();

            foreach (var d in details)
            {
                d.InvoiceId = invoice.InvoiceId;
                db.InvoiceDetails.Add(d);
            }
            await db.SaveChangesAsync();
        }

        return Ok(new { success = true, message = "Đã hoàn tất buổi điều trị và chuyển sang Thu Ngân." });
    }

    // ── GET /api/doctor/appointment/{id} ─────────────────────────────────────
    [HttpGet("appointment/{id}")]
    public async Task<IActionResult> GetAppointment(int id)
    {
        var appt = await db.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Schedule).ThenInclude(s => s.Doctor)
            .Include(a => a.PatientTreatment).ThenInclude(pt => pt!.Package)
            .Include(a => a.MedicalRecord)
            .Include(a => a.Service) // load booked service for type check
            .FirstOrDefaultAsync(a => a.AppointmentId == id);

        if (appt is null) return NotFound(new { message = "Không tìm thấy lịch hẹn." });

        return Ok(new
        {
            appointmentId  = appt.AppointmentId,
            status         = appt.Status.ToString(),
            notes          = appt.Notes,
            timeSlot       = appt.Schedule.TimeSlot.ToString("HH:mm"),
            workDate       = appt.Schedule.WorkDate.ToString("dd/MM/yyyy"),
            doctorName     = appt.Schedule.Doctor.FullName,
            patientId      = appt.Patient.PatientId,
            patientName    = appt.Patient.FullName,
            phone          = appt.Patient.Phone,
            dob            = appt.Patient.DateOfBirth?.ToString("dd/MM/yyyy"),
            age            = appt.Patient.DateOfBirth.HasValue
                             ? DateTime.Today.Year - appt.Patient.DateOfBirth.Value.Year
                             : (int?)null,
            medHistory     = appt.Patient.BaseMedicalHistory,
            packageName    = appt.PatientTreatment?.Package?.PackageName,
            usedSessions   = appt.PatientTreatment?.UsedSessions,
            totalSessions  = appt.PatientTreatment?.TotalSessions,
            hasRecord      = appt.MedicalRecord is not null,
            skinCondition  = appt.MedicalRecord?.SkinCondition,
            diagnosis      = appt.MedicalRecord?.Diagnosis,
            serviceId      = appt.ServiceId,
            serviceName    = appt.Service?.ServiceName,
            serviceType    = (int)(appt.Service?.ServiceType ?? SClinic.Models.ServiceType.Consultation),
        });
    }

    // ── POST /api/doctor/medical-record/{appointmentId} ───────────────────────
    [HttpPost("medical-record/{appointmentId}")]
    public async Task<IActionResult> SaveMedicalRecord(int appointmentId, [FromBody] SaveRecordDto dto)
    {
        var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var doctor    = await db.Doctors.FirstOrDefaultAsync(d => d.AccountId == accountId);
        if (doctor is null) return BadRequest(new { success = false, message = "Không tìm thấy bác sĩ." });

        var appt = await db.Appointments
            .Include(a => a.PatientTreatment)
            .Include(a => a.MedicalRecord)
            .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

        if (appt is null) return NotFound(new { success = false, message = "Không tìm thấy lịch hẹn." });
        if (appt.Status == AppointmentStatus.Cancelled)
            return BadRequest(new { success = false, message = "Lịch hẹn đã bị huỷ." });

        // 1. Upsert MedicalRecord
        if (appt.MedicalRecord is null)
        {
            appt.MedicalRecord = new MedicalRecord
            {
                AppointmentId = appointmentId,
                DoctorId      = doctor.DoctorId,
            };
            db.MedicalRecords.Add(appt.MedicalRecord);
        }
        appt.MedicalRecord.SkinCondition = dto.SkinCondition;
        appt.MedicalRecord.Diagnosis     = dto.Diagnosis;
        appt.MedicalRecord.RecordDate    = DateTime.Now;

        // 2. Mark appointment Completed
        appt.Status = AppointmentStatus.Completed;

        // 3. Increment sessions if linked to treatment package
        if (appt.PatientTreatment is not null)
            appt.PatientTreatment.UsedSessions++;

        await db.SaveChangesAsync();

        // 4. Create invoice — auto-populate ServiceId price from Appointment.ServiceId
        var existingInvoice = await db.Invoices
            .AnyAsync(i => i.AppointmentId == appointmentId);

        if (!existingInvoice)
        {
            // Look up the service price booked by the patient
            decimal servicePrice = 0;
            SClinic.Models.Service? bookedService = null;
            if (appt.ServiceId.HasValue)
            {
                bookedService = await db.Services.FindAsync(appt.ServiceId.Value);
                servicePrice  = bookedService?.Price ?? 0;
            }

            var invoice = new Invoice
            {
                RecordId      = appt.MedicalRecord.RecordId,
                AppointmentId = appointmentId,
                PaymentStatus = PaymentStatus.Pending,
                TotalAmount   = servicePrice,
                CreatedDate   = DateTime.Now,
            };
            db.Invoices.Add(invoice);
            await db.SaveChangesAsync();

            // Add an InvoiceDetail for the booked service so cashier sees the line item
            if (bookedService is not null)
            {
                db.InvoiceDetails.Add(new InvoiceDetail
                {
                    InvoiceId  = invoice.InvoiceId,
                    ItemType   = InvoiceItemType.Service,
                    ServiceId  = bookedService.ServiceId,
                    Quantity   = 1,
                    UnitPrice  = servicePrice,
                    SubTotal   = servicePrice,
                });
                await db.SaveChangesAsync();
            }
        }

        return Ok(new { success = true, message = "Đã lưu hồ sơ bệnh án và chuyển sang Thu Ngân." });
    }

    [HttpGet("inventory")]
    public async Task<IActionResult> GetInventory()
    {
        var services = await db.Services
            .Select(s => new { id = s.ServiceId, name = s.ServiceName, type = "service", price = s.Price, unit = "lần" })
            .ToListAsync();

        var medicines = await db.Medicines
            .Include(m => m.Category)
            .Where(m => !m.IsDeleted)
            .Select(m => new { id = m.MedicineId, name = m.MedicineName, type = "medicine", price = m.Price, unit = "viên", category = m.Category.CategoryName })
            .ToListAsync();

        return Ok(services.Concat<object>(medicines));
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

        // ── 5. Build invoice ALWAYS to signal Cashier that patient is done ──
        var invoice = new Invoice
        {
            RecordId      = record.RecordId,
            AppointmentId = req.AppointmentId,
            PaymentStatus = PaymentStatus.Pending,
            CreatedDate   = DateTime.Now,
            TotalAmount   = 0,
        };
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        decimal total = 0;

        if (req.Items is { Count: > 0 })
        {
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
        }

        invoice.TotalAmount = total;
        await db.SaveChangesAsync();

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

public class SaveRecordDto
{
    public string SkinCondition { get; set; } = "";
    public string Diagnosis     { get; set; } = "";
    public string Notes         { get; set; } = "";
}
