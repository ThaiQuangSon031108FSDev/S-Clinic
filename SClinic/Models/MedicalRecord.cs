using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SClinic.Models;

public class MedicalRecord
{
    [Key]
    public int RecordId { get; set; }

    public int AppointmentId { get; set; }
    public int DoctorId { get; set; }

    [Column(TypeName = "nvarchar(MAX)")]
    public string? SkinCondition { get; set; }

    [Column(TypeName = "nvarchar(MAX)")]
    public string? Diagnosis { get; set; }

    public DateTime RecordDate { get; set; } = DateTime.Now;

    // Navigation
    [ForeignKey(nameof(AppointmentId))]
    public Appointment Appointment { get; set; } = null!;

    [ForeignKey(nameof(DoctorId))]
    public Doctor Doctor { get; set; } = null!;

    public Invoice? Invoice { get; set; }
}
