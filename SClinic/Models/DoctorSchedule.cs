using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SClinic.Validation;

namespace SClinic.Models;

public class DoctorSchedule
{
    [Key]
    public int ScheduleId { get; set; }

    public int DoctorId { get; set; }

    [Required]
    [FutureDateOnly]
    public DateOnly WorkDate { get; set; }

    [Required]
    public TimeOnly TimeSlot { get; set; }

    public int MaxPatients { get; set; } = 1;
    public int CurrentBooked { get; set; } = 0;

    // Navigation
    [ForeignKey(nameof(DoctorId))]
    public Doctor Doctor { get; set; } = null!;
    public ICollection<Appointment> Appointments { get; set; } = [];
}
