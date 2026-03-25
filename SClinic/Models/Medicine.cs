using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SClinic.Models;

public class Medicine
{
    [Key]
    public int MedicineId { get; set; }

    public int CategoryId { get; set; }

    [Required, MaxLength(200)]
    public string MedicineName { get; set; } = string.Empty;

    public int StockQuantity { get; set; } = 0;
    public decimal Price { get; set; }

    // Navigation
    [ForeignKey(nameof(CategoryId))]
    public Category Category { get; set; } = null!;

    public ICollection<InvoiceDetail> InvoiceDetails { get; set; } = [];
}
