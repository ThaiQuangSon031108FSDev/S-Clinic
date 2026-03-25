using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SClinic.Data;
using SClinic.Models;

namespace SClinic.Controllers.Api;

[ApiController, Route("api/[controller]")]
[Authorize(Roles = "Admin,Cashier,Receptionist")]
public class InvoicesApiController(ApplicationDbContext db) : ControllerBase
{
    // GET api/invoicesapi — all invoices (optionally filter by status)
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status = null)
    {
        var q = db.Invoices
            .Include(i => i.Record)
                .ThenInclude(r => r.Appointment)
                    .ThenInclude(a => a.Patient)
            .Include(i => i.InvoiceDetails)
                .ThenInclude(d => d.Medicine)
            .Include(i => i.InvoiceDetails)
                .ThenInclude(d => d.Service)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PaymentStatus>(status, true, out var ps))
            q = q.Where(i => i.PaymentStatus == ps);

        var list = await q
            .OrderByDescending(i => i.CreatedDate)
            .Select(i => new
            {
                i.InvoiceId,
                i.TotalAmount,
                Status      = i.PaymentStatus.ToString(),
                CreatedDate = i.CreatedDate.ToString("dd/MM/yyyy HH:mm"),
                PatientName = i.Record != null && i.Record.Appointment != null ? i.Record.Appointment.Patient.FullName : "Chưa cập nhật",
                Items = i.InvoiceDetails.Select(d => new
                {
                    Name = d.ItemType == InvoiceItemType.Medicine && d.Medicine != null ? d.Medicine.MedicineName : 
                           d.ItemType == InvoiceItemType.Service && d.Service != null ? d.Service.ServiceName : "Dịch vụ/Thuốc",
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    SubTotal = d.SubTotal
                })
            })
            .ToListAsync();

        return Ok(list);
    }

    // PATCH api/invoicesapi/{id}/collect — mark as paid
    [HttpPatch("{id}/collect")]
    public async Task<IActionResult> Collect(int id, [FromBody] CollectDto dto)
    {
        var inv = await db.Invoices.FindAsync(id);
        if (inv is null) return NotFound();
        if (inv.PaymentStatus == PaymentStatus.Paid)
            return BadRequest("Hóa đơn đã được thanh toán.");

        inv.PaymentStatus = PaymentStatus.Paid;
        await db.SaveChangesAsync();
        return Ok(new { id, status = "Paid", paymentMethod = dto.Method });
    }

    // POST api/invoicesapi/create-from-appointment — Kanban: create pending invoice when moving to cashier
    [HttpPost("create-from-appointment")]
    public async Task<IActionResult> CreateFromAppointment([FromBody] CreateFromApptDto dto)
    {
        var invoice = new Invoice
        {
            RecordId      = null,
            TotalAmount   = 0,
            PaymentStatus = PaymentStatus.Pending,
            CreatedDate   = DateTime.Now
        };
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();
        return Ok(new { invoice.InvoiceId });
    }
}

public record CollectDto(string Method);
public record CreateFromApptDto(int AppointmentId);
