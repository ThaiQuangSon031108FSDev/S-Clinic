using Microsoft.EntityFrameworkCore;
using SClinic.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SClinic.Data;

public static class DbSeeder
{
    public static async Task SeedRealisticDemoData(ApplicationDbContext db)
    {
        // Prevent duplicate seeding
        if (await db.Accounts.AnyAsync(a => a.Email == "bs.lethib@sclinic.vn")) return;

        var defaultPassword = BCrypt.Net.BCrypt.HashPassword("Sclinic@123", workFactor: 11);

        // 1. ROLES
        int doctorRole = 2;
        int receptionistRole = 3;
        int patientRole = 5;

        // 2. CREATE ACCOUNTS
        var doc1Acc = new Account { Email = "bs.lethib@sclinic.vn", PasswordHash = defaultPassword, RoleId = doctorRole, IsActive = true };
        var doc2Acc = new Account { Email = "bs.hien@sclinic.vn", PasswordHash = defaultPassword, RoleId = doctorRole, IsActive = true };
        var recAcc = new Account { Email = "letan1@sclinic.vn", PasswordHash = defaultPassword, RoleId = receptionistRole, IsActive = true };
        
        var pt1Acc = new Account { Email = "thaiquangson@gmail.com", PasswordHash = defaultPassword, RoleId = patientRole, IsActive = true };
        var pt2Acc = new Account { Email = "khanhly@gmail.com", PasswordHash = defaultPassword, RoleId = patientRole, IsActive = true };
        var pt3Acc = new Account { Email = "minhtuan_99@yahoo.com", PasswordHash = defaultPassword, RoleId = patientRole, IsActive = true };
        var pt4Acc = new Account { Email = "ngocmai88@gmail.com", PasswordHash = defaultPassword, RoleId = patientRole, IsActive = true };
        var pt5Acc = new Account { Email = "tranvandat@gmail.com", PasswordHash = defaultPassword, RoleId = patientRole, IsActive = true };

        db.Accounts.AddRange(doc1Acc, doc2Acc, recAcc, pt1Acc, pt2Acc, pt3Acc, pt4Acc, pt5Acc);
        await db.SaveChangesAsync();

        // 3. CREATE PROFILES
        var pDoc1 = new Doctor { AccountId = doc1Acc.AccountId, FullName = "Bác sĩ Lê Thị B", Phone = "0900000001", Specialty = "Da Liễu Cơ Bản" };
        var pDoc2 = new Doctor { AccountId = doc2Acc.AccountId, FullName = "Bác sĩ Thu Hiền", Phone = "0900000002", Specialty = "Điều Trị Chuyên Sâu" };
        db.Doctors.AddRange(pDoc1, pDoc2);
        
        var pts = new List<Patient>
        {
            new Patient { AccountId = pt1Acc.AccountId, FullName = "Thái Quang Sơn", Phone = "0901234567", DateOfBirth = new DateOnly(1995, 5, 10), BaseMedicalHistory = "Dị ứng hải sản" },
            new Patient { AccountId = pt2Acc.AccountId, FullName = "Nguyễn Khánh Ly", Phone = "0987654321", DateOfBirth = new DateOnly(1998, 12, 1), BaseMedicalHistory = "Viêm da cơ địa" },
            new Patient { AccountId = pt3Acc.AccountId, FullName = "Trần Minh Tuấn", Phone = "0912345678", DateOfBirth = new DateOnly(1999, 8, 20), BaseMedicalHistory = "Không" },
            new Patient { AccountId = pt4Acc.AccountId, FullName = "Lê Ngọc Mai", Phone = "0933445566", DateOfBirth = new DateOnly(1988, 3, 15), BaseMedicalHistory = "Tiểu đường tuýp 2" },
            new Patient { AccountId = pt5Acc.AccountId, FullName = "Trần Văn Đạt", Phone = "0909090909", DateOfBirth = new DateOnly(2001, 1, 10), BaseMedicalHistory = "Hen suyễn" }
        };
        db.Patients.AddRange(pts);
        await db.SaveChangesAsync();

        // 4. CREATE DOCTOR SCHEDULES (Jan 1 -> Mar 28, Mon-Sat)
        var startDate = new DateOnly(2026, 1, 1);
        var endDate = new DateOnly(2026, 3, 28);
        var schedules = new List<DoctorSchedule>();
        
        var d = startDate;
        while (d <= endDate)
        {
            if (d.DayOfWeek != DayOfWeek.Sunday)
            {
                // Doc 1 schedules
                schedules.Add(new DoctorSchedule { DoctorId = pDoc1.DoctorId, WorkDate = d, TimeSlot = new TimeOnly(8, 00), MaxPatients = 1, CurrentBooked = 0 });
                schedules.Add(new DoctorSchedule { DoctorId = pDoc1.DoctorId, WorkDate = d, TimeSlot = new TimeOnly(9, 30), MaxPatients = 1, CurrentBooked = 0 });
                schedules.Add(new DoctorSchedule { DoctorId = pDoc1.DoctorId, WorkDate = d, TimeSlot = new TimeOnly(14, 00), MaxPatients = 1, CurrentBooked = 0 });
                schedules.Add(new DoctorSchedule { DoctorId = pDoc1.DoctorId, WorkDate = d, TimeSlot = new TimeOnly(15, 30), MaxPatients = 1, CurrentBooked = 0 });

                // Doc 2 schedules (afternoon shift only)
                schedules.Add(new DoctorSchedule { DoctorId = pDoc2.DoctorId, WorkDate = d, TimeSlot = new TimeOnly(13, 30), MaxPatients = 2, CurrentBooked = 0 });
                schedules.Add(new DoctorSchedule { DoctorId = pDoc2.DoctorId, WorkDate = d, TimeSlot = new TimeOnly(16, 00), MaxPatients = 1, CurrentBooked = 0 });
            }
            d = d.AddDays(1);
        }
        db.DoctorSchedules.AddRange(schedules);
        await db.SaveChangesAsync();

        // 5. FETCH EXISTING SERVICES AND MEDICINES FOR RANDOM INVOICES
        var services = await db.Services.ToListAsync();
        var medicines = await db.Medicines.ToListAsync();

        // 6. GENERATE RANDOM APPOINTMENTS & INVOICES
        var rnd = new Random(42); // fixed seed for reproducibility
        int todayDayOfYear = 86; // Approx Mar 27th

        // We'll generate about 50 historical appointments and 5 "today/future" appointments
        // Let's pick random schedules that are in the past
        var pastSchedules = schedules.Where(s => s.WorkDate < new DateOnly(2026, 3, 27)).OrderBy(x => rnd.Next()).Take(50).ToList();
        
        var diagnosesList = new[] { "Mụn viêm", "Nám mảng", "Sẹo rỗ", "Phục hồi da mỏng yếu", "Da tối màu", "Mụn nội tiết", "Viêm nang lông" };

        foreach (var s in pastSchedules)
        {
            var patient = pts[rnd.Next(pts.Count)];
            s.CurrentBooked++;
            
            // Base service (from Appointment)
            var baseService = services[rnd.Next(services.Count)];
            bool isTreatment = baseService.ServiceType == ServiceType.Treatment;

            var appt = new Appointment
            {
                PatientId = patient.PatientId,
                ScheduleId = s.ScheduleId,
                ServiceId = baseService.ServiceId,
                Status = AppointmentStatus.Completed,
                Notes = isTreatment ? "Khách mốn làm liệu trình" : "Khám lấy đơn thuốc"
            };
            db.Appointments.Add(appt);
            await db.SaveChangesAsync();

            var record = new MedicalRecord
            {
                AppointmentId = appt.AppointmentId,
                DoctorId = s.DoctorId,
                Diagnosis = diagnosesList[rnd.Next(diagnosesList.Length)],
                SkinCondition = isTreatment ? "Da đổ nhiều dầu, phản ứng tốt sau điều trị" : "Có mụn mủ vùng trán và cằm, cần kê đơn bôi",
                RecordDate = s.WorkDate.ToDateTime(s.TimeSlot)
            };
            db.MedicalRecords.Add(record);
            await db.SaveChangesAsync();

            // Create Invoice
            var invoice = new Invoice
            {
                RecordId = record.RecordId,
                AppointmentId = appt.AppointmentId,
                PaymentStatus = PaymentStatus.Paid,
                TotalAmount = 0m, // Calculate below
                CreatedDate = s.WorkDate.ToDateTime(s.TimeSlot).AddMinutes(30)
            };
            db.Invoices.Add(invoice);
            await db.SaveChangesAsync();

            decimal totalAmount = 0m;

            // 1. Add base service
            var svcDetail = new InvoiceDetail
            {
                InvoiceId = invoice.InvoiceId,
                ItemType = InvoiceItemType.Service,
                ServiceId = baseService.ServiceId,
                Quantity = 1,
                UnitPrice = baseService.Price,
                SubTotal = baseService.Price
            };
            db.InvoiceDetails.Add(svcDetail);
            totalAmount += svcDetail.SubTotal;

            // 2. Add 1-3 random medicines if Consultation, or maybe 1 if Treatment
            int medCount = isTreatment ? rnd.Next(0, 2) : rnd.Next(1, 4);
            var selectedMeds = medicines.OrderBy(x => rnd.Next()).Take(medCount).ToList();

            foreach(var m in selectedMeds)
            {
                int qty = rnd.Next(1, 3);
                var medDetail = new InvoiceDetail
                {
                    InvoiceId = invoice.InvoiceId,
                    ItemType = InvoiceItemType.Medicine,
                    MedicineId = m.MedicineId,
                    Quantity = qty,
                    UnitPrice = m.Price,
                    SubTotal = m.Price * qty
                };
                db.InvoiceDetails.Add(medDetail);
                totalAmount += medDetail.SubTotal;
            }

            // 3. Occasionally add another random extra service (like an add-on)
            if (rnd.Next(100) > 70) 
            {
                var extraSvc = services.Where(x => x.ServiceId != baseService.ServiceId).OrderBy(x => rnd.Next()).FirstOrDefault();
                if (extraSvc != null)
                {
                    var extraDetail = new InvoiceDetail
                    {
                        InvoiceId = invoice.InvoiceId,
                        ItemType = InvoiceItemType.Service,
                        ServiceId = extraSvc.ServiceId,
                        Quantity = 1,
                        UnitPrice = extraSvc.Price,
                        SubTotal = extraSvc.Price
                    };
                    db.InvoiceDetails.Add(extraDetail);
                    totalAmount += extraDetail.SubTotal;
                }
            }

            // Update Total
            invoice.TotalAmount = totalAmount;
            await db.SaveChangesAsync();
        }

        // 7. DASHBOARD DEMO (TODAY / FUTURE APPOINTMENTS)
        var todaySchedules = schedules.Where(s => s.WorkDate == new DateOnly(2026, 3, 27)).ToList();
        foreach (var patient in pts)
        {
            var freeSlot = todaySchedules.FirstOrDefault(s => s.CurrentBooked < s.MaxPatients);
            if (freeSlot != null)
            {
                freeSlot.CurrentBooked++;
                var baseService = services[rnd.Next(services.Count)];
                
                var appt = new Appointment
                {
                    PatientId = patient.PatientId,
                    ScheduleId = freeSlot.ScheduleId,
                    ServiceId = baseService.ServiceId,
                    Status = AppointmentStatus.Confirmed,
                    Notes = "Đặt lịch từ hệ thống"
                };
                db.Appointments.Add(appt);
                await db.SaveChangesAsync();
            }
        }
    }
}
