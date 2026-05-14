IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260318035147_InitialCreate'
)
BEGIN
    CREATE TABLE [Medicines] (
        [MedicineId] int NOT NULL IDENTITY,
        [MedicineName] nvarchar(200) NOT NULL,
        [CategoryName] nvarchar(100) NULL,
        [StockQuantity] int NOT NULL,
        [Price] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_Medicines] PRIMARY KEY ([MedicineId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260318035147_InitialCreate'
)
BEGIN
    CREATE TABLE [Roles] (
        [RoleId] int NOT NULL IDENTITY,
        [RoleName] nvarchar(50) NOT NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY ([RoleId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260318035147_InitialCreate'
)
BEGIN
    CREATE TABLE [Services] (
        [ServiceId] int NOT NULL IDENTITY,
        [ServiceName] nvarchar(200) NOT NULL,
        [Price] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_Services] PRIMARY KEY ([ServiceId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260318035147_InitialCreate'
)
BEGIN
    CREATE TABLE [TreatmentPackages] (
        [PackageId] int NOT NULL IDENTITY,
        [PackageName] nvarchar(200) NOT NULL,
        [TotalSessions] int NOT NULL,
        [Price] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_TreatmentPackages] PRIMARY KEY ([PackageId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260318035147_InitialCreate'
)
BEGIN
    CREATE TABLE [Accounts] (
        [AccountId] int NOT NULL IDENTITY,
        [Email] nvarchar(256) NOT NULL,
        [PasswordHash] nvarchar(max) NOT NULL,
        [RoleId] int NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_Accounts] PRIMARY KEY ([AccountId]),
        CONSTRAINT [FK_Accounts_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([RoleId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260318035147_InitialCreate'
)
BEGIN
    CREATE TABLE [Doctors] (
        [DoctorId] int NOT NULL IDENTITY,
        [AccountId] int NOT NULL,
        [FullName] nvarchar(150) NOT NULL,
        [Phone] nvarchar(20) NULL,
        [Specialty] nvarchar(100) NULL,
        CONSTRAINT [PK_Doctors] PRIMARY KEY ([DoctorId]),
        CONSTRAINT [FK_Doctors_Accounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [Accounts] ([AccountId]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260318035147_InitialCreate'
)
BEGIN
    CREATE TABLE [Patients] (
        [PatientId] int NOT NULL IDENTITY,
        [AccountId] int NULL,
        [FullName] nvarchar(150) NOT NULL,
        [Phone] nvarchar(20) NOT NULL,
        [DateOfBirth] date NULL,
        [BaseMedicalHistory] nvarchar(max) NULL,
        CONSTRAINT [PK_Patients] PRIMARY KEY ([PatientId]),
        CONSTRAINT [FK_Patients_Accounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [Accounts] ([AccountId]) ON DELETE SET NULL
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260318035147_InitialCreate'
)
BEGIN
    CREATE TABLE [DoctorSchedules] (
        [ScheduleId] int NOT NULL IDENTITY,
        [DoctorId] int NOT NULL,
        [WorkDate] date NOT NULL,
        [TimeSlot] time NOT NULL,
        [MaxPatients] int NOT NULL,
        [CurrentBooked] int NOT NULL,
        CONSTRAINT [PK_DoctorSchedules] PRIMARY KEY ([ScheduleId]),
        CONSTRAINT [FK_DoctorSchedules_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [Doctors] ([DoctorId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260318035147_InitialCreate'
)
BEGIN
    CREATE TABLE [PatientTreatments] (
        [PatientTreatmentId] int NOT NULL IDENTITY,
        [PatientId] int NOT NULL,
        [PackageId] int NOT NULL,
        [PrimaryDoctorId] int NOT NULL,
        [TotalSessions] int NOT NULL,
        [UsedSessions] int NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_PatientTreatments] PRIMARY KEY ([PatientTreatmentId]),
        CONSTRAINT [FK_PatientTreatments_Doctors_PrimaryDoctorId] FOREIGN KEY ([PrimaryDoctorId]) REFERENCES [Doctors] ([DoctorId]),
        CONSTRAINT [FK_PatientTreatments_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([PatientId]),
        CONSTRAINT [FK_PatientTreatments_TreatmentPackages_PackageId] FOREIGN KEY ([PackageId]) REFERENCES [TreatmentPackages] ([PackageId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260318035147_InitialCreate'
)
BEGIN
    CREATE TABLE [Appointments] (
        [AppointmentId] int NOT NULL IDENTITY,
        [PatientId] int NOT NULL,
        [ScheduleId] int NOT NULL,
        [PatientTreatmentId] int NULL,
        [Status] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Appointments] PRIMARY KEY ([AppointmentId]),
        CONSTRAINT [FK_Appointments_DoctorSchedules_ScheduleId] FOREIGN KEY ([ScheduleId]) REFERENCES [DoctorSchedules] ([ScheduleId]),
        CONSTRAINT [FK_Appointments_PatientTreatments_PatientTreatmentId] FOREIGN KEY ([PatientTreatmentId]) REFERENCES [PatientTreatments] ([PatientTreatmentId]),
        CONSTRAINT [FK_Appointments_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([PatientId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260318035147_InitialCreate'
)
BEGIN
    CREATE TABLE [TreatmentSessionLogs] (
        [LogId] int NOT NULL IDENTITY,
        [PatientTreatmentId] int NOT NULL,
        [UsedDate] datetime2 NOT NULL,
        [PerformedBy] int NOT NULL,
        [SessionNotes] nvarchar(1000) NULL,
        CONSTRAINT [PK_TreatmentSessionLogs] PRIMARY KEY ([LogId]),
        CONSTRAINT [FK_TreatmentSessionLogs_Accounts_PerformedBy] FOREIGN KEY ([PerformedBy]) REFERENCES [Accounts] ([AccountId]),
        CONSTRAINT [FK_TreatmentSessionLogs_PatientTreatments_PatientTreatmentId] FOREIGN KEY ([PatientTreatmentId]) REFERENCES [PatientTreatments] ([PatientTreatmentId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260318035147_InitialCreate'
)
BEGIN
    CREATE TABLE [MedicalRecords] (
        [RecordId] int NOT NULL IDENTITY,
        [AppointmentId] int NOT NULL,
        [DoctorId] int NOT NULL,
        [SkinCondition] nvarchar(500) NULL,
        [Diagnosis] nvarchar(1000) NULL,
        [RecordDate] datetime2 NOT NULL,
        CONSTRAINT [PK_MedicalRecords] PRIMARY KEY ([RecordId]),
        CONSTRAINT [FK_MedicalRecords_Appointments_AppointmentId] FOREIGN KEY ([AppointmentId]) REFERENCES [Appointments] ([AppointmentId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MedicalRecords_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [Doctors] ([DoctorId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260318035147_InitialCreate'
)
BEGIN
    CREATE TABLE [SessionImages] (
        [ImageId] int NOT NULL IDENTITY,
        [LogId] int NOT NULL,
        [ImageUrl] nvarchar(500) NOT NULL,
        [UploadDate] datetime2 NOT NULL,
        CONSTRAINT [PK_SessionImages] PRIMARY KEY ([ImageId]),
        CONSTRAINT [FK_SessionImages_TreatmentSessionLogs_LogId] FOREIGN KEY ([LogId]) REFERENCES [TreatmentSessionLogs] ([LogId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260318035147_InitialCreate'
)
BEGIN
    CREATE TABLE [Invoices] (
        [InvoiceId] int NOT NULL IDENTITY,
        [RecordId] int NULL,
        [TotalAmount] decimal(18,2) NOT NULL,
        [PaymentStatus] nvarchar(max) NOT NULL,
        [CreatedDate] datetime2 NOT NULL,
        CONSTRAINT [PK_Invoices] PRIMARY KEY ([InvoiceId]),
        CONSTRAINT [FK_Invoices_MedicalRecords_RecordId] FOREIGN KEY ([RecordId]) REFERENCES [MedicalRecords] ([RecordId]) ON DELETE SET NULL
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260318035147_InitialCreate'
)
BEGIN
    CREATE TABLE [InvoiceDetails] (
        [DetailId] int NOT NULL IDENTITY,
        [InvoiceId] int NOT NULL,
        [ItemType] nvarchar(max) NOT NULL,
        [MedicineId] int NULL,
        [ServiceId] int NULL,
        [PackageId] int NULL,
        [Quantity] int NOT NULL,
        [UnitPrice] decimal(18,2) NOT NULL,
        [SubTotal] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_InvoiceDetails] PRIMARY KEY ([DetailId]),
        CONSTRAINT [FK_InvoiceDetails_Invoices_InvoiceId] FOREIGN KEY ([InvoiceId]) REFERENCES [Invoices] ([InvoiceId]) ON DELETE CASCADE,
        CONSTRAINT [FK_InvoiceDetails_Medicines_MedicineId] FOREIGN KEY ([MedicineId]) REFERENCES [Medicines] ([MedicineId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_InvoiceDetails_Services_ServiceId] FOREIGN KEY ([ServiceId]) REFERENCES [Services] ([ServiceId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_InvoiceDetails_TreatmentPackages_PackageId] FOREIGN KEY ([PackageId]) REFERENCES [TreatmentPackages] ([PackageId]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260318035147_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'MedicineId', N'CategoryName', N'MedicineName', N'Price', N'StockQuantity') AND [object_id] = OBJECT_ID(N'[Medicines]'))
        SET IDENTITY_INSERT [Medicines] ON;
    EXEC(N'INSERT INTO [Medicines] ([MedicineId], [CategoryName], [MedicineName], [Price], [StockQuantity])
    VALUES (1, N''Topical Retinoid'', N''Tretinoin 0.05%'', 150000.0, 100),
    (2, N''Topical Antibiotic'', N''Clindamycin Gel 1%'', 120000.0, 80),
    (3, N''Skincare'', N''Sunscreen SPF50+'', 250000.0, 200)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'MedicineId', N'CategoryName', N'MedicineName', N'Price', N'StockQuantity') AND [object_id] = OBJECT_ID(N'[Medicines]'))
        SET IDENTITY_INSERT [Medicines] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260318035147_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'RoleId', N'RoleName') AND [object_id] = OBJECT_ID(N'[Roles]'))
        SET IDENTITY_INSERT [Roles] ON;
    EXEC(N'INSERT INTO [Roles] ([RoleId], [RoleName])
    VALUES (1, N''Admin''),
    (2, N''Doctor''),
    (3, N''Receptionist''),
    (4, N''Cashier''),
    (5, N''Patient'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'RoleId', N'RoleName') AND [object_id] = OBJECT_ID(N'[Roles]'))
        SET IDENTITY_INSERT [Roles] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260318035147_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'ServiceId', N'Price', N'ServiceName') AND [object_id] = OBJECT_ID(N'[Services]'))
        SET IDENTITY_INSERT [Services] ON;
    EXEC(N'INSERT INTO [Services] ([ServiceId], [Price], [ServiceName])
    VALUES (1, 200000.0, N''Skin Consultation''),
    (2, 800000.0, N''Laser Acne Treatment''),
    (3, 600000.0, N''Chemical Peel'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'ServiceId', N'Price', N'ServiceName') AND [object_id] = OBJECT_ID(N'[Services]'))
        SET IDENTITY_INSERT [Services] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260318035147_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PackageId', N'PackageName', N'Price', N'TotalSessions') AND [object_id] = OBJECT_ID(N'[TreatmentPackages]'))
        SET IDENTITY_INSERT [TreatmentPackages] ON;
    EXEC(N'INSERT INTO [TreatmentPackages] ([PackageId], [PackageName], [Price], [TotalSessions])
    VALUES (1, N''Acne Treatment Basic (5 sessions)'', 3500000.0, 5),
    (2, N''Premium Skin Rejuvenation (10 sessions)'', 9000000.0, 10)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PackageId', N'PackageName', N'Price', N'TotalSessions') AND [object_id] = OBJECT_ID(N'[TreatmentPackages]'))
        SET IDENTITY_INSERT [TreatmentPackages] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260318035147_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'AccountId', N'Email', N'IsActive', N'PasswordHash', N'RoleId') AND [object_id] = OBJECT_ID(N'[Accounts]'))
        SET IDENTITY_INSERT [Accounts] ON;
    EXEC(N'INSERT INTO [Accounts] ([AccountId], [Email], [IsActive], [PasswordHash], [RoleId])
    VALUES (1, N''admin@sclinic.vn'', CAST(1 AS bit), N''$2a$11$d09YF9vFZhQkYags5Hh91eJSJPt0xdpqSEQXOFo83fo11Fqfjf4Ze'', 1)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'AccountId', N'Email', N'IsActive', N'PasswordHash', N'RoleId') AND [object_id] = OBJECT_ID(N'[Accounts]'))
        SET IDENTITY_INSERT [Accounts] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260318035147_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Accounts_Email] ON [Accounts] ([Email]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260318035147_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Accounts_RoleId] ON [Accounts] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260318035147_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Appointments_PatientId] ON [Appointments] ([PatientId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260318035147_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Appointments_PatientTreatmentId] ON [Appointments] ([PatientTreatmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260318035147_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Appointments_ScheduleId] ON [Appointments] ([ScheduleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260318035147_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Doctors_AccountId] ON [Doctors] ([AccountId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260318035147_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DoctorSchedules_DoctorId] ON [DoctorSchedules] ([DoctorId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260318035147_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_InvoiceDetails_InvoiceId] ON [InvoiceDetails] ([InvoiceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260318035147_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_InvoiceDetails_MedicineId] ON [InvoiceDetails] ([MedicineId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260318035147_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_InvoiceDetails_PackageId] ON [InvoiceDetails] ([PackageId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260318035147_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_InvoiceDetails_ServiceId] ON [InvoiceDetails] ([ServiceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260318035147_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Invoices_RecordId] ON [Invoices] ([RecordId]) WHERE [RecordId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260318035147_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_MedicalRecords_AppointmentId] ON [MedicalRecords] ([AppointmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260318035147_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_MedicalRecords_DoctorId] ON [MedicalRecords] ([DoctorId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260318035147_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Patients_AccountId] ON [Patients] ([AccountId]) WHERE [AccountId] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260318035147_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Patients_Phone] ON [Patients] ([Phone]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260318035147_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PatientTreatments_PackageId] ON [PatientTreatments] ([PackageId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260318035147_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PatientTreatments_PatientId] ON [PatientTreatments] ([PatientId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260318035147_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PatientTreatments_PrimaryDoctorId] ON [PatientTreatments] ([PrimaryDoctorId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260318035147_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Roles_RoleName] ON [Roles] ([RoleName]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260318035147_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SessionImages_LogId] ON [SessionImages] ([LogId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260318035147_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TreatmentSessionLogs_PatientTreatmentId] ON [TreatmentSessionLogs] ([PatientTreatmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260318035147_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TreatmentSessionLogs_PerformedBy] ON [TreatmentSessionLogs] ([PerformedBy]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260318035147_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260318035147_InitialCreate', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260320032449_AddCategories'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Medicines]') AND [c].[name] = N'CategoryName');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Medicines] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [Medicines] DROP COLUMN [CategoryName];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260320032449_AddCategories'
)
BEGIN
    ALTER TABLE [Medicines] ADD [CategoryId] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260320032449_AddCategories'
)
BEGIN
    CREATE TABLE [Categories] (
        [CategoryId] int NOT NULL IDENTITY,
        [CategoryName] nvarchar(100) NOT NULL,
        [Description] nvarchar(max) NULL,
        CONSTRAINT [PK_Categories] PRIMARY KEY ([CategoryId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260320032449_AddCategories'
)
BEGIN
    EXEC(N'UPDATE [Accounts] SET [PasswordHash] = N''$2a$11$ZDkvtEDJffsYkDCDW59KqOVaL/i32ZSHSbj7q41GF7Ycj4M6PJj/a''
    WHERE [AccountId] = 1;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260320032449_AddCategories'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'CategoryId', N'CategoryName', N'Description') AND [object_id] = OBJECT_ID(N'[Categories]'))
        SET IDENTITY_INSERT [Categories] ON;
    EXEC(N'INSERT INTO [Categories] ([CategoryId], [CategoryName], [Description])
    VALUES (1, N''Thuốc Kê Đơn'', N''Thuốc kê đơn bắc sĩ''),
    (2, N''Dược Mỹ Phẩm'', N''Mỹ phẩm được liệu''),
    (3, N''Thực Phẩm Chức Năng'', N''Vitamin và thực phẩm bổ sung'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'CategoryId', N'CategoryName', N'Description') AND [object_id] = OBJECT_ID(N'[Categories]'))
        SET IDENTITY_INSERT [Categories] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260320032449_AddCategories'
)
BEGIN
    EXEC(N'UPDATE [Medicines] SET [CategoryId] = 1
    WHERE [MedicineId] = 1;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260320032449_AddCategories'
)
BEGIN
    EXEC(N'UPDATE [Medicines] SET [CategoryId] = 1
    WHERE [MedicineId] = 2;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260320032449_AddCategories'
)
BEGIN
    EXEC(N'UPDATE [Medicines] SET [CategoryId] = 2
    WHERE [MedicineId] = 3;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260320032449_AddCategories'
)
BEGIN
                    UPDATE Medicines SET CategoryId = 1  -- Thuốc Kê Đơn
                    WHERE MedicineId IN (2, 3);          -- Kháng sinh + Kem Klenzit
                    UPDATE Medicines SET CategoryId = 2  -- Dược Mỹ Phẩm
                    WHERE MedicineId IN (4,5,6,7,8,9,10);
                    -- Fallback: any remaining rows with CategoryId=0 → Dược Mỹ Phẩm
                    UPDATE Medicines SET CategoryId = 2
                    WHERE CategoryId = 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260320032449_AddCategories'
)
BEGIN
    CREATE INDEX [IX_Medicines_CategoryId] ON [Medicines] ([CategoryId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260320032449_AddCategories'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Categories_CategoryName] ON [Categories] ([CategoryName]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260320032449_AddCategories'
)
BEGIN
    ALTER TABLE [Medicines] ADD CONSTRAINT [FK_Medicines_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([CategoryId]) ON DELETE CASCADE;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260320032449_AddCategories'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260320032449_AddCategories', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260325035955_AddAppointmentServiceNotesAndMedicineSoftDelete'
)
BEGIN
    ALTER TABLE [Medicines] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260325035955_AddAppointmentServiceNotesAndMedicineSoftDelete'
)
BEGIN
    ALTER TABLE [Appointments] ADD [Notes] nvarchar(max) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260325035955_AddAppointmentServiceNotesAndMedicineSoftDelete'
)
BEGIN
    ALTER TABLE [Appointments] ADD [ServiceId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260325035955_AddAppointmentServiceNotesAndMedicineSoftDelete'
)
BEGIN
    EXEC(N'UPDATE [Accounts] SET [PasswordHash] = N''$2a$11$sudMqWRGd1mrPAdlr2lZOuxv5/IIb.0mDf1KmpQrALr9O9XgGY0bO''
    WHERE [AccountId] = 1;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260325035955_AddAppointmentServiceNotesAndMedicineSoftDelete'
)
BEGIN
    EXEC(N'UPDATE [Medicines] SET [IsDeleted] = CAST(0 AS bit)
    WHERE [MedicineId] = 1;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260325035955_AddAppointmentServiceNotesAndMedicineSoftDelete'
)
BEGIN
    EXEC(N'UPDATE [Medicines] SET [IsDeleted] = CAST(0 AS bit)
    WHERE [MedicineId] = 2;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260325035955_AddAppointmentServiceNotesAndMedicineSoftDelete'
)
BEGIN
    EXEC(N'UPDATE [Medicines] SET [IsDeleted] = CAST(0 AS bit)
    WHERE [MedicineId] = 3;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260325035955_AddAppointmentServiceNotesAndMedicineSoftDelete'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260325035955_AddAppointmentServiceNotesAndMedicineSoftDelete', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260325134322_UpdateServiceNames'
)
BEGIN
    EXEC(N'UPDATE [Accounts] SET [PasswordHash] = N''$2a$11$c7ORiYZN6t1ABKyTgIUk/up6dE3L2tzWgdDd6BrFp6X6rV//TkMcW''
    WHERE [AccountId] = 1;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260325134322_UpdateServiceNames'
)
BEGIN
                    UPDATE Services SET ServiceName = N'Khám & Soi da 3D',           Price = 300000 WHERE ServiceId = 1;
                    UPDATE Services SET ServiceName = N'Lấy nhân mụn chuẩn Y khoa',  Price = 400000 WHERE ServiceId = 2;
                    UPDATE Services SET ServiceName = N'Peel da sinh học BHA/AHA',    Price = 800000 WHERE ServiceId = 3;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260325134322_UpdateServiceNames'
)
BEGIN
                    SET IDENTITY_INSERT [Services] ON;
                    IF NOT EXISTS (SELECT 1 FROM Services WHERE ServiceId = 4)
                        INSERT INTO Services (ServiceId, ServiceName, Price) VALUES (4, N'Điện di Ion phục hồi B5', 500000);
                    IF NOT EXISTS (SELECT 1 FROM Services WHERE ServiceId = 5)
                        INSERT INTO Services (ServiceId, ServiceName, Price) VALUES (5, N'Chiếu ánh sáng Omega Light', 200000);
                    IF NOT EXISTS (SELECT 1 FROM Services WHERE ServiceId = 6)
                        INSERT INTO Services (ServiceId, ServiceName, Price) VALUES (6, N'Tư vấn lập phác đồ cá nhân', 0);
                    SET IDENTITY_INSERT [Services] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260325134322_UpdateServiceNames'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260325134322_UpdateServiceNames', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260326114643_AddInvoiceAppointmentId'
)
BEGIN
    ALTER TABLE [Invoices] ADD [AppointmentId] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260326114643_AddInvoiceAppointmentId'
)
BEGIN
    EXEC(N'UPDATE [Accounts] SET [PasswordHash] = N''$2a$11$2WybcZPse5n/163pLuDFd.CNwrexUR/tujVEeGaXKhqSbdARberqu''
    WHERE [AccountId] = 1;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260326114643_AddInvoiceAppointmentId'
)
BEGIN
    CREATE INDEX [IX_Invoices_AppointmentId] ON [Invoices] ([AppointmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260326114643_AddInvoiceAppointmentId'
)
BEGIN
    ALTER TABLE [Invoices] ADD CONSTRAINT [FK_Invoices_Appointments_AppointmentId] FOREIGN KEY ([AppointmentId]) REFERENCES [Appointments] ([AppointmentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260326114643_AddInvoiceAppointmentId'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260326114643_AddInvoiceAppointmentId', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260326232534_AddServiceType'
)
BEGIN
    ALTER TABLE [Services] ADD [ServiceType] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260326232534_AddServiceType'
)
BEGIN
    EXEC(N'UPDATE [Accounts] SET [PasswordHash] = N''$2a$11$ccmaPwbjjZpiQyivhFt3PO9heFbK6ITQFkm4Ut5XWfjnT5sUyn2sC''
    WHERE [AccountId] = 1;
    SELECT @@ROWCOUNT');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260326232534_AddServiceType'
)
BEGIN
    CREATE INDEX [IX_Appointments_ServiceId] ON [Appointments] ([ServiceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260326232534_AddServiceType'
)
BEGIN
    ALTER TABLE [Appointments] ADD CONSTRAINT [FK_Appointments_Services_ServiceId] FOREIGN KEY ([ServiceId]) REFERENCES [Services] ([ServiceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260326232534_AddServiceType'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260326232534_AddServiceType', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260326232555_AddServiceTypeData'
)
BEGIN
                    UPDATE Services SET ServiceType = 1 WHERE ServiceId = 1; -- Khám & Soi da 3D
                    UPDATE Services SET ServiceType = 2 WHERE ServiceId = 2; -- Lấy nhân mụn
                    UPDATE Services SET ServiceType = 2 WHERE ServiceId = 3; -- Peel da
                    UPDATE Services SET ServiceType = 2 WHERE ServiceId = 4; -- Điện di Ion
                    UPDATE Services SET ServiceType = 2 WHERE ServiceId = 5; -- Chiếu ánh sáng
                    UPDATE Services SET ServiceType = 1, Price = 500000 WHERE ServiceId = 6; -- Tư vấn lập phác đồ
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260326232555_AddServiceTypeData'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260326232555_AddServiceTypeData', N'8.0.0');
END;
GO

COMMIT;
GO

