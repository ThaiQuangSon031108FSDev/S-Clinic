using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SClinic.Models;

public class TreatmentPackage
{
    [Key]
    public int PackageId { get; set; }

    [Required, MaxLength(200)]
    public string PackageName { get; set; } = string.Empty;

    public int TotalSessions { get; set; }
    public decimal Price { get; set; }

    // Navigation
    public ICollection<PatientTreatment> PatientTreatments { get; set; } = [];
    public ICollection<InvoiceDetail> InvoiceDetails { get; set; } = [];
}
