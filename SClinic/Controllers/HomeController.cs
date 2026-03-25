using Microsoft.AspNetCore.Mvc;

namespace SClinic.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();
}
