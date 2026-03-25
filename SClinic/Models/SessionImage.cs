using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SClinic.Models;

public class SessionImage
{
    [Key]
    public int ImageId { get; set; }

    public int LogId { get; set; }

    [Required, MaxLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    public DateTime UploadDate { get; set; } = DateTime.Now;

    // Navigation
    [ForeignKey(nameof(LogId))]
    public TreatmentSessionLog Log { get; set; } = null!;
}
