using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SClinic.Data;
using SClinic.Hubs;
using SClinic.Models;

namespace SClinic.Controllers.Api;

[ApiController, Route("api/[controller]")]
[Authorize(Roles = "Admin,Cashier,Receptionist")]
public class InvoicesApiController(
    ApplicationDbContext db,
    IHubContext<ClinicHub> hub,
    SClinic.Services.Interfaces.IInvoiceService invoiceService) : ControllerBase
{
    // GET api/invoicesapi — all invoices (optionally filter by status)
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status = null)
    {
        var q = db.Invoices
            .Include(i => i.Record)
                .ThenInclude(r => r.Appointment)
                    .ThenInclude(a => a.Patient)
            .Include(i => i.Appointment)        // direct FK fallback
                .ThenInclude(a => a!.Patient)
            .Include(i => i.InvoiceDetails)
                .ThenInclude(d => d.Medicine)
            .Include(i => i.InvoiceDetails)
                .ThenInclude(d => d.Service)
            .Include(i => i.InvoiceDetails)
                .ThenInclude(d => d.Package)    // Gói liệu trình
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PaymentStatus>(status, true, out var ps))
            q = q.Where(i => i.PaymentStatus == ps);

        var list = await q
            .OrderByDescending(i => i.CreatedDate)
            .ToListAsync();

        var result = list.Select(i =>
        {
            // PatientName: try Record chain first, then direct Appointment FK
            var patientName = i.Record?.Appointment?.Patient?.FullName
                           ?? i.Appointment?.Patient?.FullName
                           ?? "Chưa cập nhật";

            // ServiceName: Service lẻ hoặc tên gói liệu trình
            var serviceName = i.InvoiceDetails.FirstOrDefault(d => d.ItemType == InvoiceItemType.Package)?.Package?.PackageName
                           ?? i.InvoiceDetails.FirstOrDefault(d => d.ItemType == InvoiceItemType.Service)?.Service?.ServiceName
                           ?? "";

            return new
            {
                i.InvoiceId,
                i.TotalAmount,
                PaymentStatus = i.PaymentStatus.ToString(),
                CreatedDate   = i.CreatedDate.ToString("dd/MM/yyyy HH:mm"),
                PatientName   = patientName,
                ServiceName   = serviceName,
                Details       = i.InvoiceDetails.Select(d => new
                {
                    Name      = d.ItemType == InvoiceItemType.Medicine && d.Medicine != null ? d.Medicine.MedicineName
                               : d.ItemType == InvoiceItemType.Service && d.Service  != null ? d.Service.ServiceName
                               : d.ItemType == InvoiceItemType.Package && d.Package  != null ? $"🎁 {d.Package.PackageName}"
                               : "—",
                    d.Quantity,
                    d.UnitPrice,
                    Subtotal  = d.SubTotal
                }).ToList()
            };
        });

        return Ok(result);
    }

    // PATCH api/invoicesapi/{id}/collect — mark as paid
    [HttpPatch("{id}/collect")]
    public async Task<IActionResult> Collect(int id, [FromBody] CollectDto dto)
    {
        try 
        {
            var success = await invoiceService.PayInvoiceAsync(id);
            if (!success)
                return BadRequest("Hóa đơn đã được thanh toán hoặc không tồn tại.");

            // 🔔 SignalR: notify Admin that payment was collected
            var inv = await invoiceService.GetInvoiceAsync(id);
            var patientName = inv?.Record?.Appointment?.Patient?.FullName 
                           ?? inv?.Appointment?.Patient?.FullName ?? "Bệnh nhân";

            if (inv != null)
            {
                await hub.Clients.Group("Admin").SendAsync("ReceiveNotification", new
                {
                    type    = "payment",
                    icon    = "✅",
                    title   = "Đã thu tiền",
                    message = $"Hóa đơn #{id} của {patientName} — {inv.TotalAmount:N0}đ ({dto.Method})"
                });
            }

            return Ok(new { id, status = "Paid", paymentMethod = dto.Method });
        }
        catch (InvalidOperationException ex) 
        {
            return BadRequest(ex.Message);
        }
    }

    // POST api/invoicesapi/create-from-appointment — Kanban: create pending invoice when moving to cashier
    [HttpPost("create-from-appointment")]
    public async Task<IActionResult> CreateFromAppointment([FromBody] CreateFromApptDto dto)
    {
        // Prevent duplicate invoice for same appointment
        var existing = await db.Invoices
            .FirstOrDefaultAsync(i => i.AppointmentId == dto.AppointmentId);
        if (existing is not null)
            return Ok(new { existing.InvoiceId });

        var invoice = new Invoice
        {
            AppointmentId = dto.AppointmentId,
            RecordId      = null,
            TotalAmount   = 0,
            PaymentStatus = PaymentStatus.Pending,
            CreatedDate   = DateTime.Now
        };
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();
        return Ok(new { invoice.InvoiceId });
    }

    // GET api/invoicesapi/revenue-chart?range=week|month  (Bug #7 fix)
    [HttpGet("revenue-chart")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RevenueChart([FromQuery] string range = "week")
    {
        var today    = DateTime.Today;
        var tomorrow = today.AddDays(1);
        int days     = range == "month" ? 30 : 7;
        var from     = today.AddDays(-(days - 1));

        var raw = await db.Invoices
            .Where(i => i.PaymentStatus == PaymentStatus.Paid
                     && i.CreatedDate >= from && i.CreatedDate < tomorrow)
            .GroupBy(i => i.CreatedDate.Date)
            .Select(g => new { Date = g.Key, Total = g.Sum(x => x.TotalAmount) })
            .ToListAsync();

        // Fill zero for missing days
        var result = Enumerable.Range(0, days).Select(offset =>
        {
            var d   = from.AddDays(offset);
            var rev = raw.FirstOrDefault(r => r.Date == d)?.Total ?? 0;
            return new { label = d.ToString("dd/MM"), value = Math.Round(rev / 1_000_000m, 2) };
        }).ToList();

        return Ok(result);
    }
}

public record CollectDto(string Method);
public record CreateFromApptDto(int AppointmentId);
