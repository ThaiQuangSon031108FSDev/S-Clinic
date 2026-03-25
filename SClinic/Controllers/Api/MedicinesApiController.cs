using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SClinic.Data;
using SClinic.Models;

namespace SClinic.Controllers.Api;

[ApiController, Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class MedicinesApiController(ApplicationDbContext db) : ControllerBase
{
    // GET api/medicinesapi
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await db.Medicines
            .Include(m => m.Category)
            .Where(m => !m.IsDeleted)
            .OrderBy(m => m.MedicineName)
            .Select(m => new
            {
                m.MedicineId,
                Name       = m.MedicineName,
                Category   = m.Category.CategoryName,
                CategoryId = m.CategoryId,
                m.StockQuantity,
                Price      = m.Price.ToString("N0") + "đ",
                PriceRaw   = m.Price,
                IsLow      = m.StockQuantity <= 10
            })
            .ToListAsync();

        return Ok(list);
    }

    // GET api/medicinesapi/categories
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var cats = await db.Categories
            .OrderBy(c => c.CategoryName)
            .Select(c => new { c.CategoryId, c.CategoryName, c.Description })
            .ToListAsync();
        return Ok(cats);
    }

    // POST api/medicinesapi — create new medicine
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] MedicineCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || dto.CategoryId <= 0)
            return BadRequest("Tên và danh mục không được để trống.");
        if (dto.StockQuantity < 0) return BadRequest("Tồn kho không được âm.");
        if (dto.Price < 0) return BadRequest("Đơn giá không được âm.");

        var med = new Medicine
        {
            MedicineName  = dto.Name.Trim(),
            CategoryId    = dto.CategoryId,
            StockQuantity = dto.StockQuantity,
            Price         = dto.Price,
        };
        db.Medicines.Add(med);
        await db.SaveChangesAsync();
        return Ok(new { med.MedicineId });
    }

    // PUT api/medicinesapi/{id} — update medicine
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] MedicineCreateDto dto)
    {
        if (dto.Price < 0) return BadRequest("Đơn giá không được âm.");
        var med = await db.Medicines.FindAsync(id);
        if (med is null) return NotFound();

        med.MedicineName = dto.Name.Trim();
        med.CategoryId   = dto.CategoryId;
        med.Price        = dto.Price;
        await db.SaveChangesAsync();
        return Ok(new { success = true });
    }

    // PATCH api/medicinesapi/{id}/restock
    [HttpPatch("{id}/restock")]
    public async Task<IActionResult> Restock(int id, [FromBody] RestockDto dto)
    {
        if (dto.Qty <= 0) return BadRequest("Số lượng phải lớn hơn 0.");
        var med = await db.Medicines.FindAsync(id);
        if (med is null) return NotFound();

        med.StockQuantity += dto.Qty;
        await db.SaveChangesAsync();
        return Ok(new { id, newStock = med.StockQuantity });
    }

    // DELETE api/medicinesapi/{id} — soft delete (Bug #14)
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var med = await db.Medicines.FindAsync(id);
        if (med is null) return NotFound();
        med.IsDeleted = true;
        await db.SaveChangesAsync();
        return Ok(new { success = true });
    }
}

public record RestockDto(int Qty);
public record MedicineCreateDto(string Name, int CategoryId, int StockQuantity, decimal Price);
