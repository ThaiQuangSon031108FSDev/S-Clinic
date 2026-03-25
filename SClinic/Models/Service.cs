using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SClinic.Models;

public class Service
{
    [Key]
    public int ServiceId { get; set; }

    [Required, MaxLength(200)]
    public string ServiceName { get; set; } = string.Empty;

    public decimal Price { get; set; }

    // Navigation
    public ICollection<InvoiceDetail> InvoiceDetails { get; set; } = [];
}
