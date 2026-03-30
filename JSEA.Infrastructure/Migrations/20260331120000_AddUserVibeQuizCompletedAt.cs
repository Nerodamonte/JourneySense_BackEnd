using JSEA_Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JSEA_Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260331120000_AddUserVibeQuizCompletedAt")]
public class AddUserVibeQuizCompletedAt : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "vibe_quiz_completed_at",
            table: "users",
            type: "timestamp with time zone",
            nullable: true);

        // User đã tồn tại trước khi có quiz onboarding — coi như đã xong, không bắt quiz.
        migrationBuilder.Sql("""
            UPDATE users SET vibe_quiz_completed_at = NOW() WHERE vibe_quiz_completed_at IS NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "vibe_quiz_completed_at",
            table: "users");
    }
}
