using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SClinic.Data;

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

        // Fixed slot list to check against
        var allSlots = new[]
        {
            "08:00","08:30","09:00","09:30","10:00","10:30",
            "13:30","14:00","14:30","15:00","15:30","16:00","16:30","17:00"
        };

        var result = allSlots.Select(slotStr =>
        {
            if (!TimeOnly.TryParseExact(slotStr, "HH:mm", out var t))
                return new { slot = slotStr, full = false };

            var matching = schedules.Where(s => s.TimeSlot == t).ToList();

            // No schedule at all → still bookable (backend will auto-assign/create)
            if (matching.Count == 0) return new { slot = slotStr, full = false };

            // Has schedules but ALL are fully booked → full
            bool full = matching.All(s => s.CurrentBooked >= s.MaxPatients);
            return new { slot = slotStr, full };
        }).ToList();

        return Ok(result);
    }
}
