using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SClinic.Data;
using System.Security.Claims;

namespace SClinic.Controllers;

[Authorize(Roles = "Doctor,Admin")]
public class DoctorController(ApplicationDbContext db) : Controller
{
    // GET /Doctor/Dashboard — Phiếu khám bệnh
    public IActionResult Dashboard()
    {
        ViewData["Title"] = "Khám bệnh";
        return View();
    }

    // GET /Doctor/Schedule
    public async Task<IActionResult> Schedule()
    {
        ViewData["Title"] = "Lịch làm việc";
        var accountId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var doctor    = await db.Doctors.FirstOrDefaultAsync(d => d.AccountId == accountId);
        if (doctor is null) return View(new List<Models.DoctorSchedule>());

        var schedules = await db.DoctorSchedules
            .Where(s => s.DoctorId == doctor.DoctorId && s.WorkDate >= DateOnly.FromDateTime(DateTime.Today))
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
