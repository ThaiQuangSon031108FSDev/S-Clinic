using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SClinic.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CategoryName",
                table: "Medicines");

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Medicines",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    CategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.CategoryId);
                });

            migrationBuilder.UpdateData(
                table: "Accounts",
                keyColumn: "AccountId",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$ZDkvtEDJffsYkDCDW59KqOVaL/i32ZSHSbj7q41GF7Ycj4M6PJj/a");

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "CategoryId", "CategoryName", "Description" },
                values: new object[,]
                {
                    { 1, "Thuốc Kê Đơn", "Thuốc kê đơn bắc sĩ" },
                    { 2, "Dược Mỹ Phẩm", "Mỹ phẩm được liệu" },
                    { 3, "Thực Phẩm Chức Năng", "Vitamin và thực phẩm bổ sung" }
                });

            migrationBuilder.UpdateData(
                table: "Medicines",
                keyColumn: "MedicineId",
                keyValue: 1,
                column: "CategoryId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Medicines",
                keyColumn: "MedicineId",
                keyValue: 2,
                column: "CategoryId",
                value: 1);

            migrationBuilder.UpdateData(
                table: "Medicines",
                keyColumn: "MedicineId",
                keyValue: 3,
                column: "CategoryId",
                value: 2);

            // ── Data migration: assign CategoryId for medicines not in EF seed (4-10 from Data.sql) ──
            migrationBuilder.Sql(@"
                UPDATE Medicines SET CategoryId = 1  -- Thuốc Kê Đơn
                WHERE MedicineId IN (2, 3);          -- Kháng sinh + Kem Klenzit

                UPDATE Medicines SET CategoryId = 2  -- Dược Mỹ Phẩm
                WHERE MedicineId IN (4,5,6,7,8,9,10);

                -- Fallback: any remaining rows with CategoryId=0 → Dược Mỹ Phẩm
                UPDATE Medicines SET CategoryId = 2
                WHERE CategoryId = 0;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Medicines_CategoryId",
                table: "Medicines",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_CategoryName",
                table: "Categories",
                column: "CategoryName",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Medicines_Categories_CategoryId",
                table: "Medicines",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "CategoryId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Medicines_Categories_CategoryId",
                table: "Medicines");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Medicines_CategoryId",
                table: "Medicines");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Medicines");

            migrationBuilder.AddColumn<string>(
                name: "CategoryName",
                table: "Medicines",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Accounts",
                keyColumn: "AccountId",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$d09YF9vFZhQkYags5Hh91eJSJPt0xdpqSEQXOFo83fo11Fqfjf4Ze");

            migrationBuilder.UpdateData(
                table: "Medicines",
                keyColumn: "MedicineId",
                keyValue: 1,
                column: "CategoryName",
                value: "Topical Retinoid");

            migrationBuilder.UpdateData(
                table: "Medicines",
                keyColumn: "MedicineId",
                keyValue: 2,
                column: "CategoryName",
                value: "Topical Antibiotic");

            migrationBuilder.UpdateData(
                table: "Medicines",
                keyColumn: "MedicineId",
                keyValue: 3,
                column: "CategoryName",
                value: "Skincare");
        }
    }
}
