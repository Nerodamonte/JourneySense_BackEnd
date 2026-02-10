using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JSEA_Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixRelationshipsAndNaming : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "feedbacks_visit_id_fkey",
                table: "feedbacks");

            migrationBuilder.DropForeignKey(
                name: "micro_experiences_id_fkey",
                table: "micro_experiences");

            migrationBuilder.DropForeignKey(
                name: "micro_experiences_id_fkey1",
                table: "micro_experiences");

            migrationBuilder.DropForeignKey(
                name: "ratings_visit_id_fkey",
                table: "ratings");

            migrationBuilder.DropForeignKey(
                name: "refresh_tokens_user_id_fkey",
                table: "refresh_tokens");

            migrationBuilder.DropForeignKey(
                name: "users_id_fkey",
                table: "users");

            migrationBuilder.DropForeignKey(
                name: "FK_visits_feedbacks_id",
                table: "visits");

            migrationBuilder.DropForeignKey(
                name: "FK_visits_ratings_id",
                table: "visits");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_user_profiles_user_id",
                table: "user_profiles");

            migrationBuilder.DropIndex(
                name: "transactions_order_code_key",
                table: "transactions");

            migrationBuilder.AlterTable(
                name: "users",
                oldComment: "Quản lý authentication và phân quyền");

            migrationBuilder.AlterTable(
                name: "user_profiles",
                oldComment: "Metadata chi tiết cho từng loại user");

            migrationBuilder.AlterColumn<string>(
                name: "item_snapshot",
                table: "transactions",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true,
                oldComment: "Lưu thông tin gói tại thời điểm mua");

            migrationBuilder.AlterColumn<int>(
                name: "rating",
                table: "ratings",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true,
                oldComment: "1-5 stars");

            migrationBuilder.AlterColumn<int>(
                name: "km",
                table: "packages",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true,
                oldComment: "Giới hạn km hoặc thuộc tính liên quan");

            migrationBuilder.AlterColumn<string>(
                name: "benefit",
                table: "packages",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true,
                oldComment: "Lưu các quyền lợi của gói");

            migrationBuilder.AlterColumn<string>(
                name: "country",
                table: "micro_experiences",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                defaultValue: "Vietnam",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true,
                oldDefaultValueSql: "'Vietnam'::character varying");

            migrationBuilder.AlterColumn<int>(
                name: "stop_order",
                table: "journey_waypoints",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true,
                oldComment: "Thứ tự điểm dừng 1, 2, 3...");

            migrationBuilder.AlterColumn<Guid>(
                name: "experience_id",
                table: "journey_waypoints",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "Có thể là 1 micro-experience hoặc điểm tùy chỉnh");

            migrationBuilder.AlterColumn<decimal>(
                name: "avg_rating",
                table: "experience_metrics",
                type: "numeric(2,1)",
                precision: 2,
                scale: 1,
                nullable: true,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(2,1)",
                oldPrecision: 2,
                oldScale: 1,
                oldNullable: true,
                oldDefaultValueSql: "0");

            migrationBuilder.AddForeignKey(
                name: "experience_details_experience_id_fkey",
                table: "experience_details",
                column: "experience_id",
                principalTable: "micro_experiences",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "experience_metrics_experience_id_fkey",
                table: "experience_metrics",
                column: "experience_id",
                principalTable: "micro_experiences",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "feedbacks_visit_id_fkey",
                table: "feedbacks",
                column: "visit_id",
                principalTable: "visits",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "ratings_visit_id_fkey",
                table: "ratings",
                column: "visit_id",
                principalTable: "visits",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "refresh_tokens_user_id_fkey",
                table: "refresh_tokens",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "user_profiles_user_id_fkey",
                table: "user_profiles",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "experience_details_experience_id_fkey",
                table: "experience_details");

            migrationBuilder.DropForeignKey(
                name: "experience_metrics_experience_id_fkey",
                table: "experience_metrics");

            migrationBuilder.DropForeignKey(
                name: "feedbacks_visit_id_fkey",
                table: "feedbacks");

            migrationBuilder.DropForeignKey(
                name: "ratings_visit_id_fkey",
                table: "ratings");

            migrationBuilder.DropForeignKey(
                name: "refresh_tokens_user_id_fkey",
                table: "refresh_tokens");

            migrationBuilder.DropForeignKey(
                name: "user_profiles_user_id_fkey",
                table: "user_profiles");

            migrationBuilder.AlterTable(
                name: "users",
                comment: "Quản lý authentication và phân quyền");

            migrationBuilder.AlterTable(
                name: "user_profiles",
                comment: "Metadata chi tiết cho từng loại user");

            migrationBuilder.AlterColumn<string>(
                name: "item_snapshot",
                table: "transactions",
                type: "jsonb",
                nullable: true,
                comment: "Lưu thông tin gói tại thời điểm mua",
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "rating",
                table: "ratings",
                type: "integer",
                nullable: true,
                comment: "1-5 stars",
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "km",
                table: "packages",
                type: "integer",
                nullable: true,
                comment: "Giới hạn km hoặc thuộc tính liên quan",
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "benefit",
                table: "packages",
                type: "jsonb",
                nullable: true,
                comment: "Lưu các quyền lợi của gói",
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "country",
                table: "micro_experiences",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                defaultValueSql: "'Vietnam'::character varying",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true,
                oldDefaultValue: "Vietnam");

            migrationBuilder.AlterColumn<int>(
                name: "stop_order",
                table: "journey_waypoints",
                type: "integer",
                nullable: true,
                comment: "Thứ tự điểm dừng 1, 2, 3...",
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "experience_id",
                table: "journey_waypoints",
                type: "uuid",
                nullable: true,
                comment: "Có thể là 1 micro-experience hoặc điểm tùy chỉnh",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "avg_rating",
                table: "experience_metrics",
                type: "numeric(2,1)",
                precision: 2,
                scale: 1,
                nullable: true,
                defaultValueSql: "0",
                oldClrType: typeof(decimal),
                oldType: "numeric(2,1)",
                oldPrecision: 2,
                oldScale: 1,
                oldNullable: true,
                oldDefaultValue: 0m);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_user_profiles_user_id",
                table: "user_profiles",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "transactions_order_code_key",
                table: "transactions",
                column: "order_code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "feedbacks_visit_id_fkey",
                table: "feedbacks",
                column: "visit_id",
                principalTable: "visits",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "micro_experiences_id_fkey",
                table: "micro_experiences",
                column: "id",
                principalTable: "experience_metrics",
                principalColumn: "experience_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "micro_experiences_id_fkey1",
                table: "micro_experiences",
                column: "id",
                principalTable: "experience_details",
                principalColumn: "experience_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "ratings_visit_id_fkey",
                table: "ratings",
                column: "visit_id",
                principalTable: "visits",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "refresh_tokens_user_id_fkey",
                table: "refresh_tokens",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "users_id_fkey",
                table: "users",
                column: "id",
                principalTable: "user_profiles",
                principalColumn: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_visits_feedbacks_id",
                table: "visits",
                column: "id",
                principalTable: "feedbacks",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_visits_ratings_id",
                table: "visits",
                column: "id",
                principalTable: "ratings",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
