using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SClinic.Data;
using SClinic.Models;

namespace SClinic.Controllers;

/// <summary>Dev-only utilities — NOT for production.</summary>
[Route("[controller]")]
public class DevController(ApplicationDbContext db, IWebHostEnvironment env) : Controller
{
    // ── GET /dev/fix-encoding ─────────────────────────────────────────────
    // Re-writes all Vietnamese strings that got garbled when Data.sql was
    // executed by SSMS using Windows-1252 instead of UTF-8.
    [HttpGet("fix-encoding")]
    public async Task<IActionResult> FixEncoding()
    {
        var log = new System.Text.StringBuilder();

        // 1. DOCTORS
        var doctors = await db.Doctors.ToListAsync();
        var doctorFix = new Dictionary<int, (string Name, string Spec)>
        {
            [1] = ("Bs. Lê Thị B",     "Chuyên khoa II Da liễu - Trị Mụn"),
            [2] = ("Bs. Nguyễn Văn C",  "Chuyên khoa Laser & Phục hồi da"),
            [3] = ("Bs. Trần Thị D",    "Chuyên khoa Thẩm mỹ Nội khoa"),
            [4] = ("Bs. Phạm Hoàng E",  "Chuyên khoa Trị Nám - Tàn nhang"),
            [5] = ("Bs. Võ Thị F",      "Bác sĩ Da liễu Tổng quát"),
        };
        foreach (var d in doctors)
        {
            if (doctorFix.TryGetValue(d.DoctorId, out var fix))
            { d.FullName = fix.Name; d.Specialty = fix.Spec; }
        }
        log.AppendLine($"✅ Doctors: {doctors.Count} rows updated");

        // 2. PATIENTS
        var patients = await db.Patients.ToListAsync();
        var patientFix = new Dictionary<int, (string Name, string? Phone, string? History)>
        {
            [1]  = ("Thái Quang Sơn",     "0987654321", "Dị ứng Penicillin. Da nhạy cảm cồn."),
            [2]  = ("Trần Phương Anh",     "0912345678", "Không có tiền sử dị ứng"),
            [3]  = ("Nguyễn Văn A",        "0933100001", "Dị ứng hải sản"),
            [4]  = ("Lê Thị C",            "0933100002", "Đang dùng Retinol 1%"),
            [5]  = ("Phạm Văn D",          "0933100003", "Dị ứng Paracetamol"),
            [6]  = ("Hoàng Thanh Trúc",    "0966777888", "Không"),
            [7]  = ("Vũ Hải Đăng",         "0977888999", "Da mỏng đỏ, giãn mao mạch"),
            [8]  = ("Bùi Thị Mỹ Hạnh",    "0988999111", "Không"),
            [9]  = ("Đặng Tuấn Anh",       "0999000111", "Không"),
            [10] = ("Ngô Quý Đôn",         "0900111222", "Tiểu đường tuýp 2"),
            [11] = ("Dương Thúy Quỳnh",    "0911222333", "Không"),
            [12] = ("Lý Hạo Nam",          "0922333444", "Không"),
            [13] = ("Trịnh Hải Yến",       "0933500001", "Dị ứng thời tiết"),
            [14] = ("Mai Bảo Ngọc",        "0933500002", "Không"),
            [15] = ("Tạ Quang Thắng",      "0933500003", "Huyết áp cao"),
        };
        foreach (var p in patients)
        {
            if (patientFix.TryGetValue(p.PatientId, out var fix))
            { p.FullName = fix.Name; p.BaseMedicalHistory = fix.History; }
        }
        log.AppendLine($"✅ Patients: {patients.Count} rows updated");

        // 3. MEDICINES
        var medicines = await db.Medicines.ToListAsync();
        var medFix = new Dictionary<int, (string Name, string Cat)>
        {
            [1]  = ("Isotretinoin 10mg (Acnocut)",         "Thuốc Kê Đơn"),
            [2]  = ("Kháng sinh Azithromycin 500mg",       "Thuốc Kê Đơn"),
            [3]  = ("Kem bôi trị mụn Klenzit MS",          "Thuốc Kê Đơn"),
            [4]  = ("Kem phục hồi B5 La Roche-Posay",      "Dược Mỹ Phẩm"),
            [5]  = ("Serum Niacinamide 10% Paula's Choice","Dược Mỹ Phẩm"),
            [6]  = ("Sữa rửa mặt Cerave Foaming 236ml",   "Dược Mỹ Phẩm"),
            [7]  = ("Kem chống nắng MartiDerm Proteos",    "Dược Mỹ Phẩm"),
            [8]  = ("Toner BHA 2% Obagi Medical",          "Dược Mỹ Phẩm"),
            [9]  = ("Dung dịch chấm mụn Mario Badescu",   "Dược Mỹ Phẩm"),
            [10] = ("Serum Vitamin C 15% Vichy",           "Dược Mỹ Phẩm"),
        };
        foreach (var m in medicines)
        {
            if (medFix.TryGetValue(m.MedicineId, out var fix))
                m.MedicineName = fix.Name;
                // CategoryId FK set by migration data — no CategoryName to update
        }
        log.AppendLine($"✅ Medicines: {medicines.Count} rows updated");

        // 4. SERVICES
        var services = await db.Services.ToListAsync();
        var svcFix = new Dictionary<int, string>
        {
            [1] = "Khám & Soi da 3D",
            [2] = "Lấy nhân mụn chuẩn Y khoa",
            [3] = "Peel da sinh học BHA/AHA",
            [4] = "Điện di Ion phục hồi B5",
            [5] = "Chiếu ánh sáng sinh học Omega Light",
        };
        foreach (var s in services)
            if (svcFix.TryGetValue(s.ServiceId, out var name)) s.ServiceName = name;
        log.AppendLine($"✅ Services: {services.Count} rows updated");

        // 5. TREATMENT PACKAGES
        var packages = await db.TreatmentPackages.ToListAsync();
        var pkgFix = new Dictionary<int, string>
        {
            [1] = "Liệu trình Trị Mụn Chuẩn Y Khoa",
            [2] = "Gói Phục Hồi Da Nhiễm Corticoid",
            [3] = "Trẻ Hóa Da Cấy Mesotherapy",
            [4] = "Trị Nám / Tàn Nhang Laser Pico",
            [5] = "Triệt Lông Vùng Nách Diode Laser",
        };
        foreach (var p in packages)
            if (pkgFix.TryGetValue(p.PackageId, out var name)) p.PackageName = name;
        log.AppendLine($"✅ TreatmentPackages: {packages.Count} rows updated");

        await db.SaveChangesAsync();

        log.AppendLine("\n🎉 Hoàn tất! Reload lại các trang để kiểm tra font chữ.");
        return Content(log.ToString(), "text/plain; charset=utf-8");
    }

    // ── GET /dev/seed-schedules ───────────────────────────────────────────────
    // Tạo lịch làm việc cho 5 bác sĩ × 14 slot × 30 ngày tới
    [HttpGet("seed-schedules")]
    public async Task<IActionResult> SeedSchedules()
    {
        var doctors = await db.Doctors.Select(d => d.DoctorId).ToListAsync();
        if (doctors.Count == 0)
            return Content("❌ Chưa có bác sĩ trong DB. Gọi /dev/fix-encoding trước.", "text/plain; charset=utf-8");

        var slots = new[]
        {
            new TimeOnly(8,0),  new TimeOnly(8,30),  new TimeOnly(9,0),
            new TimeOnly(9,30), new TimeOnly(10,0),  new TimeOnly(10,30),
            new TimeOnly(13,30),new TimeOnly(14,0),  new TimeOnly(14,30),
            new TimeOnly(15,0), new TimeOnly(15,30),new TimeOnly(16,0),
            new TimeOnly(16,30),new TimeOnly(17,0)
        };

        int added = 0;
        var today = DateOnly.FromDateTime(DateTime.Today);

        for (int day = 0; day <= 30; day++)
        {
            var date = today.AddDays(day);
            // Skip Sunday (0)
            if (date.DayOfWeek == DayOfWeek.Sunday) continue;

            foreach (var doctorId in doctors)
            {
                foreach (var slot in slots)
                {
                    bool exists = await db.DoctorSchedules
                        .AnyAsync(s => s.DoctorId == doctorId
                                    && s.WorkDate  == date
                                    && s.TimeSlot  == slot);
                    if (!exists)
                    {
                        db.DoctorSchedules.Add(new DoctorSchedule
                        {
                            DoctorId      = doctorId,
                            WorkDate      = date,
                            TimeSlot      = slot,
                            MaxPatients   = 3,
                            CurrentBooked = 0
                        });
                        added++;
                    }
                }
            }
        }

        await db.SaveChangesAsync();
        return Content($"✅ Đã tạo {added} lịch làm việc cho {doctors.Count} bác sĩ trong 30 ngày tới.\n👉 Bệnh nhân có thể đặt lịch ngay!", "text/plain; charset=utf-8");
    }
}
