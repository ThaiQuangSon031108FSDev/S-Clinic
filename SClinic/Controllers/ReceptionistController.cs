using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SClinic.Controllers;

[Authorize(Roles = "Receptionist,Admin")]
public class ReceptionistController : Controller
{
    // GET /Receptionist/Dashboard
    public IActionResult Dashboard() => View();

    // GET /Receptionist/Appointments
    public IActionResult Appointments() => View();

    // GET /Receptionist/LogSession
    public IActionResult LogSession() => View();

    // GET /Receptionist/RegisterPatient
    public IActionResult RegisterPatient() => View();
}
