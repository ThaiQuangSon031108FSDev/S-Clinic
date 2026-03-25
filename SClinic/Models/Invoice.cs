using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SClinic.Models;

public enum PaymentStatus
{
    Pending,
    Paid,
    Cancelled
}

public class Invoice
{
    [Key]
    public int InvoiceId { get; set; }

    // Nullable: direct package/product sales may not have a MedicalRecord
    public int? RecordId { get; set; }
    public decimal TotalAmount { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    // Navigation
    [ForeignKey(nameof(RecordId))]
    public MedicalRecord? Record { get; set; }

    public ICollection<InvoiceDetail> InvoiceDetails { get; set; } = [];
}
