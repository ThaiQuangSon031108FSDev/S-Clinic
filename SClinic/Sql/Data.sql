-- =========================================================================
-- S-CLINIC SEED DATA (THÁNG 02 - THÁNG 03/2026)
-- CHẠY SCRIPT NÀY SAU KHI EF CORE ĐÃ UPDATE-DATABASE TẠO BẢNG XONG
--
-- ✅ FIX #1: Roles dùng tên tiếng Anh để khớp [Authorize(Roles="Doctor")] 
-- ✅ FIX #2: Phones Patient 13,14,15 đã được đổi (tránh UNIQUE constraint)
-- ✅ FIX #3: PasswordHash dùng BCrypt hợp lệ (password gốc: Sclinic@123)
-- ✅ FIX #4: Invoices.RecordId = NULL được phép (FK đã sửa nullable)
-- =========================================================================

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- 1. XÓA DỮ LIỆU CŨ (theo thứ tự FK để tránh lỗi constraint)
-- Giữ lại __EFMigrationsHistory
DELETE FROM InvoiceDetails;
DELETE FROM Invoices;
DELETE FROM SessionImages;
DELETE FROM TreatmentSessionLogs;
DELETE FROM Appointments;
DELETE FROM MedicalRecords;
DELETE FROM DoctorSchedules;
DELETE FROM PatientTreatments;
DELETE FROM Patients;
DELETE FROM Doctors;
DELETE FROM Accounts;
DELETE FROM Roles;
DELETE FROM TreatmentPackages;
DELETE FROM Services;
DELETE FROM Medicines;   -- phải xóa trước Categories (FK)
DELETE FROM Categories;
GO

-- 2. BẢNG ROLES — Tên tiếng Anh để [Authorize(Roles="...")] hoạt động
SET IDENTITY_INSERT Roles ON;
INSERT INTO Roles (RoleId, RoleName) VALUES 
(1, N'Admin'),
(2, N'Doctor'),
(3, N'Receptionist'),
(4, N'Cashier'),
(5, N'Patient');
SET IDENTITY_INSERT Roles OFF;
GO

-- 3. BẢNG ACCOUNTS
-- ⚠️  PasswordHash bên dưới = BCrypt của "Sclinic@123" (cost factor 11)
-- Tất cả tài khoản nhân viên đều dùng cùng mật khẩu để demo dễ test
SET IDENTITY_INSERT Accounts ON;
INSERT INTO Accounts (AccountId, Email, PasswordHash, RoleId, IsActive) VALUES 
(1,  'admin@sclinic.vn',          '$2a$11$K7bFqNK0uJb8uJsQoK8J1OqvhGkM5BT3wYNg9KrjG2f.vbNpEhQLa', 1, 1),
-- 5 Bác sĩ
(2,  'bs.lethib@sclinic.vn',      '$2a$11$K7bFqNK0uJb8uJsQoK8J1OqvhGkM5BT3wYNg9KrjG2f.vbNpEhQLa', 2, 1),
(3,  'bs.nguyenc@sclinic.vn',     '$2a$11$K7bFqNK0uJb8uJsQoK8J1OqvhGkM5BT3wYNg9KrjG2f.vbNpEhQLa', 2, 1),
(4,  'bs.trand@sclinic.vn',       '$2a$11$K7bFqNK0uJb8uJsQoK8J1OqvhGkM5BT3wYNg9KrjG2f.vbNpEhQLa', 2, 1),
(5,  'bs.phamhoange@sclinic.vn',  '$2a$11$K7bFqNK0uJb8uJsQoK8J1OqvhGkM5BT3wYNg9KrjG2f.vbNpEhQLa', 2, 1),
(6,  'bs.vothif@sclinic.vn',      '$2a$11$K7bFqNK0uJb8uJsQoK8J1OqvhGkM5BT3wYNg9KrjG2f.vbNpEhQLa', 2, 1),
-- 4 Lễ tân
(7,  'letan1@sclinic.vn',         '$2a$11$K7bFqNK0uJb8uJsQoK8J1OqvhGkM5BT3wYNg9KrjG2f.vbNpEhQLa', 3, 1),
(8,  'letan2@sclinic.vn',         '$2a$11$K7bFqNK0uJb8uJsQoK8J1OqvhGkM5BT3wYNg9KrjG2f.vbNpEhQLa', 3, 1),
(9,  'letan3@sclinic.vn',         '$2a$11$K7bFqNK0uJb8uJsQoK8J1OqvhGkM5BT3wYNg9KrjG2f.vbNpEhQLa', 3, 1),
(10, 'letan4@sclinic.vn',         '$2a$11$K7bFqNK0uJb8uJsQoK8J1OqvhGkM5BT3wYNg9KrjG2f.vbNpEhQLa', 3, 1),
-- 2 Thu ngân
(11, 'thungan1@sclinic.vn',       '$2a$11$K7bFqNK0uJb8uJsQoK8J1OqvhGkM5BT3wYNg9KrjG2f.vbNpEhQLa', 4, 1),
(12, 'thungan2@sclinic.vn',       '$2a$11$K7bFqNK0uJb8uJsQoK8J1OqvhGkM5BT3wYNg9KrjG2f.vbNpEhQLa', 4, 1),
-- Tài khoản bệnh nhân
(13, 'thaiquangson@gmail.com',    '$2a$11$K7bFqNK0uJb8uJsQoK8J1OqvhGkM5BT3wYNg9KrjG2f.vbNpEhQLa', 5, 1),
(14, 'phuonganh.tran@gmail.com',  '$2a$11$K7bFqNK0uJb8uJsQoK8J1OqvhGkM5BT3wYNg9KrjG2f.vbNpEhQLa', 5, 1);
SET IDENTITY_INSERT Accounts OFF;
GO

-- 4. BẢNG DOCTORS
SET IDENTITY_INSERT Doctors ON;
INSERT INTO Doctors (DoctorId, AccountId, FullName, Phone, Specialty) VALUES 
(1, 2, N'Bs. Lê Thị B',      '0901000001', N'Chuyên khoa II Da liễu - Trị Mụn'),
(2, 3, N'Bs. Nguyễn Văn C',  '0901000002', N'Chuyên khoa Laser & Phục hồi da'),
(3, 4, N'Bs. Trần Thị D',    '0901000003', N'Chuyên khoa Thẩm mỹ Nội khoa'),
(4, 5, N'Bs. Phạm Hoàng E',  '0901000004', N'Chuyên khoa Trị Nám - Tàn nhang'),
(5, 6, N'Bs. Võ Thị F',      '0901000005', N'Bác sĩ Da liễu Tổng quát');
SET IDENTITY_INSERT Doctors OFF;
GO

-- 5. BẢNG PATIENTS (15 Khách hàng)
-- ✅ FIX #2: Phones PatientId 13,14,15 đã đổi thành SĐT duy nhất
SET IDENTITY_INSERT Patients ON;
INSERT INTO Patients (PatientId, AccountId, FullName, Phone, DateOfBirth, BaseMedicalHistory) VALUES 
(1,  13, N'Thái Quang Sơn',      '0987654321', '2002-05-15', N'Dị ứng Penicillin. Da nhạy cảm cồn.'),
(2,  14, N'Trần Phương Anh',     '0912345678', '1998-10-20', N'Không có tiền sử dị ứng'),
(3,  NULL, N'Nguyễn Văn A',      '0933100001', '1995-02-12', N'Dị ứng hải sản'),
(4,  NULL, N'Lê Thị C',          '0933100002', '2000-11-05', N'Đang dùng Retinol 1%'),
(5,  NULL, N'Phạm Văn D',        '0933100003', '1990-08-30', N'Dị ứng Paracetamol'),
(6,  NULL, N'Hoàng Thanh Trúc',  '0966777888', '1999-01-25', N'Không'),
(7,  NULL, N'Vũ Hải Đăng',       '0977888999', '2001-04-14', N'Da mỏng đỏ, giãn mao mạch'),
(8,  NULL, N'Bùi Thị Mỹ Hạnh',  '0988999111', '1996-09-09', N'Không'),
(9,  NULL, N'Đặng Tuấn Anh',     '0999000111', '1992-12-12', N'Không'),
(10, NULL, N'Ngô Quý Đôn',       '0900111222', '1988-03-03', N'Tiểu đường tuýp 2'),
(11, NULL, N'Dương Thúy Quỳnh',  '0911222333', '1997-07-07', N'Không'),
(12, NULL, N'Lý Hạo Nam',        '0922333444', '2003-06-18', N'Không'),
(13, NULL, N'Trịnh Hải Yến',     '0933500001', '1994-05-22', N'Dị ứng thời tiết'),
(14, NULL, N'Mai Bảo Ngọc',      '0933500002', '2000-02-28', N'Không'),
(15, NULL, N'Tạ Quang Thắng',    '0933500003', '1991-11-11', N'Huyết áp cao');
SET IDENTITY_INSERT Patients OFF;
GO

-- 6. SERVICES & TREATMENT PACKAGES
SET IDENTITY_INSERT Services ON;
INSERT INTO Services (ServiceId, ServiceName, Price) VALUES 
(1, N'Khám & Soi da 3D',                       300000.00),
(2, N'Lấy nhân mụn chuẩn Y khoa',              400000.00),
(3, N'Peel da sinh học BHA/AHA',                800000.00),
(4, N'Điện di Ion phục hồi B5',                 500000.00),
(5, N'Chiếu ánh sáng sinh học Omega Light',     200000.00);
SET IDENTITY_INSERT Services OFF;

SET IDENTITY_INSERT TreatmentPackages ON;
INSERT INTO TreatmentPackages (PackageId, PackageName, TotalSessions, Price) VALUES 
(1, N'Liệu trình Trị Mụn Chuẩn Y Khoa',        10, 5500000.00),
(2, N'Gói Phục Hồi Da Nhiễm Corticoid',          8, 6000000.00),
(3, N'Trẻ Hóa Da Cấy Mesotherapy',               5, 12000000.00),
(4, N'Trị Nám / Tàn Nhang Laser Pico',           12, 15000000.00),
(5, N'Triệt Lông Vùng Nách Diode Laser',         10, 2500000.00);
SET IDENTITY_INSERT TreatmentPackages OFF;
GO

-- 7. CATEGORIES
SET IDENTITY_INSERT Categories ON;
INSERT INTO Categories (CategoryId, CategoryName, Description) VALUES
(1, N'Thuốc Kê Đơn',           N'Thuốc kê đơn bác sĩ'),
(2, N'Dược Mỹ Phẩm',           N'Mỹ phẩm được liệu'),
(3, N'Thực Phẩm Chức Năng',    N'Vitamin và thực phẩm bổ sung');
SET IDENTITY_INSERT Categories OFF;
GO

-- 8. MEDICINES (CategoryId: 1=Thuốc Kê Đơn, 2=Dược Mỹ Phẩm)
SET IDENTITY_INSERT Medicines ON;
INSERT INTO Medicines (MedicineId, MedicineName, CategoryId, StockQuantity, Price) VALUES
(1,  N'Isotretinoin 10mg (Acnocut)',              1, 150, 15000.00),
(2,  N'Kháng sinh Azithromycin 500mg',           1,   3, 15000.00),
(3,  N'Kem bôi trị mụn Klenzit MS',              1,  50, 120000.00),
(4,  N'Kem phục hồi B5 La Roche-Posay',          2,  45, 420000.00),
(5,  N'Serum Niacinamide 10% Paula''s Choice',   2,  20, 1250000.00),
(6,  N'Sữa rửa mặt Cerave Foaming 236ml',        2,  80, 380000.00),
(7,  N'Kem chống nắng MartiDerm Proteos',         2,  30, 1150000.00),
(8,  N'Toner BHA 2% Obagi Medical',              2,  15, 850000.00),
(9,  N'Dung dịch chấm mụn Mario Badescu',        2,  25, 450000.00),
(10, N'Serum Vitamin C 15% Vichy',               2,  10, 950000.00);
SET IDENTITY_INSERT Medicines OFF;
GO

-- 8. PATIENT TREATMENTS
SET IDENTITY_INSERT PatientTreatments ON;
INSERT INTO PatientTreatments (PatientTreatmentId, PatientId, PackageId, PrimaryDoctorId, TotalSessions, UsedSessions, Status) VALUES 
(1, 1, 1, 1, 10, 4, N'Active'),  -- Thái Quang Sơn: gói mụn 10 buổi, đã dùng 4
(2, 2, 5, 3, 10, 2, N'Active');  -- Phương Anh: gói triệt lông, đã dùng 2
SET IDENTITY_INSERT PatientTreatments OFF;
GO

-- 9. DOCTOR SCHEDULES
SET IDENTITY_INSERT DoctorSchedules ON;
INSERT INTO DoctorSchedules (ScheduleId, DoctorId, WorkDate, TimeSlot, MaxPatients, CurrentBooked) VALUES 
(1, 1, '2026-03-05', '10:00:00', 1, 1),  -- Lịch quá khứ
(2, 1, '2026-03-12', '15:30:00', 1, 1),  -- Lịch quá khứ
(3, 1, '2026-03-18', '08:45:00', 1, 1),  -- Hôm nay - Nguyễn Văn A (Pending)
(4, 1, '2026-03-18', '09:30:00', 1, 1),  -- Hôm nay - Lê Thị C (Confirmed)
(5, 2, '2026-03-18', '10:15:00', 1, 1),  -- Hôm nay - Phạm Văn D (Completed)
(6, 1, '2026-03-19', '09:00:00', 1, 1),  -- Ngày mai - Thái Quang Sơn
(7, 1, '2026-03-19', '14:00:00', 1, 1),  -- Ngày mai - Phương Anh
(8, 1, '2026-03-19', '15:30:00', 1, 0);  -- Ngày mai - CÒN TRỐNG
SET IDENTITY_INSERT DoctorSchedules OFF;
GO

-- 10. APPOINTMENTS
SET IDENTITY_INSERT Appointments ON;
INSERT INTO Appointments (AppointmentId, PatientId, ScheduleId, PatientTreatmentId, Status) VALUES 
(1, 1, 1, 1, N'Completed'),  -- Sơn buổi 3 (gói mụn)
(2, 1, 2, 1, N'Completed'),  -- Sơn buổi 4 (gói mụn)
(3, 3, 3, NULL, N'Pending'),   -- Nguyễn Văn A chờ khám hôm nay
(4, 4, 4, NULL, N'Confirmed'), -- Lê Thị C đang khám hôm nay
(5, 5, 5, NULL, N'Completed'), -- Phạm Văn D đã khám xong, chờ thanh toán
(6, 1, 6, 1,    N'Confirmed'), -- Sơn hẹn ngày mai (buổi 5 gói mụn)
(7, 2, 7, 2,    N'Confirmed'); -- Phương Anh hẹn ngày mai (gói triệt lông)
SET IDENTITY_INSERT Appointments OFF;
GO

-- 11. INVOICES (RecordId = NULL cho phép vì FK đã sửa thành nullable)
SET IDENTITY_INSERT Invoices ON;
INSERT INTO Invoices (InvoiceId, RecordId, TotalAmount, PaymentStatus, CreatedDate) VALUES 
(1, NULL, 5500000.00,  N'Paid',    '2026-02-20'), -- Hóa đơn cũ Sơn mua gói mụn
(2, NULL, 12500000.00, N'Paid',    '2026-03-18'), -- Doanh thu hôm nay (gói Meso + kem)
(3, NULL, 1250000.00,  N'Pending', '2026-03-18'); -- Hóa đơn D chờ thu ngân
SET IDENTITY_INSERT Invoices OFF;
GO

-- 12. INVOICE DETAILS
SET IDENTITY_INSERT InvoiceDetails ON;
INSERT INTO InvoiceDetails (DetailId, InvoiceId, ItemType, PackageId, MedicineId, ServiceId, Quantity, UnitPrice, SubTotal) VALUES 
(1, 1, N'Package',  1,    NULL, NULL, 1, 5500000.00,  5500000.00),   -- Gói mụn
(2, 2, N'Package',  3,    NULL, NULL, 1, 12000000.00, 12000000.00),  -- Gói Meso
(3, 2, N'Medicine', NULL, 4,    NULL, 1, 500000.00,   500000.00),    -- Kem B5
(4, 3, N'Service',  NULL, NULL, 3,   1, 800000.00,   800000.00),    -- Peel BHA
(5, 3, N'Medicine', NULL, 9,    NULL, 1, 450000.00,   450000.00);   -- Chấm mụn
SET IDENTITY_INSERT InvoiceDetails OFF;
GO

PRINT N'✅ SEED DATA ĐÃ ĐƯỢC ĐỔ THÀNH CÔNG VÀO CƠ SỞ DỮ LIỆU S-CLINIC!';
PRINT N'';
PRINT N'📌 Thông tin đăng nhập demo (tất cả đều dùng mật khẩu: Sclinic@123):';
PRINT N'   Admin       : admin@sclinic.vn';
PRINT N'   Bác sĩ      : bs.lethib@sclinic.vn';
PRINT N'   Lễ tân      : letan1@sclinic.vn';
PRINT N'   Thu ngân    : thungan1@sclinic.vn';
PRINT N'   Bệnh nhân 1 : thaiquangson@gmail.com';
PRINT N'   Bệnh nhân 2 : phuonganh.tran@gmail.com';