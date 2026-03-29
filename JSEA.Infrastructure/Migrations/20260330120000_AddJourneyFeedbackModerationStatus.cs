using JSEA_Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JSEA_Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260330120000_AddJourneyFeedbackModerationStatus")]
public class AddJourneyFeedbackModerationStatus : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "journey_feedback_moderation_status",
            table: "journeys",
            type: "character varying(20)",
            maxLength: 20,
            nullable: false,
            defaultValue: "approved");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "journey_feedback_moderation_status",
            table: "journeys");
    }
}
