using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SClinic.Data.Migrations
{
    public partial class UpdateServiceNames : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update admin password hash
            migrationBuilder.UpdateData(
                table: "Accounts",
                keyColumn: "AccountId",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$c7ORiYZN6t1ABKyTgIUk/up6dE3L2tzWgdDd6BrFp6X6rV//TkMcW");

            // Rename existing services to Vietnamese
            migrationBuilder.Sql(@"
                UPDATE Services SET ServiceName = N'Khám & Soi da 3D',           Price = 300000 WHERE ServiceId = 1;
                UPDATE Services SET ServiceName = N'Lấy nhân mụn chuẩn Y khoa',  Price = 400000 WHERE ServiceId = 2;
                UPDATE Services SET ServiceName = N'Peel da sinh học BHA/AHA',    Price = 800000 WHERE ServiceId = 3;
            ");

            // Insert services 4-6 only if they don't exist
            migrationBuilder.Sql(@"
                SET IDENTITY_INSERT [Services] ON;
                IF NOT EXISTS (SELECT 1 FROM Services WHERE ServiceId = 4)
                    INSERT INTO Services (ServiceId, ServiceName, Price) VALUES (4, N'Điện di Ion phục hồi B5', 500000);
                IF NOT EXISTS (SELECT 1 FROM Services WHERE ServiceId = 5)
                    INSERT INTO Services (ServiceId, ServiceName, Price) VALUES (5, N'Chiếu ánh sáng Omega Light', 200000);
                IF NOT EXISTS (SELECT 1 FROM Services WHERE ServiceId = 6)
                    INSERT INTO Services (ServiceId, ServiceName, Price) VALUES (6, N'Tư vấn lập phác đồ cá nhân', 0);
                SET IDENTITY_INSERT [Services] OFF;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Accounts",
                keyColumn: "AccountId",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$sudMqWRGd1mrPAdlr2lZOuxv5/IIb.0mDf1KmpQrALr9O9XgGY0bO");

            migrationBuilder.Sql(@"
                UPDATE Services SET ServiceName = 'Skin Consultation',    Price = 200000 WHERE ServiceId = 1;
                UPDATE Services SET ServiceName = 'Laser Acne Treatment', Price = 800000 WHERE ServiceId = 2;
                UPDATE Services SET ServiceName = 'Chemical Peel',        Price = 600000 WHERE ServiceId = 3;
                DELETE FROM Services WHERE ServiceId IN (4, 5, 6);
            ");
        }
    }
}
