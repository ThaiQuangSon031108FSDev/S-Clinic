using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SClinic.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceTypeData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Set ServiceType: 1=Consultation, 2=Treatment
            // Fix price of "Tư vấn lập phác đồ" to 500,000đ
            migrationBuilder.Sql(@"
                UPDATE Services SET ServiceType = 1 WHERE ServiceId = 1; -- Khám & Soi da 3D
                UPDATE Services SET ServiceType = 2 WHERE ServiceId = 2; -- Lấy nhân mụn
                UPDATE Services SET ServiceType = 2 WHERE ServiceId = 3; -- Peel da
                UPDATE Services SET ServiceType = 2 WHERE ServiceId = 4; -- Điện di Ion
                UPDATE Services SET ServiceType = 2 WHERE ServiceId = 5; -- Chiếu ánh sáng
                UPDATE Services SET ServiceType = 1, Price = 500000 WHERE ServiceId = 6; -- Tư vấn lập phác đồ
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert prices and service types to default (0)
            migrationBuilder.Sql(@"
                UPDATE Services SET ServiceType = 0 WHERE ServiceId IN (1, 2, 3, 4, 5, 6);
                UPDATE Services SET Price = 0 WHERE ServiceId = 6;
            ");
        }
    }
}
