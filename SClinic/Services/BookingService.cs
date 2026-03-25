using Microsoft.EntityFrameworkCore;
using SClinic.Data;
using SClinic.Models;
using SClinic.Services.Interfaces;

namespace SClinic.Services;

public class BookingService(ApplicationDbContext db) : IBookingService
{
    public async Task<IEnumerable<DoctorSchedule>> GetAvailableSlotsAsync(int doctorId, DateOnly date)
    {
        return await db.DoctorSchedules
            .Where(s => s.DoctorId == doctorId
                     && s.WorkDate == date
                     && s.CurrentBooked < s.MaxPatients)
            .OrderBy(s => s.TimeSlot)
            .ToListAsync();
    }

    public async Task<Appointment?> BookAppointmentAsync(int patientId, int scheduleId, int? patientTreatmentId = null)
    {
        await using var tx = await db.Database.BeginTransactionAsync();

        var schedule = await db.DoctorSchedules
            .FirstOrDefaultAsync(s => s.ScheduleId == scheduleId && s.CurrentBooked < s.MaxPatients);

        if (schedule is null) return null;

        schedule.CurrentBooked++;

        var appointment = new Appointment
        {
            PatientId = patientId,
            ScheduleId = scheduleId,
            PatientTreatmentId = patientTreatmentId,
            Status = AppointmentStatus.Pending
        };

        db.Appointments.Add(appointment);
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return appointment;
    }

    public async Task<bool> CancelAppointmentAsync(int appointmentId)
    {
        await using var tx = await db.Database.BeginTransactionAsync();

        var appointment = await db.Appointments
            .Include(a => a.Schedule)
            .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

        if (appointment is null || appointment.Status == AppointmentStatus.Completed)
            return false;

        appointment.Status = AppointmentStatus.Cancelled;
        appointment.Schedule.CurrentBooked = Math.Max(0, appointment.Schedule.CurrentBooked - 1);

        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return true;
    }

    public async Task<bool> ConfirmAppointmentAsync(int appointmentId)
    {
        var appointment = await db.Appointments.FindAsync(appointmentId);
        if (appointment is null || appointment.Status != AppointmentStatus.Pending) return false;

        appointment.Status = AppointmentStatus.Confirmed;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<Appointment>> GetPatientAppointmentsAsync(int patientId)
    {
        return await db.Appointments
            .Include(a => a.Schedule)
                .ThenInclude(s => s.Doctor)
            .Where(a => a.PatientId == patientId)
            .OrderByDescending(a => a.Schedule.WorkDate)
            .ThenByDescending(a => a.Schedule.TimeSlot)
            .ToListAsync();
    }
}
