using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SClinic.Data;

namespace SClinic.Controllers;

public class HomeController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        // Bug #4 fix: load real services from DB for pricing section
        var services = await db.Services
            .OrderBy(s => s.Price)
            .Select(s => new { s.ServiceId, s.ServiceName, s.Price })
            .ToListAsync();
        ViewBag.Services = services;
        return View();
    }
}
