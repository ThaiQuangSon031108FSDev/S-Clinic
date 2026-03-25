using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SClinic.Models;
using SClinic.Services.Interfaces;

namespace SClinic.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InvoicesController(IInvoiceService invoiceService) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var invoice = await invoiceService.GetInvoiceAsync(id);
        return invoice is null ? NotFound() : Ok(invoice);
    }

    [HttpGet("pending")]
    [Authorize(Roles = "Cashier,Admin")]
    public async Task<IActionResult> GetPending()
    {
        var invoices = await invoiceService.GetPendingInvoicesAsync();
        return Ok(invoices);
    }

    [HttpPost]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> Create([FromBody] CreateInvoiceRequest req)
    {
        var invoice = await invoiceService.CreateInvoiceAsync(req.RecordId, req.Details);
        return CreatedAtAction(nameof(Get), new { id = invoice.InvoiceId }, invoice);
    }

    [HttpPost("{id}/pay")]
    [Authorize(Roles = "Cashier,Admin")]
    public async Task<IActionResult> Pay(int id)
    {
        try
        {
            var success = await invoiceService.PayInvoiceAsync(id);
            return success ? Ok(new { message = "Payment processed." }) : BadRequest(new { message = "Invoice not found or already paid." });
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
    }
}

public record CreateInvoiceRequest(int RecordId, IEnumerable<InvoiceDetail> Details);
