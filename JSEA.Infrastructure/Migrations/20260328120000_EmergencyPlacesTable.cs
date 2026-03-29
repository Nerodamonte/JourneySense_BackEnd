using JSEA_Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JSEA_Infrastructure.Migrations;

/// <summary>Bảng emergency_places; gỡ cột emergency_kind trên experiences/micro_experiences nếu có.</summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260328120000_EmergencyPlacesTable")]
public class EmergencyPlacesTable : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS emergency_places (
                id              uuid DEFAULT gen_random_uuid() PRIMARY KEY,
                type            varchar(20) NOT NULL,
                name            varchar(255) NOT NULL,
                address         text,
                phone           varchar(50),
                location        geography(Point,4326) NOT NULL,
                opening_hours   jsonb,
                is_24h          boolean DEFAULT false NOT NULL,
                google_rating   numeric(2,1),
                accessible_by   character varying(20)[] NULL,
                created_at      timestamptz DEFAULT now() NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_emergency_places_location ON emergency_places USING GIST (location);
            CREATE INDEX IF NOT EXISTS idx_emergency_places_type ON emergency_places (type);
            """);

        migrationBuilder.Sql("""
            ALTER TABLE IF EXISTS experiences DROP COLUMN IF EXISTS emergency_kind;
            ALTER TABLE IF EXISTS micro_experiences DROP COLUMN IF EXISTS emergency_kind;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS emergency_places;");
        // Không tự thêm lại emergency_kind trên experiences để tránh mơ hồ schema.
    }
}
