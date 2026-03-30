    using Microsoft.EntityFrameworkCore;
    using SClinic.Models;

    namespace SClinic.Data;

    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : DbContext(options)
    {
        public DbSet<Role> Roles { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<DoctorSchedule> DoctorSchedules { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<MedicalRecord> MedicalRecords { get; set; }
        public DbSet<TreatmentPackage> TreatmentPackages { get; set; }
        public DbSet<PatientTreatment> PatientTreatments { get; set; }
        public DbSet<TreatmentSessionLog> TreatmentSessionLogs { get; set; }
        public DbSet<SessionImage> SessionImages { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Medicine> Medicines { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceDetail> InvoiceDetails { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── Unique Constraints ─────────────────────────────────────────────
            modelBuilder.Entity<Role>()
                .HasIndex(r => r.RoleName).IsUnique();

            modelBuilder.Entity<Account>()
                .HasIndex(a => a.Email).IsUnique();

            modelBuilder.Entity<Patient>()
                .HasIndex(p => p.Phone).IsUnique();

            // ── Decimal Precision (18,2) ───────────────────────────────────────
            modelBuilder.Entity<Service>()
                .Property(s => s.Price).HasColumnType("decimal(18,2)");

            modelBuilder.Entity<TreatmentPackage>()
                .Property(tp => tp.Price).HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Medicine>()
                .Property(m => m.Price).HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Invoice>()
                .Property(i => i.TotalAmount).HasColumnType("decimal(18,2)");

            modelBuilder.Entity<InvoiceDetail>()
                .Property(id => id.UnitPrice).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<InvoiceDetail>()
                .Property(id => id.SubTotal).HasColumnType("decimal(18,2)");

            // ── Enum to String Conversions ─────────────────────────────────────
            modelBuilder.Entity<Appointment>()
                .Property(a => a.Status).HasConversion<string>();

            modelBuilder.Entity<PatientTreatment>()
                .Property(pt => pt.Status).HasConversion<string>();

            modelBuilder.Entity<Invoice>()
                .Property(i => i.PaymentStatus).HasConversion<string>();

            modelBuilder.Entity<InvoiceDetail>()
                .Property(id => id.ItemType).HasConversion<string>();

            // ── 1-to-1 Relationships ───────────────────────────────────────────
            modelBuilder.Entity<Account>()
                .HasOne(a => a.Doctor)
                .WithOne(d => d.Account)
                .HasForeignKey<Doctor>(d => d.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Account>()
                .HasOne(a => a.Patient)
                .WithOne(p => p.Account)
                .HasForeignKey<Patient>(p => p.AccountId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<MedicalRecord>()
                .HasOne(mr => mr.Appointment)
                .WithOne(a => a.MedicalRecord)
                .HasForeignKey<MedicalRecord>(mr => mr.AppointmentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Record)
                .WithOne(mr => mr.Invoice)
                .HasForeignKey<Invoice>(i => i.RecordId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // ── Cascade Rules ──────────────────────────────────────────────────
            // SQL Server disallows multiple cascade paths to the same table.
            // Strategy: use NoAction on all FKs; handle deletions in service layer.

            // Appointments → Patient (NoAction to prevent cycle via PatientTreatments)
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Patient)
                .WithMany(p => p.Appointments)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.NoAction);

            // Appointments → DoctorSchedule (NoAction)
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Schedule)
                .WithMany(s => s.Appointments)
                .HasForeignKey(a => a.ScheduleId)
                .OnDelete(DeleteBehavior.NoAction);

            // Appointments → PatientTreatment (nullable, SetNull path already handled)
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.PatientTreatment)
                .WithMany(pt => pt.Appointments)
                .HasForeignKey(a => a.PatientTreatmentId)
                .OnDelete(DeleteBehavior.NoAction);

            // PatientTreatments → Patient (NoAction to break cycle)
            modelBuilder.Entity<PatientTreatment>()
                .HasOne(pt => pt.Patient)
                .WithMany(p => p.PatientTreatments)
                .HasForeignKey(pt => pt.PatientId)
                .OnDelete(DeleteBehavior.NoAction);

            // PatientTreatments → TreatmentPackage (NoAction)
            modelBuilder.Entity<PatientTreatment>()
                .HasOne(pt => pt.Package)
                .WithMany(pkg => pkg.PatientTreatments)
                .HasForeignKey(pt => pt.PackageId)
                .OnDelete(DeleteBehavior.NoAction);

            // PatientTreatments → PrimaryDoctor (NoAction)
            modelBuilder.Entity<PatientTreatment>()
                .HasOne(pt => pt.PrimaryDoctor)
                .WithMany(d => d.PatientTreatments)
                .HasForeignKey(pt => pt.PrimaryDoctorId)
                .OnDelete(DeleteBehavior.NoAction);

            // DoctorSchedules → Doctor (NoAction)
            modelBuilder.Entity<DoctorSchedule>()
                .HasOne(s => s.Doctor)
                .WithMany(d => d.Schedules)
                .HasForeignKey(s => s.DoctorId)
                .OnDelete(DeleteBehavior.NoAction);

            // MedicalRecord → Doctor (NoAction)
            modelBuilder.Entity<MedicalRecord>()
                .HasOne(mr => mr.Doctor)
                .WithMany(d => d.MedicalRecords)
                .HasForeignKey(mr => mr.DoctorId)
                .OnDelete(DeleteBehavior.NoAction);

            // TreatmentSessionLog → PerformedBy Account (NoAction)
            modelBuilder.Entity<TreatmentSessionLog>()
                .HasOne(tsl => tsl.PerformedByAccount)
                .WithMany()
                .HasForeignKey(tsl => tsl.PerformedBy)
                .OnDelete(DeleteBehavior.NoAction);

            // Restrict delete: cannot delete Medicine/Service/Package already on an Invoice
            modelBuilder.Entity<InvoiceDetail>()
                .HasOne(id => id.Medicine)
                .WithMany(m => m.InvoiceDetails)
                .HasForeignKey(id => id.MedicineId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InvoiceDetail>()
                .HasOne(id => id.Service)
                .WithMany(s => s.InvoiceDetails)
                .HasForeignKey(id => id.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InvoiceDetail>()
                .HasOne(id => id.Package)
                .WithMany(p => p.InvoiceDetails)
                .HasForeignKey(id => id.PackageId)
                .OnDelete(DeleteBehavior.Restrict);


            // ── Seed Data ──────────────────────────────────────────────────────
            modelBuilder.Entity<Role>().HasData(
                new Role { RoleId = 1, RoleName = "Admin" },
                new Role { RoleId = 2, RoleName = "Doctor" },
                new Role { RoleId = 3, RoleName = "Receptionist" },
                new Role { RoleId = 4, RoleName = "Cashier" },
                new Role { RoleId = 5, RoleName = "Patient" }
            );

            // Default admin account (password: Admin@123)
            modelBuilder.Entity<Account>().HasData(
                new Account
                {
                    AccountId = 1,
                    Email = "admin@sclinic.vn",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    RoleId = 1,
                    IsActive = true
                }
            );

            modelBuilder.Entity<TreatmentPackage>().HasData(
                new TreatmentPackage
                {
                    PackageId = 1,
                    PackageName = "Acne Treatment Basic (5 sessions)",
                    TotalSessions = 5,
                    Price = 3_500_000m
                },
                new TreatmentPackage
                {
                    PackageId = 2,
                    PackageName = "Premium Skin Rejuvenation (10 sessions)",
                    TotalSessions = 10,
                    Price = 9_000_000m
                }
            );


            // ── Category unique index ──────────────────────────────────────────
            modelBuilder.Entity<Category>()
                .HasIndex(c => c.CategoryName).IsUnique();

            modelBuilder.Entity<Category>().HasData(
                new Category { CategoryId = 1, CategoryName = "Thuốc Kê Đơn",  Description = "Thuốc kê đơn bắc sĩ" },
                new Category { CategoryId = 2, CategoryName = "Dược Mỹ Phẩm",   Description = "Mỹ phẩm được liệu" },
                new Category { CategoryId = 3, CategoryName = "Thực Phẩm Chức Năng", Description = "Vitamin và thực phẩm bổ sung" }
            );

            modelBuilder.Entity<Medicine>().HasData(
                new Medicine { MedicineId = 1, MedicineName = "Tretinoin 0.05%",    CategoryId = 1, StockQuantity = 100, Price = 150_000m },
                new Medicine { MedicineId = 2, MedicineName = "Clindamycin Gel 1%", CategoryId = 1, StockQuantity = 80,  Price = 120_000m },
                new Medicine { MedicineId = 3, MedicineName = "Sunscreen SPF50+",   CategoryId = 2, StockQuantity = 200, Price = 250_000m }
            );
        }
    }
