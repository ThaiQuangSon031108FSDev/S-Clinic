using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SClinic.Models;

public class TreatmentSessionLog
{
    [Key]
    public int LogId { get; set; }

    public int PatientTreatmentId { get; set; }
    public DateTime UsedDate { get; set; } = DateTime.Now;
    public int PerformedBy { get; set; }

    [MaxLength(1000)]
    public string? SessionNotes { get; set; }

    // Navigation
    [ForeignKey(nameof(PatientTreatmentId))]
    public PatientTreatment PatientTreatment { get; set; } = null!;

    [ForeignKey(nameof(PerformedBy))]
    public Account PerformedByAccount { get; set; } = null!;

    public ICollection<SessionImage> SessionImages { get; set; } = [];
}
