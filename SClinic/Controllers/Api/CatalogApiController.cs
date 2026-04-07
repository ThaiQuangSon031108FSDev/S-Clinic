using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SClinic.Data;
using SClinic.Models;

namespace SClinic.Controllers.Api;

/// <summary>Admin CRUD for Categories, Services, TreatmentPackages.</summary>
[ApiController, Route("api/catalog")]
[Authorize(Roles = "Admin")]
public class CatalogApiController(ApplicationDbContext db) : ControllerBase
{
    // ══════════════════════════════════════════════════════════════════════════
    // CATEGORIES
    // ══════════════════════════════════════════════════════════════════════════

    // GET /api/catalog/categories
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var list = await db.Categories
            .Select(c => new
            {
                c.CategoryId,
                c.CategoryName,
                c.Description,
                MedicineCount = c.Medicines.Count
            })
            .OrderBy(c => c.CategoryName)
            .ToListAsync();
        return Ok(list);
    }

    // POST /api/catalog/categories
    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory([FromBody] CategoryDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CategoryName))
            return BadRequest(new { success = false, message = "Tên danh mục không được để trống." });

        var cat = new Category { CategoryName = dto.CategoryName.Trim(), Description = dto.Description?.Trim() };
        db.Categories.Add(cat);
        await db.SaveChangesAsync();
        return Ok(new { success = true, categoryId = cat.CategoryId });
    }

    // PUT /api/catalog/categories/{id}
    [HttpPut("categories/{id:int}")]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryDto dto)
    {
        var cat = await db.Categories.FindAsync(id);
        if (cat is null) return NotFound(new { success = false, message = "Không tìm thấy danh mục." });

        if (string.IsNullOrWhiteSpace(dto.CategoryName))
            return BadRequest(new { success = false, message = "Tên danh mục không được để trống." });

        cat.CategoryName = dto.CategoryName.Trim();
        cat.Description  = dto.Description?.Trim();
        await db.SaveChangesAsync();
        return Ok(new { success = true });
    }

    // DELETE /api/catalog/categories/{id} — protected: cannot delete if has medicines
    [HttpDelete("categories/{id:int}")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var cat = await db.Categories
            .Include(c => c.Medicines)
            .FirstOrDefaultAsync(c => c.CategoryId == id);
        if (cat is null) return NotFound(new { success = false, message = "Không tìm thấy danh mục." });

        if (cat.Medicines.Any())
            return BadRequest(new
            {
                success = false,
                message = $"Không thể xóa — danh mục còn {cat.Medicines.Count} loại thuốc/sản phẩm."
            });

        db.Categories.Remove(cat);
        await db.SaveChangesAsync();
        return Ok(new { success = true });
    }

    // ══════════════════════════════════════════════════════════════════════════
    // SERVICES
    // ══════════════════════════════════════════════════════════════════════════

    // GET /api/catalog/services
    [HttpGet("services")]
    [AllowAnonymous] // used by booking page too
    public async Task<IActionResult> GetServices()
    {
        var list = await db.Services
            .OrderBy(s => s.ServiceName)
            .Select(s => new
            {
                s.ServiceId,
                s.ServiceName,
                s.Price,
                ServiceType = s.ServiceType.ToString()
            })
            .ToListAsync();
        return Ok(list);
    }

    // POST /api/catalog/services
    [HttpPost("services")]
    public async Task<IActionResult> CreateService([FromBody] ServiceDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ServiceName))
            return BadRequest(new { success = false, message = "Tên dịch vụ không được để trống." });
        if (dto.Price < 0)
            return BadRequest(new { success = false, message = "Giá tiền không được âm." });

        var svc = new Service
        {
            ServiceName = dto.ServiceName.Trim(),
            Price       = dto.Price,
            ServiceType = Enum.TryParse<ServiceType>(dto.ServiceType, out var t) ? t : ServiceType.Consultation
        };
        db.Services.Add(svc);
        await db.SaveChangesAsync();
        return Ok(new { success = true, serviceId = svc.ServiceId });
    }

    // PUT /api/catalog/services/{id}
    [HttpPut("services/{id:int}")]
    public async Task<IActionResult> UpdateService(int id, [FromBody] ServiceDto dto)
    {
        var svc = await db.Services.FindAsync(id);
        if (svc is null) return NotFound(new { success = false, message = "Không tìm thấy dịch vụ." });
        if (string.IsNullOrWhiteSpace(dto.ServiceName))
            return BadRequest(new { success = false, message = "Tên dịch vụ không được để trống." });
        if (dto.Price < 0)
            return BadRequest(new { success = false, message = "Giá tiền không được âm." });

        svc.ServiceName = dto.ServiceName.Trim();
        svc.Price       = dto.Price;
        if (Enum.TryParse<ServiceType>(dto.ServiceType, out var t)) svc.ServiceType = t;
        await db.SaveChangesAsync();
        return Ok(new { success = true });
    }

    // DELETE /api/catalog/services/{id}
    [HttpDelete("services/{id:int}")]
    public async Task<IActionResult> DeleteService(int id)
    {
        var svc = await db.Services
            .Include(s => s.InvoiceDetails)
            .FirstOrDefaultAsync(s => s.ServiceId == id);
        if (svc is null) return NotFound(new { success = false, message = "Không tìm thấy dịch vụ." });

        if (svc.InvoiceDetails.Any())
            return BadRequest(new { success = false, message = "Không thể xóa — dịch vụ đang có trong hóa đơn." });

        db.Services.Remove(svc);
        await db.SaveChangesAsync();
        return Ok(new { success = true });
    }

    // ══════════════════════════════════════════════════════════════════════════
    // TREATMENT PACKAGES
    // ══════════════════════════════════════════════════════════════════════════

    // GET /api/catalog/packages
    [HttpGet("packages")]
    public async Task<IActionResult> GetPackages()
    {
        var list = await db.TreatmentPackages
            .OrderBy(p => p.PackageName)
            .Select(p => new
            {
                p.PackageId,
                p.PackageName,
                p.TotalSessions,
                p.Price,
                PatientCount = p.PatientTreatments.Count
            })
            .ToListAsync();
        return Ok(list);
    }

    // POST /api/catalog/packages
    [HttpPost("packages")]
    public async Task<IActionResult> CreatePackage([FromBody] PackageDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.PackageName))
            return BadRequest(new { success = false, message = "Tên gói không được để trống." });
        if (dto.Price < 0)
            return BadRequest(new { success = false, message = "Giá tiền không được âm." });
        if (dto.TotalSessions <= 0)
            return BadRequest(new { success = false, message = "Số buổi phải lớn hơn 0." });

        var pkg = new TreatmentPackage
        {
            PackageName   = dto.PackageName.Trim(),
            TotalSessions = dto.TotalSessions,
            Price         = dto.Price
        };
        db.TreatmentPackages.Add(pkg);
        await db.SaveChangesAsync();
        return Ok(new { success = true, packageId = pkg.PackageId });
    }

    // PUT /api/catalog/packages/{id}
    [HttpPut("packages/{id:int}")]
    public async Task<IActionResult> UpdatePackage(int id, [FromBody] PackageDto dto)
    {
        var pkg = await db.TreatmentPackages.FindAsync(id);
        if (pkg is null) return NotFound(new { success = false, message = "Không tìm thấy gói." });
        if (string.IsNullOrWhiteSpace(dto.PackageName))
            return BadRequest(new { success = false, message = "Tên gói không được để trống." });
        if (dto.Price < 0)
            return BadRequest(new { success = false, message = "Giá tiền không được âm." });
        if (dto.TotalSessions <= 0)
            return BadRequest(new { success = false, message = "Số buổi phải lớn hơn 0." });

        pkg.PackageName   = dto.PackageName.Trim();
        pkg.TotalSessions = dto.TotalSessions;
        pkg.Price         = dto.Price;
        await db.SaveChangesAsync();
        return Ok(new { success = true });
    }

    // DELETE /api/catalog/packages/{id}
    [HttpDelete("packages/{id:int}")]
    public async Task<IActionResult> DeletePackage(int id)
    {
        var pkg = await db.TreatmentPackages
            .Include(p => p.PatientTreatments)
            .FirstOrDefaultAsync(p => p.PackageId == id);
        if (pkg is null) return NotFound(new { success = false, message = "Không tìm thấy gói." });

        if (pkg.PatientTreatments.Any())
            return BadRequest(new { success = false, message = "Không thể xóa — đã có bệnh nhân đăng ký gói này." });

        db.TreatmentPackages.Remove(pkg);
        await db.SaveChangesAsync();
        return Ok(new { success = true });
    }
}

// ── DTOs ──────────────────────────────────────────────────────────────────────
public record CategoryDto(string CategoryName, string? Description);
public record ServiceDto(string ServiceName, decimal Price, string ServiceType);
public record PackageDto(string PackageName, int TotalSessions, decimal Price);
