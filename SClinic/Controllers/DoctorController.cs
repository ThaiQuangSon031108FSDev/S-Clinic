using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SClinic.Data;
using SClinic.Models;
using System.Security.Claims;

namespace SClinic.Controllers;

[Authorize(Roles = "Doctor,Admin")]
public class DoctorController(ApplicationDbContext db) : Controller
{
    // GET /Doctor/Schedule — Entry point for doctors (replaces Dashboard)

    public async Task<IActionResult> MedicalRecord(int id)
    {
        var appt = await db.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Schedule)
            .FirstOrDefaultAsync(a => a.AppointmentId == id);

        if (appt is null) return NotFound();
        if (appt.Status == AppointmentStatus.Cancelled) return BadRequest("Lịch hẹn đã bị huỷ.");

        ViewData["Title"] = $"Phiếu khám — {appt.Patient.FullName}";
        ViewBag.AppointmentId = id;
        return View();
    }

    // GET /Doctor/Schedule
    public async Task<IActionResult> Schedule()
    {
        ViewData["Title"] = "Lịch làm việc";
        var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var doctor    = await db.Doctors.FirstOrDefaultAsync(d => d.AccountId == accountId);
        if (doctor is null) return View(new List<DoctorSchedule>());

        // Include Appointments + Patient so the calendar card shows who is booked on each slot
        var schedules = await db.DoctorSchedules
            .Include(s => s.Appointments.Where(a => a.Status == AppointmentStatus.Confirmed
                                                 || a.Status == AppointmentStatus.Completed))
                .ThenInclude(a => a.Patient)
            .Where(s => s.DoctorId == doctor.DoctorId
                     && s.WorkDate >= DateOnly.FromDateTime(DateTime.Today))
            .OrderBy(s => s.WorkDate).ThenBy(s => s.TimeSlot)
            .ToListAsync();

        return View(schedules);
    }

    // GET /Doctor/Patients
    public async Task<IActionResult> Patients()
    {
        ViewData["Title"] = "Bệnh nhân";
        var patients = await db.Patients.OrderBy(p => p.FullName).ToListAsync();
        return View(patients);
    }
}
