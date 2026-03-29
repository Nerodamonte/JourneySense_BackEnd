using JSEA_Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JSEA_Infrastructure.Migrations;

/// <summary>
/// Cột experiences.emergency_kind (hoặc micro_experiences nếu DB cũ). Khớp model Experience.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260327120000_AddExperienceEmergencyKind")]
public class AddExperienceEmergencyKind : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DO $$
            BEGIN
                IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'experiences')
                   AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'experiences' AND column_name = 'emergency_kind') THEN
                    ALTER TABLE experiences ADD COLUMN emergency_kind character varying(20) NULL;
                END IF;

                IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'micro_experiences')
                   AND NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public' AND table_name = 'micro_experiences' AND column_name = 'emergency_kind') THEN
                    ALTER TABLE micro_experiences ADD COLUMN emergency_kind character varying(20) NULL;
                END IF;
            END $$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE IF EXISTS experiences DROP COLUMN IF EXISTS emergency_kind;
            ALTER TABLE IF EXISTS micro_experiences DROP COLUMN IF EXISTS emergency_kind;
            """);
    }
}
