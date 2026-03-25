using SClinic.Models;

namespace SClinic.Services.Interfaces;

public interface ITreatmentService
{
    Task<PatientTreatment?> AssignPackageAsync(int patientId, int packageId, int primaryDoctorId);
    Task<TreatmentSessionLog?> LogSessionAsync(int patientTreatmentId, int performedBy, string? notes, IEnumerable<string> imageUrls);
    Task<IEnumerable<PatientTreatment>> GetPatientTreatmentsAsync(int patientId);
    Task<PatientTreatment?> GetTreatmentDetailAsync(int patientTreatmentId);
}
