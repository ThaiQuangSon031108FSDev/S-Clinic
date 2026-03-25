using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SClinic.Data;

namespace SClinic.Controllers.Api;

[ApiController, Route("api/[controller]")]
public class DoctorsApiController(ApplicationDbContext db) : ControllerBase
{
    // GET api/doctorsapi — list all active doctors for booking
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await db.Doctors
            .OrderBy(d => d.FullName)
            .Select(d => new
            {
                id        = d.DoctorId,
                fullName  = d.FullName,
                name      = d.FullName,
                specialty = d.Specialty
            })
            .ToListAsync();

        return Ok(list);
    }
}
