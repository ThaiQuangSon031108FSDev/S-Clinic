using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SClinic.Data;
using SClinic.Models;
using SClinic.Services.Interfaces;
using System.Security.Claims;

namespace SClinic.Controllers.Api;

[ApiController, Route("api/treatments")]
[Authorize]
public class TreatmentsApiController(
    ApplicationDbContext db,
    IWebHostEnvironment env,
    ITreatmentService treatmentSvc) : ControllerBase
{
    // ── GET /api/treatments/packages ─────────────────────────────────────────
    [HttpGet("packages")]
    [Authorize(Roles = "Receptionist,Admin,Doctor,Cashier")]
    public async Task<IActionResult> GetPackages()
    {
        var packages = await db.TreatmentPackages
            .Select(p => new
            {
                p.PackageId,
                p.PackageName,
                p.TotalSessions,
                p.Price
            })
            .ToListAsync();
        return Ok(packages);
    }

    // ── GET /api/treatments/doctors ──────────────────────────────────────────
    [HttpGet("doctors")]
    [Authorize(Roles = "Receptionist,Admin,Cashier")]
    public async Task<IActionResult> GetDoctors()
    {
        var doctors = await db.Doctors
            .Select(d => new { d.DoctorId, d.FullName, d.Specialty })
            .ToListAsync();
        return Ok(doctors);
    }

    // ── GET /api/treatments/patient/{patientId} ───────────────────────────────
    [HttpGet("patient/{patientId:int}")]
    [Authorize(Roles = "Receptionist,Admin,Doctor,Cashier")]
    public async Task<IActionResult> GetPatientTreatments(int patientId)
    {
        var list = await db.PatientTreatments
            .Include(pt => pt.Package)
            .Include(pt => pt.PrimaryDoctor)
            .Where(pt => pt.PatientId == patientId)
            .Select(pt => new
            {
                pt.PatientTreatmentId,
                PackageName      = pt.Package.PackageName,
                pt.TotalSessions,
                pt.UsedSessions,
                Remaining        = pt.TotalSessions - pt.UsedSessions,
                Status           = pt.Status.ToString(),
                pt.PrimaryDoctorId,
                PrimaryDoctorName = pt.PrimaryDoctor.FullName
            })
            .ToListAsync();
        return Ok(list);
    }

    // ── GET /api/treatments/my-active — Patient sticky routing ───────────────
    [HttpGet("my-active")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> MyActive()
    {
        var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var patient   = await db.Patients.FirstOrDefaultAsync(p => p.AccountId == accountId);
        if (patient is null) return Ok(Array.Empty<object>());

        var list = await db.PatientTreatments
            .Include(pt => pt.Package)
            .Include(pt => pt.PrimaryDoctor)
            .Where(pt => pt.PatientId == patient.PatientId
                      && pt.Status   == TreatmentStatus.Active)
            .Select(pt => new
            {
                pt.PatientTreatmentId,
                PackageName       = pt.Package.PackageName,
                pt.TotalSessions,
                pt.UsedSessions,
                Remaining         = pt.TotalSessions - pt.UsedSessions,
                pt.PrimaryDoctorId,
                PrimaryDoctorName = pt.PrimaryDoctor.FullName
            })
            .ToListAsync();
        return Ok(list);
    }

    // ── POST /api/treatments/sell — Receptionist bán gói, thu tiền ngay ──────
    [HttpPost("sell")]
    [Authorize(Roles = "Receptionist,Admin,Cashier")]
    public async Task<IActionResult> Sell([FromBody] SellPackageDto dto)
    {
        var package = await db.TreatmentPackages.FindAsync(dto.PackageId);
        if (package is null)
            return BadRequest(new { success = false, message = "Gói không tồn tại." });

        var patient = await db.Patients.FindAsync(dto.PatientId);
        if (patient is null)
            return BadRequest(new { success = false, message = "Bệnh nhân không tồn tại." });

        var doctor = await db.Doctors.FindAsync(dto.DoctorId);
        if (doctor is null)
            return BadRequest(new { success = false, message = "Bác sĩ không tồn tại." });

        await using var tx = await db.Database.BeginTransactionAsync();

        // 1. Create PatientTreatment
        var treatment = new PatientTreatment
        {
            PatientId       = dto.PatientId,
            PackageId       = dto.PackageId,
            PrimaryDoctorId = dto.DoctorId,
            TotalSessions   = package.TotalSessions,
            UsedSessions    = 0,
            Status          = TreatmentStatus.Active
        };
        db.PatientTreatments.Add(treatment);
        await db.SaveChangesAsync();

        // 2. Create Invoice — Paid immediately (thu ngay)
        var invoice = new Invoice
        {
            AppointmentId = null,
            RecordId      = null,
            TotalAmount   = package.Price,
            PaymentStatus = PaymentStatus.Paid,
            CreatedDate   = DateTime.Now
        };
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        db.InvoiceDetails.Add(new InvoiceDetail
        {
            InvoiceId = invoice.InvoiceId,
            ItemType  = InvoiceItemType.Package,
            PackageId = package.PackageId,
            Quantity  = 1,
            UnitPrice = package.Price,
            SubTotal  = package.Price
        });
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return Ok(new
        {
            success              = true,
            patientTreatmentId   = treatment.PatientTreatmentId,
            invoiceId            = invoice.InvoiceId,
            packageName          = package.PackageName,
            totalSessions        = package.TotalSessions
        });
    }

    // ── POST /api/treatments/doctor-assign — Bác sĩ đề xuất gói ─────────────
    [HttpPost("doctor-assign")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> DoctorAssign([FromBody] DoctorAssignDto dto)
    {
        var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var doctor    = await db.Doctors.FirstOrDefaultAsync(d => d.AccountId == accountId);
        if (doctor is null)
            return BadRequest(new { success = false, message = "Không tìm thấy bác sĩ." });

        var package = await db.TreatmentPackages.FindAsync(dto.PackageId);
        if (package is null)
            return BadRequest(new { success = false, message = "Gói không tồn tại." });

        await using var tx = await db.Database.BeginTransactionAsync();

        // 1. Create PatientTreatment
        var treatment = new PatientTreatment
        {
            PatientId       = dto.PatientId,
            PackageId       = dto.PackageId,
            PrimaryDoctorId = doctor.DoctorId,
            TotalSessions   = package.TotalSessions,
            UsedSessions    = 0,
            Status          = TreatmentStatus.Active
        };
        db.PatientTreatments.Add(treatment);
        await db.SaveChangesAsync();

        // 2. Create Invoice — Pending (thu ngân thu khi khách ra quầy)
        var invoice = new Invoice
        {
            AppointmentId = dto.AppointmentId,
            RecordId      = null,
            TotalAmount   = package.Price,
            PaymentStatus = PaymentStatus.Pending,
            CreatedDate   = DateTime.Now
        };
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        db.InvoiceDetails.Add(new InvoiceDetail
        {
            InvoiceId = invoice.InvoiceId,
            ItemType  = InvoiceItemType.Package,
            PackageId = package.PackageId,
            Quantity  = 1,
            UnitPrice = package.Price,
            SubTotal  = package.Price
        });
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return Ok(new
        {
            success            = true,
            patientTreatmentId = treatment.PatientTreatmentId,
            invoiceId          = invoice.InvoiceId
        });
    }

    // ── POST /api/treatments/{id}/use-session — Lễ tân trừ 1 buổi ───────────
    [HttpPost("{id:int}/use-session")]
    [Authorize(Roles = "Receptionist,Admin,Doctor")]
    public async Task<IActionResult> UseSession(int id, [FromBody] UseSessionDto dto)
    {
        var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var log = await treatmentSvc.LogSessionAsync(id, accountId, dto.Notes, []);
        if (log is null)
            return BadRequest(new
            {
                success = false,
                message = "Không thể trừ buổi. Liệu trình đã hết hoặc không ở trạng thái Active."
            });

        // Reload to return updated counts
        var pt = await db.PatientTreatments.FindAsync(id);
        return Ok(new
        {
            success      = true,
            logId        = log.LogId,
            usedSessions = pt!.UsedSessions,
            totalSessions = pt.TotalSessions,
            remaining    = pt.TotalSessions - pt.UsedSessions,
            isCompleted  = pt.Status == TreatmentStatus.Completed
        });
    }

    // ── POST /api/treatments/upload-image/{logId} ────────────────────────────
    [HttpPost("upload-image/{logId:int}")]
    [Authorize(Roles = "Doctor,Admin,Receptionist")]
    public async Task<IActionResult> UploadImage(int logId, [FromForm] IFormFile image, [FromForm] string? label)
    {
        var log = await db.TreatmentSessionLogs.FindAsync(logId);
        if (log is null)
            return NotFound(new { success = false, message = "Không tìm thấy nhật ký buổi." });

        if (image is not { Length: > 0 })
            return BadRequest(new { success = false, message = "Không có file." });

        // Security: whitelist extensions only
        var ext = Path.GetExtension(image.FileName).ToLowerInvariant();
        if (ext is not (".jpg" or ".jpeg" or ".png"))
            return BadRequest(new { success = false, message = "Chỉ chấp nhận file JPG, PNG." });

        var uploads  = Path.Combine(env.WebRootPath, "uploads", "session-images");
        Directory.CreateDirectory(uploads);

        var safeLabel = (label ?? "img").Replace("..", "").Replace("/", "");
        var fileName  = $"log-{logId}-{safeLabel}-{DateTime.Now:yyyyMMddHHmmss}{ext}";
        await using var fs = new FileStream(Path.Combine(uploads, fileName), FileMode.Create);
        await image.CopyToAsync(fs);

        var imgUrl = $"/uploads/session-images/{fileName}";
        db.SessionImages.Add(new SessionImage { LogId = logId, ImageUrl = imgUrl, UploadDate = DateTime.Now });
        await db.SaveChangesAsync();

        return Ok(new { success = true, imageUrl = imgUrl });
    }

    // ── GET /api/treatments/{id}/timeline ────────────────────────────────────
    [HttpGet("{id:int}/timeline")]
    [Authorize(Roles = "Doctor,Admin,Receptionist,Patient")]
    public async Task<IActionResult> Timeline(int id)
    {
        var sessionNumber = 0;
        var logs = await db.TreatmentSessionLogs
            .Include(l => l.SessionImages)
            .Include(l => l.PerformedByAccount)
            .Where(l => l.PatientTreatmentId == id)
            .OrderBy(l => l.UsedDate)
            .ToListAsync();

        var result = logs.Select(l => new
        {
            l.LogId,
            l.SessionNotes,
            UsedDate      = l.UsedDate.ToString("dd/MM/yyyy HH:mm"),
            SessionNumber = ++sessionNumber,
            PerformedBy   = l.PerformedByAccount.Email,
            Images        = l.SessionImages.Select(img => new
            {
                img.ImageId,
                img.ImageUrl
            }).ToList()
        });

        return Ok(result);
    }

    // ── GET /api/treatments/search-patient?phone=xxx ──────────────────────────
    [HttpGet("search-patient")]
    [Authorize(Roles = "Receptionist,Admin,Doctor")]
    public async Task<IActionResult> SearchPatient([FromQuery] string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return BadRequest(new { message = "Nhập số điện thoại." });

        var patient = await db.Patients
            .Where(p => p.Phone == phone)
            .Select(p => new { p.PatientId, p.FullName, p.Phone })
            .FirstOrDefaultAsync();

        if (patient is null)
            return NotFound(new { message = "Không tìm thấy bệnh nhân." });

        var treatments = await db.PatientTreatments
            .Include(pt => pt.Package)
            .Include(pt => pt.PrimaryDoctor)
            .Where(pt => pt.PatientId == patient.PatientId && pt.Status == TreatmentStatus.Active)
            .Select(pt => new
            {
                pt.PatientTreatmentId,
                PackageName       = pt.Package.PackageName,
                pt.TotalSessions,
                pt.UsedSessions,
                Remaining         = pt.TotalSessions - pt.UsedSessions,
                pt.PrimaryDoctorId,
                PrimaryDoctorName = pt.PrimaryDoctor.FullName
            })
            .ToListAsync();

        return Ok(new { patient, treatments });
    }
}

// ── DTOs ──────────────────────────────────────────────────────────────────────
public class SellPackageDto
{
    public int PatientId  { get; set; }
    public int PackageId  { get; set; }
    public int DoctorId   { get; set; }
}

public class DoctorAssignDto
{
    public int  PatientId     { get; set; }
    public int  PackageId     { get; set; }
    public int? AppointmentId { get; set; }
}

public class UseSessionDto
{
    public string? Notes { get; set; }
}
