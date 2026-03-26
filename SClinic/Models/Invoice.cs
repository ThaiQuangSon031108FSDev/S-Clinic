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

    // Direct link to appointment (fallback when RecordId is null)
    public int? AppointmentId { get; set; }

    public decimal TotalAmount { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public DateTime CreatedDate { get; set; } = DateTime.Now;

    // Navigation
    [ForeignKey(nameof(RecordId))]
    public MedicalRecord? Record { get; set; }

    [ForeignKey(nameof(AppointmentId))]
    public Appointment? Appointment { get; set; }

    public ICollection<InvoiceDetail> InvoiceDetails { get; set; } = [];
}
