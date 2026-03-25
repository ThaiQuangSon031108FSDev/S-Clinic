using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SClinic.Models;

public enum AppointmentStatus
{
    Pending,
    Confirmed,
    Completed,
    Cancelled
}

public class Appointment
{
    [Key]
    public int AppointmentId { get; set; }

    public int PatientId { get; set; }
    public int ScheduleId { get; set; }
    public int? PatientTreatmentId { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

    // Navigation
    [ForeignKey(nameof(PatientId))]
    public Patient Patient { get; set; } = null!;

    [ForeignKey(nameof(ScheduleId))]
    public DoctorSchedule Schedule { get; set; } = null!;

    [ForeignKey(nameof(PatientTreatmentId))]
    public PatientTreatment? PatientTreatment { get; set; }

    public MedicalRecord? MedicalRecord { get; set; }
}
