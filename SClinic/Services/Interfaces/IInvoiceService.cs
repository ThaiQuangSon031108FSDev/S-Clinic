using SClinic.Models;

namespace SClinic.Services.Interfaces;

public interface IInvoiceService
{
    Task<Invoice> CreateInvoiceAsync(int recordId, IEnumerable<InvoiceDetail> details);
    Task<bool> PayInvoiceAsync(int invoiceId);
    Task<Invoice?> GetInvoiceAsync(int invoiceId);
    Task<IEnumerable<Invoice>> GetPendingInvoicesAsync();
}
