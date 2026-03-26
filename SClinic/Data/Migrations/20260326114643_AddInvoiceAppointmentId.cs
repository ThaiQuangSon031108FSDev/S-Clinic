using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SClinic.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceAppointmentId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AppointmentId",
                table: "Invoices",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Accounts",
                keyColumn: "AccountId",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$2WybcZPse5n/163pLuDFd.CNwrexUR/tujVEeGaXKhqSbdARberqu");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_AppointmentId",
                table: "Invoices",
                column: "AppointmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Appointments_AppointmentId",
                table: "Invoices",
                column: "AppointmentId",
                principalTable: "Appointments",
                principalColumn: "AppointmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Appointments_AppointmentId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_AppointmentId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "AppointmentId",
                table: "Invoices");

            migrationBuilder.UpdateData(
                table: "Accounts",
                keyColumn: "AccountId",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$c7ORiYZN6t1ABKyTgIUk/up6dE3L2tzWgdDd6BrFp6X6rV//TkMcW");
        }
    }
}
