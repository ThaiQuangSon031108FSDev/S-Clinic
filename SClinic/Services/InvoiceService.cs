using Microsoft.EntityFrameworkCore;
using SClinic.Data;
using SClinic.Models;
using SClinic.Services.Interfaces;

namespace SClinic.Services;

public class InvoiceService(ApplicationDbContext db) : IInvoiceService
{
    public async Task<Invoice> CreateInvoiceAsync(int recordId, IEnumerable<InvoiceDetail> details)
    {
        var detailList = details.ToList();

        // Calculate each SubTotal and the grand TotalAmount
        foreach (var d in detailList)
            d.SubTotal = d.UnitPrice * d.Quantity;

        var invoice = new Invoice
        {
            RecordId = recordId,
            TotalAmount = detailList.Sum(d => d.SubTotal),
            PaymentStatus = PaymentStatus.Pending,
            CreatedDate = DateTime.Now,
            InvoiceDetails = detailList
        };

        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();
        return invoice;
    }

    /// <summary>
    /// Marks an invoice as Paid and deducts StockQuantity for every Medicine line item.
    /// Runs inside a single DB transaction — if any stock goes negative the whole operation rolls back.
    /// </summary>
    public async Task<bool> PayInvoiceAsync(int invoiceId)
    {
        await using var tx = await db.Database.BeginTransactionAsync();

        var invoice = await db.Invoices
            .Include(i => i.InvoiceDetails)
                .ThenInclude(d => d.Medicine)
            .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

        if (invoice is null || invoice.PaymentStatus != PaymentStatus.Pending)
            return false;

        // Deduct stock for all medicine line items
        foreach (var detail in invoice.InvoiceDetails.Where(d => d.ItemType == InvoiceItemType.Medicine))
        {
            if (detail.Medicine is null) continue;

            if (detail.Medicine.StockQuantity < detail.Quantity)
                throw new InvalidOperationException(
                    $"Insufficient stock for '{detail.Medicine.MedicineName}'. " +
                    $"Available: {detail.Medicine.StockQuantity}, Required: {detail.Quantity}.");

            detail.Medicine.StockQuantity -= detail.Quantity;
        }

        invoice.PaymentStatus = PaymentStatus.Paid;

        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return true;
    }

    public async Task<Invoice?> GetInvoiceAsync(int invoiceId)
    {
        return await db.Invoices
            .Include(i => i.InvoiceDetails)
                .ThenInclude(d => d.Medicine)
            .Include(i => i.InvoiceDetails)
                .ThenInclude(d => d.Service)
            .Include(i => i.InvoiceDetails)
                .ThenInclude(d => d.Package)
            .Include(i => i.Record)
            .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);
    }

    public async Task<IEnumerable<Invoice>> GetPendingInvoicesAsync()
    {
        return await db.Invoices
            .Include(i => i.Record)
                .ThenInclude(r => r.Appointment)
                    .ThenInclude(a => a.Patient)
            .Where(i => i.PaymentStatus == PaymentStatus.Pending)
            .OrderBy(i => i.CreatedDate)
            .ToListAsync();
    }
}
