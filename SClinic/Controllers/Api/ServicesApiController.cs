using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SClinic.Data;
using System.Security.Claims;

namespace SClinic.Controllers.Api;

[ApiController, Route("api/[controller]")]
public class ServicesApiController(ApplicationDbContext db) : ControllerBase
{
    // GET api/servicesapi — public, used by Book page to load real prices
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await db.Services
            .OrderBy(s => s.Price)
            .Select(s => new { s.ServiceId, s.ServiceName, s.Price })
            .ToListAsync();
        return Ok(list);
    }

    // GET api/servicesapi/slots?date=2026-03-25
    // Returns each time slot string and whether it's full (all doctors booked out)
    [HttpGet("slots")]
    public async Task<IActionResult> GetSlots([FromQuery] string date)
    {
        if (!DateOnly.TryParse(date, out var d))
            return BadRequest("Ngày không hợp lệ.");

        // All schedules for this date
        var schedules = await db.DoctorSchedules
            .Where(s => s.WorkDate == d)
            .Select(s => new { s.TimeSlot, s.MaxPatients, s.CurrentBooked })
            .ToListAsync();

        // Check if the current patient already has an appointment today to lock their slot
        var patientBookedSlots = new List<TimeOnly>();
        if (User.Identity?.IsAuthenticated == true && User.IsInRole("Patient"))
        {
            var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (accountIdClaim != null && int.TryParse(accountIdClaim.Value, out var accountId))
            {
                var patient = await db.Patients.FirstOrDefaultAsync(p => p.AccountId == accountId);
                if (patient != null)
                {
                    patientBookedSlots = await db.Appointments
                        .Where(a => a.PatientId == patient.PatientId
                                 && a.Schedule.WorkDate == d
                                 && a.Status != SClinic.Models.AppointmentStatus.Cancelled)
                        .Select(a => a.Schedule.TimeSlot)
                        .ToListAsync();
                }
            }
        }

        // Fixed slot list to check against
        var allSlots = new[]
        {
            "08:00","08:30","09:00","09:30","10:00","10:30",
            "13:30","14:00","14:30","15:00","15:30","16:00","16:30","17:00"
        };

        var result = allSlots.Select(slotStr =>
        {
            if (!TimeOnly.TryParseExact(slotStr, "HH:mm", out var t))
                return new { slot = slotStr, full = true };

            // Find all doctor schedules for this slot
            var matching = schedules.Where(s => s.TimeSlot == t).ToList();

            // 1. If NO doctor has a schedule for this slot -> Cannot book (full)
            if (matching.Count == 0) return new { slot = slotStr, full = true };

            // 2. If ALL doctors are fully booked in this slot -> full
            bool full = matching.All(s => s.CurrentBooked >= s.MaxPatients);

            // 3. If the Patient ALREADY booked an appointment in this slot -> lock it for them
            if (patientBookedSlots.Contains(t)) full = true;

            return new { slot = slotStr, full };
        }).ToList();

        return Ok(result);
    }
}
