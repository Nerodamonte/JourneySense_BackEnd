using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JSEA_Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserVibeTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "preferred_travel_styles",
                table: "user_profiles");

            migrationBuilder.CreateTable(
                name: "travel_styles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    descripton = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_travel_styles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_vibes",
                columns: table => new
                {
                    user_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    travel_style_id = table.Column<Guid>(type: "uuid", nullable: false),
                    selected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_vibes_pkey", x => new { x.user_profile_id, x.travel_style_id });
                    table.ForeignKey(
                        name: "FK_user_vibes_travel_styles_travel_style_id",
                        column: x => x.travel_style_id,
                        principalTable: "travel_styles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_vibes_user_profiles_user_profile_id",
                        column: x => x.user_profile_id,
                        principalTable: "user_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_vibes_travel_style_id",
                table: "user_vibes",
                column: "travel_style_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_vibes");

            migrationBuilder.DropTable(
                name: "travel_styles");

            migrationBuilder.AddColumn<List<string>>(
                name: "preferred_travel_styles",
                table: "user_profiles",
                type: "text[]",
                nullable: true);
        }
    }
}
