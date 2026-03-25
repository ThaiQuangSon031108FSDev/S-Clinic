using Microsoft.EntityFrameworkCore;
using SClinic.Data;
using SClinic.Models;
using SClinic.Services.Interfaces;

namespace SClinic.Services;

public class TreatmentService(ApplicationDbContext db) : ITreatmentService
{
    public async Task<PatientTreatment?> AssignPackageAsync(int patientId, int packageId, int primaryDoctorId)
    {
        var package = await db.TreatmentPackages.FindAsync(packageId);
        if (package is null) return null;

        var treatment = new PatientTreatment
        {
            PatientId = patientId,
            PackageId = packageId,
            PrimaryDoctorId = primaryDoctorId,
            TotalSessions = package.TotalSessions,
            UsedSessions = 0,
            Status = TreatmentStatus.Active
        };

        db.PatientTreatments.Add(treatment);
        await db.SaveChangesAsync();
        return treatment;
    }

    /// <summary>
    /// Deducts one session from UsedSessions, creates a log, and saves Before/After images.
    /// </summary>
    public async Task<TreatmentSessionLog?> LogSessionAsync(
        int patientTreatmentId,
        int performedBy,
        string? notes,
        IEnumerable<string> imageUrls)
    {
        await using var tx = await db.Database.BeginTransactionAsync();

        var treatment = await db.PatientTreatments.FindAsync(patientTreatmentId);
        if (treatment is null || treatment.Status != TreatmentStatus.Active) return null;
        if (treatment.UsedSessions >= treatment.TotalSessions) return null;

        treatment.UsedSessions++;
        if (treatment.UsedSessions >= treatment.TotalSessions)
            treatment.Status = TreatmentStatus.Completed;

        var log = new TreatmentSessionLog
        {
            PatientTreatmentId = patientTreatmentId,
            PerformedBy = performedBy,
            SessionNotes = notes,
            UsedDate = DateTime.Now,
            SessionImages = imageUrls
                .Select(url => new SessionImage { ImageUrl = url, UploadDate = DateTime.Now })
                .ToList()
        };

        db.TreatmentSessionLogs.Add(log);
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return log;
    }

    public async Task<IEnumerable<PatientTreatment>> GetPatientTreatmentsAsync(int patientId)
    {
        return await db.PatientTreatments
            .Include(pt => pt.Package)
            .Include(pt => pt.PrimaryDoctor)
            .Where(pt => pt.PatientId == patientId)
            .ToListAsync();
    }

    public async Task<PatientTreatment?> GetTreatmentDetailAsync(int patientTreatmentId)
    {
        return await db.PatientTreatments
            .Include(pt => pt.Package)
            .Include(pt => pt.PrimaryDoctor)
            .Include(pt => pt.SessionLogs)
                .ThenInclude(log => log.SessionImages)
            .FirstOrDefaultAsync(pt => pt.PatientTreatmentId == patientTreatmentId);
    }
}
