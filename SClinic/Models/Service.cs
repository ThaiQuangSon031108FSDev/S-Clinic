using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SClinic.Models;

public enum ServiceType
{
    Consultation = 1, // Khám / Tư vấn — shows diagnosis + prescription form
    Treatment    = 2, // Điều trị / Làm dịch vụ — shows photo upload + treatment notes
}

public class Service
{
    [Key]
    public int ServiceId { get; set; }

    [Required, MaxLength(200)]
    public string ServiceName { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public ServiceType ServiceType { get; set; } = ServiceType.Consultation;

    // Navigation
    public ICollection<InvoiceDetail> InvoiceDetails { get; set; } = [];
}
