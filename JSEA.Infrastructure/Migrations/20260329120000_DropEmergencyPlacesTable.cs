using JSEA_Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JSEA_Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260329120000_DropEmergencyPlacesTable")]
public class DropEmergencyPlacesTable : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS emergency_places;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Không tạo lại bảng: luồng khẩn cấp dùng Goong realtime.
    }
}
