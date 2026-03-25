using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SClinic.Models;

public class Doctor
{
    [Key]
    public int DoctorId { get; set; }

    public int AccountId { get; set; }

    [Required, MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(100)]
    public string? Specialty { get; set; }

    // Navigation
    [ForeignKey(nameof(AccountId))]
    public Account Account { get; set; } = null!;
    public ICollection<DoctorSchedule> Schedules { get; set; } = [];
    public ICollection<MedicalRecord> MedicalRecords { get; set; } = [];
    public ICollection<PatientTreatment> PatientTreatments { get; set; } = [];
}
