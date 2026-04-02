using JSEA_Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JSEA_Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260401120000_AddJourneyMembersAndWaypointProgress")]
public class AddJourneyMembersAndWaypointProgress : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "journey_members",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                journey_id = table.Column<Guid>(type: "uuid", nullable: false),
                traveler_id = table.Column<Guid>(type: "uuid", nullable: true),
                guest_key = table.Column<Guid>(type: "uuid", nullable: true),
                display_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                is_registered_user = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                left_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("journey_members_pkey", x => x.id);
                table.ForeignKey(
                    name: "fk_journey_members_journey",
                    column: x => x.journey_id,
                    principalTable: "journeys",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_journey_members_traveler",
                    column: x => x.traveler_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "journey_waypoint_member_progress",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                journey_member_id = table.Column<Guid>(type: "uuid", nullable: false),
                journey_waypoint_id = table.Column<Guid>(type: "uuid", nullable: true),
                milestone_kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                arrived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                departed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                skipped = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("journey_waypoint_member_progress_pkey", x => x.id);
                table.CheckConstraint(
                    "ck_wp_progress_milestone",
                    "(milestone_kind = 'destination' AND journey_waypoint_id IS NULL) OR (milestone_kind = 'waypoint' AND journey_waypoint_id IS NOT NULL)");
                table.ForeignKey(
                    name: "fk_wp_progress_member",
                    column: x => x.journey_member_id,
                    principalTable: "journey_members",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_wp_progress_waypoint",
                    column: x => x.journey_waypoint_id,
                    principalTable: "journey_waypoints",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_journey_members_journey_id",
            table: "journey_members",
            column: "journey_id");

        migrationBuilder.CreateIndex(
            name: "ix_journey_members_traveler_id",
            table: "journey_members",
            column: "traveler_id");

        migrationBuilder.CreateIndex(
            name: "ix_wp_progress_member_id",
            table: "journey_waypoint_member_progress",
            column: "journey_member_id");

        migrationBuilder.Sql("""
            CREATE UNIQUE INDEX ux_journey_members_active_traveler
            ON journey_members (journey_id, traveler_id)
            WHERE traveler_id IS NOT NULL AND is_active = true;
            """);

        migrationBuilder.Sql("""
            CREATE UNIQUE INDEX ux_journey_members_active_guest
            ON journey_members (journey_id, guest_key)
            WHERE guest_key IS NOT NULL AND is_active = true;
            """);

        migrationBuilder.Sql("""
            CREATE UNIQUE INDEX ux_wp_progress_member_waypoint
            ON journey_waypoint_member_progress (journey_member_id, journey_waypoint_id)
            WHERE milestone_kind = 'waypoint' AND journey_waypoint_id IS NOT NULL;
            """);

        migrationBuilder.Sql("""
            CREATE UNIQUE INDEX ux_wp_progress_member_destination
            ON journey_waypoint_member_progress (journey_member_id)
            WHERE milestone_kind = 'destination';
            """);

        migrationBuilder.Sql("""
            INSERT INTO journey_members (id, journey_id, traveler_id, guest_key, display_name, is_registered_user, role, is_active, joined_at)
            SELECT gen_random_uuid(), j.id, j.traveler_id, NULL,
                   COALESCE((SELECT p.full_name FROM user_profiles p WHERE p.user_id = j.traveler_id LIMIT 1), 'Owner'),
                   true, 'owner', true, COALESCE(j.created_at, NOW())
            FROM journeys j
            WHERE NOT EXISTS (
              SELECT 1 FROM journey_members m
              WHERE m.journey_id = j.id AND m.traveler_id = j.traveler_id AND m.is_active AND m.role = 'owner');
            """);

        migrationBuilder.Sql("""
            INSERT INTO journey_waypoint_member_progress (id, journey_member_id, journey_waypoint_id, milestone_kind, arrived_at, departed_at, skipped)
            SELECT gen_random_uuid(), m.id, w.id, 'waypoint', w.actual_arrival_at, w.actual_departure_at, false
            FROM journey_waypoints w
            INNER JOIN journey_members m ON m.journey_id = w.journey_id AND m.traveler_id IS NOT NULL
            INNER JOIN journeys j ON j.id = w.journey_id AND j.traveler_id = m.traveler_id
            WHERE m.is_active AND m.role = 'owner' AND w.actual_departure_at IS NOT NULL
            AND NOT EXISTS (
              SELECT 1 FROM journey_waypoint_member_progress p
              WHERE p.journey_member_id = m.id AND p.journey_waypoint_id = w.id AND p.milestone_kind = 'waypoint');
            """);

        migrationBuilder.Sql("""
            INSERT INTO journey_waypoint_member_progress (id, journey_member_id, journey_waypoint_id, milestone_kind, arrived_at, departed_at, skipped)
            SELECT gen_random_uuid(), m.id, NULL, 'destination', j.completed_at, j.completed_at, false
            FROM journeys j
            INNER JOIN journey_members m ON m.journey_id = j.id AND m.traveler_id = j.traveler_id
            WHERE m.is_active AND m.role = 'owner'
              AND j.completed_at IS NOT NULL
              AND (j.status ILIKE 'completed' OR j.status ILIKE 'Completed')
            AND NOT EXISTS (
              SELECT 1 FROM journey_waypoint_member_progress p
              WHERE p.journey_member_id = m.id AND p.milestone_kind = 'destination');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP INDEX IF EXISTS ux_wp_progress_member_destination;");
        migrationBuilder.Sql("DROP INDEX IF EXISTS ux_wp_progress_member_waypoint;");
        migrationBuilder.Sql("DROP INDEX IF EXISTS ux_journey_members_active_guest;");
        migrationBuilder.Sql("DROP INDEX IF EXISTS ux_journey_members_active_traveler;");

        migrationBuilder.DropTable(name: "journey_waypoint_member_progress");
        migrationBuilder.DropTable(name: "journey_members");
    }
}
