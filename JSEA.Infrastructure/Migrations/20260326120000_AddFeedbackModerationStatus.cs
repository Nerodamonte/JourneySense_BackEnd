using JSEA_Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JSEA_Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260326120000_AddFeedbackModerationStatus")]
public class AddFeedbackModerationStatus : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "moderation_status",
            table: "feedbacks",
            type: "character varying(20)",
            maxLength: 20,
            nullable: false,
            defaultValue: "approved");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "moderation_status",
            table: "feedbacks");
    }
}
