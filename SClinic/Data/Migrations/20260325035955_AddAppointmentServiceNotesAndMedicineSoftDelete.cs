using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SClinic.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentServiceNotesAndMedicineSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Medicines",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Appointments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ServiceId",
                table: "Appointments",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Accounts",
                keyColumn: "AccountId",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$sudMqWRGd1mrPAdlr2lZOuxv5/IIb.0mDf1KmpQrALr9O9XgGY0bO");

            migrationBuilder.UpdateData(
                table: "Medicines",
                keyColumn: "MedicineId",
                keyValue: 1,
                column: "IsDeleted",
                value: false);

            migrationBuilder.UpdateData(
                table: "Medicines",
                keyColumn: "MedicineId",
                keyValue: 2,
                column: "IsDeleted",
                value: false);

            migrationBuilder.UpdateData(
                table: "Medicines",
                keyColumn: "MedicineId",
                keyValue: 3,
                column: "IsDeleted",
                value: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Medicines");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "ServiceId",
                table: "Appointments");

            migrationBuilder.UpdateData(
                table: "Accounts",
                keyColumn: "AccountId",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$ZDkvtEDJffsYkDCDW59KqOVaL/i32ZSHSbj7q41GF7Ycj4M6PJj/a");
        }
    }
}
