using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SClinic.Models;

public enum TreatmentStatus
{
    Active,
    Completed,
    Suspended
}

public class PatientTreatment
{
    [Key]
    public int PatientTreatmentId { get; set; }

    public int PatientId { get; set; }
    public int PackageId { get; set; }
    public int PrimaryDoctorId { get; set; }
    public int TotalSessions { get; set; }
    public int UsedSessions { get; set; } = 0;
    public TreatmentStatus Status { get; set; } = TreatmentStatus.Active;

    // Navigation
    [ForeignKey(nameof(PatientId))]
    public Patient Patient { get; set; } = null!;

    [ForeignKey(nameof(PackageId))]
    public TreatmentPackage Package { get; set; } = null!;

    [ForeignKey(nameof(PrimaryDoctorId))]
    public Doctor PrimaryDoctor { get; set; } = null!;

    public ICollection<TreatmentSessionLog> SessionLogs { get; set; } = [];
    public ICollection<Appointment> Appointments { get; set; } = [];
}
