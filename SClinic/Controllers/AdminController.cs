using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SClinic.Data;

namespace SClinic.Controllers;

[Authorize(Roles = "Admin,Cashier")]
public class AdminController(ApplicationDbContext db) : Controller
{
    // GET /Admin/Dashboard — Admin only
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Dashboard()
    {
        ViewData["Title"] = "Báo cáo";
        var today = DateTime.Today;

        ViewBag.TodayRevenue = await db.Invoices
            .Where(i => i.PaymentStatus == Models.PaymentStatus.Paid && i.CreatedDate.Date == today)
            .SumAsync(i => (decimal?)i.TotalAmount) ?? 0;

        ViewBag.WaitingCount = await db.Appointments
            .CountAsync(a => a.Status == Models.AppointmentStatus.Pending);

        ViewBag.PendingInvoices = await db.Invoices
            .CountAsync(i => i.PaymentStatus == Models.PaymentStatus.Pending);

        ViewBag.LowStockCount = await db.Medicines
            .CountAsync(m => m.StockQuantity <= 10);

        return View();
    }

    // GET /Admin/Finance
    public IActionResult Finance()
    {
        ViewData["Title"] = "Thu ngân";
        return View();
    }

    // GET /Admin/Inventory — Admin only
    [Authorize(Roles = "Admin")]
    public IActionResult Inventory()
    {
        ViewData["Title"] = "Kho thuốc";
        return View();
    }
}
