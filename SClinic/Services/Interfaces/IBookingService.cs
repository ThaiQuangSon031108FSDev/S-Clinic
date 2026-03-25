using SClinic.Models;

namespace SClinic.Services.Interfaces;

public interface IBookingService
{
    Task<IEnumerable<DoctorSchedule>> GetAvailableSlotsAsync(int doctorId, DateOnly date);
    Task<Appointment?> BookAppointmentAsync(int patientId, int scheduleId, int? patientTreatmentId = null);
    Task<bool> CancelAppointmentAsync(int appointmentId);
    Task<bool> ConfirmAppointmentAsync(int appointmentId);
    Task<IEnumerable<Appointment>> GetPatientAppointmentsAsync(int patientId);
}
