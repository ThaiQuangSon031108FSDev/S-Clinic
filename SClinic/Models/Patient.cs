using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SClinic.Validation;

namespace SClinic.Models;

public class Patient
{
    [Key]
    public int PatientId { get; set; }

    public int? AccountId { get; set; }

    [Required, MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    [ValidPhoneFormat]
    public string Phone { get; set; } = string.Empty;

    public DateOnly? DateOfBirth { get; set; }

    public string? BaseMedicalHistory { get; set; }

    // Navigation
    [ForeignKey(nameof(AccountId))]
    public Account? Account { get; set; }
    public ICollection<Appointment> Appointments { get; set; } = [];
    public ICollection<PatientTreatment> PatientTreatments { get; set; } = [];
}
