using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SClinic.Data;
using SClinic.Hubs;
using SClinic.Models;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SClinic.Controllers.Api;

/// <summary>SePay webhook — tự động xác nhận thanh toán khi có tiền vào.</summary>
[ApiController, Route("api/payment")]
public class PaymentWebhookController(
    ApplicationDbContext db,
    IHubContext<ClinicHub> hub) : ControllerBase
{
    // POST api/payment/webhook  — SePay gọi endpoint này khi có giao dịch
    [HttpPost("webhook")]
    [AllowAnonymous] // SePay không cần auth
    public async Task<IActionResult> SePayWebhook([FromBody] JsonElement body)
    {
        try
        {
            // ── Parse SePay payload ──────────────────────────────────────────
            // transferType phải là "in" (tiền vào)
            var transferType = body.TryGetProperty("transferType", out var tt)
                ? tt.GetString() : null;
            if (transferType != "in")
                return Ok(new { success = true, message = "Ignored: not incoming." });

            var content = body.TryGetProperty("content", out var c)
                ? c.GetString() ?? "" : "";
            var amount  = body.TryGetProperty("transferAmount", out var a)
                ? a.GetDecimal() : 0;

            // ── Tìm mã hóa đơn trong nội dung: "SCLINIC HD0001" hoặc "SCLINIC HD1" ──
            var match = Regex.Match(content, @"SCLINIC\s*HD\s*(\d+)", RegexOptions.IgnoreCase);
            if (!match.Success)
                return Ok(new { success = true, message = "No invoice code found in content." });

            var invoiceId = int.Parse(match.Groups[1].Value);

            // ── Tìm hóa đơn ─────────────────────────────────────────────────
            var invoice = await db.Invoices
                .Include(i => i.Appointment)
                    .ThenInclude(a => a!.Patient)
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

            if (invoice is null)
                return Ok(new { success = true, message = $"Invoice {invoiceId} not found." });

            if (invoice.PaymentStatus == PaymentStatus.Paid)
                return Ok(new { success = true, message = "Already paid." });

            // ── Kiểm tra số tiền khớp (sai lệch < 1000đ là ok) ─────────────
            var expectedAmount = invoice.TotalAmount;
            if (Math.Abs(amount - expectedAmount) > 1000)
                return Ok(new { success = true, message = $"Amount mismatch: got {amount}, expected {expectedAmount}." });

            // ── Mark Paid ────────────────────────────────────────────────────
            invoice.PaymentStatus  = PaymentStatus.Paid;
            await db.SaveChangesAsync();

            // ── SignalR: notify Cashier + Admin ──────────────────────────────
            var patient = invoice.Appointment?.Patient?.FullName ?? $"HD#{invoiceId}";
            var notify  = new
            {
                type    = "payment",
                icon    = "✅",
                title   = "Thanh toán tự động",
                message = $"{patient} đã chuyển khoản {amount:N0}đ cho HD#{invoiceId:D4}."
            };
            await hub.Clients.Group("Cashier").SendAsync("ReceiveNotification", notify);
            await hub.Clients.Group("Admin").SendAsync("ReceiveNotification", notify);

            return Ok(new { success = true, invoiceId, amount, patient });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }
}
