using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SClinic.Models;

public enum InvoiceItemType
{
    Medicine,
    Service,
    Package
}

public class InvoiceDetail
{
    [Key]
    public int DetailId { get; set; }

    public int InvoiceId { get; set; }
    public InvoiceItemType ItemType { get; set; }

    public int? MedicineId { get; set; }
    public int? ServiceId { get; set; }
    public int? PackageId { get; set; }

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }

    // Navigation
    [ForeignKey(nameof(InvoiceId))]
    public Invoice Invoice { get; set; } = null!;

    [ForeignKey(nameof(MedicineId))]
    public Medicine? Medicine { get; set; }

    [ForeignKey(nameof(ServiceId))]
    public Service? Service { get; set; }

    [ForeignKey(nameof(PackageId))]
    public TreatmentPackage? Package { get; set; }
}
