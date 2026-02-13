using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace JSEA_Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class intial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:fuzzystrmatch", ",,")
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .Annotation("Npgsql:PostgresExtension:postgis", ",,")
                .Annotation("Npgsql:PostgresExtension:uuid-ossp", ",,");

            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    icon_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: true, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("categories_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "email_otps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    otp_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    expired_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_used = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_verified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("email_otps_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "packages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    sale_price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    benefit = table.Column<string>(type: "jsonb", nullable: true),
                    km = table.Column<int>(type: "integer", nullable: true),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_popular = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: true, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("packages_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "system_configs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    config_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    config_value = table.Column<string>(type: "jsonb", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("system_configs_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    email_verified = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    phone_verified = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("users_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    old_values = table.Column<string>(type: "jsonb", nullable: true),
                    new_values = table.Column<string>(type: "jsonb", nullable: true),
                    ip_address = table.Column<IPAddress>(type: "inet", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("audit_logs_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_audit_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "journeys",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    traveler_id = table.Column<Guid>(type: "uuid", nullable: true),
                    origin_location = table.Column<Point>(type: "geography(Point,4326)", nullable: true),
                    origin_address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    destination_location = table.Column<Point>(type: "geography(Point,4326)", nullable: true),
                    destination_address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    route_path = table.Column<LineString>(type: "geography(LineString,4326)", nullable: true),
                    actual_route_path = table.Column<LineString>(type: "geography(LineString,4326)", nullable: true),
                    total_distance_meters = table.Column<int>(type: "integer", nullable: true),
                    actual_distance_meters = table.Column<int>(type: "integer", nullable: true),
                    estimated_duration_minutes = table.Column<int>(type: "integer", nullable: true),
                    current_mood = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    vehicle_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    max_detour_distance_meters = table.Column<int>(type: "integer", nullable: true),
                    preferred_stop_duration_minutes = table.Column<int>(type: "integer", nullable: true),
                    time_budget_minutes = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("journeys_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_journey_user",
                        column: x => x.traveler_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "micro_experiences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    uploaded_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    slug = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    location = table.Column<Point>(type: "geography(Point,4326)", nullable: true),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, defaultValueSql: "'Vietnam'::character varying"),
                    suitable_moods = table.Column<List<string>>(type: "character varying(50)[]", nullable: true),
                    preferred_times = table.Column<List<string>>(type: "character varying(50)[]", nullable: true),
                    weather_suitability = table.Column<List<string>>(type: "character varying(50)[]", nullable: true),
                    seasonality = table.Column<List<string>>(type: "character varying(50)[]", nullable: true),
                    tags = table.Column<List<string>>(type: "text[]", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("micro_experiences_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_exp_cat",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_exp_user",
                        column: x => x.uploaded_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notification_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    message = table.Column<string>(type: "text", nullable: true),
                    related_experience_id = table.Column<Guid>(type: "uuid", nullable: true),
                    related_journey_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_read = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("notifications_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_notif_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()"),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("refresh_tokens_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_token_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    amount = table.Column<long>(type: "bigint", nullable: true),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    item_snapshot = table.Column<string>(type: "jsonb", nullable: true),
                    payment_method = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("transactions_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_trans_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "user_packages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    package_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: true, defaultValue: true),
                    activated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()"),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_packages_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_upkg_pkg",
                        column: x => x.package_id,
                        principalTable: "packages",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_upkg_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    avatar_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    bio = table.Column<string>(type: "text", nullable: true),
                    preferred_travel_styles = table.Column<List<string>>(type: "text[]", nullable: true),
                    interests = table.Column<List<string>>(type: "text[]", nullable: true),
                    accessibility_needs = table.Column<string>(type: "text", nullable: true),
                    department = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    permissions = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_profiles_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_profile",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "route_segments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    journey_id = table.Column<Guid>(type: "uuid", nullable: true),
                    segment_path = table.Column<LineString>(type: "geography(LineString,4326)", nullable: true),
                    segment_order = table.Column<int>(type: "integer", nullable: true),
                    distance_meters = table.Column<int>(type: "integer", nullable: true),
                    estimated_duration_minutes = table.Column<int>(type: "integer", nullable: true),
                    is_scenic = table.Column<bool>(type: "boolean", nullable: true),
                    is_busy = table.Column<bool>(type: "boolean", nullable: true),
                    is_cultural_area = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("route_segments_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_segment_journey",
                        column: x => x.journey_id,
                        principalTable: "journeys",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    experience_id = table.Column<Guid>(type: "uuid", nullable: true),
                    uploaded_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    event_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    start_datetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    end_datetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    recurrence_pattern = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    recurrence_rule = table.Column<string>(type: "text", nullable: true),
                    score_boost_factor = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("events_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_exp",
                        column: x => x.experience_id,
                        principalTable: "micro_experiences",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "experience_details",
                columns: table => new
                {
                    experience_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    opening_hours = table.Column<string>(type: "jsonb", nullable: true),
                    price_range = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    crowd_level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    safety_notes = table.Column<string>(type: "text", nullable: true),
                    accessibility_info = table.Column<string>(type: "text", nullable: true),
                    moderation_notes = table.Column<string>(type: "text", nullable: true),
                    rejection_reason = table.Column<string>(type: "text", nullable: true),
                    verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    verified_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    featured_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    featured_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("experience_details_pkey", x => x.experience_id);
                    table.ForeignKey(
                        name: "fk_detail_exp",
                        column: x => x.experience_id,
                        principalTable: "micro_experiences",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_detail_featured",
                        column: x => x.featured_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_detail_verifier",
                        column: x => x.verified_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "experience_metrics",
                columns: table => new
                {
                    experience_id = table.Column<Guid>(type: "uuid", nullable: false),
                    featured_score = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: true),
                    total_visits = table.Column<int>(type: "integer", nullable: true, defaultValue: 0),
                    total_ratings = table.Column<int>(type: "integer", nullable: true, defaultValue: 0),
                    avg_rating = table.Column<decimal>(type: "numeric(2,1)", precision: 2, scale: 1, nullable: true, defaultValue: 0m),
                    acceptance_rate = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: true),
                    last_visit_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("experience_metrics_pkey", x => x.experience_id);
                    table.ForeignKey(
                        name: "fk_metric_exp",
                        column: x => x.experience_id,
                        principalTable: "micro_experiences",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "experience_photos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    experience_id = table.Column<Guid>(type: "uuid", nullable: true),
                    photo_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    thumbnail_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    caption = table.Column<string>(type: "text", nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: true),
                    uploaded_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_cover = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("experience_photos_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_photo_exp",
                        column: x => x.experience_id,
                        principalTable: "micro_experiences",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "journey_waypoints",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    journey_id = table.Column<Guid>(type: "uuid", nullable: true),
                    experience_id = table.Column<Guid>(type: "uuid", nullable: true),
                    location = table.Column<Point>(type: "geography(Point,4326)", nullable: true),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    stop_order = table.Column<int>(type: "integer", nullable: true),
                    estimated_arrival_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    actual_arrival_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("journey_waypoints_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_waypoint_exp",
                        column: x => x.experience_id,
                        principalTable: "micro_experiences",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_waypoint_journey",
                        column: x => x.journey_id,
                        principalTable: "journeys",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_favorites",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    experience_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_favorites_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_fav_exp",
                        column: x => x.experience_id,
                        principalTable: "micro_experiences",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_fav_user",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "visits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    traveler_id = table.Column<Guid>(type: "uuid", nullable: true),
                    experience_id = table.Column<Guid>(type: "uuid", nullable: true),
                    journey_id = table.Column<Guid>(type: "uuid", nullable: true),
                    visited_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()"),
                    actual_duration_minutes = table.Column<int>(type: "integer", nullable: true),
                    photo_urls = table.Column<List<string>>(type: "text[]", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("visits_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_visit_exp",
                        column: x => x.experience_id,
                        principalTable: "micro_experiences",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_visit_journey",
                        column: x => x.journey_id,
                        principalTable: "journeys",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_visit_user",
                        column: x => x.traveler_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "journey_suggestions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    journey_id = table.Column<Guid>(type: "uuid", nullable: true),
                    experience_id = table.Column<Guid>(type: "uuid", nullable: true),
                    segment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    detour_distance_meters = table.Column<int>(type: "integer", nullable: true),
                    estimated_stop_minutes = table.Column<int>(type: "integer", nullable: true),
                    relevance_score = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: true),
                    suggested_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("journey_suggestions_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_sugg_exp",
                        column: x => x.experience_id,
                        principalTable: "micro_experiences",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_sugg_journey",
                        column: x => x.journey_id,
                        principalTable: "journeys",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_sugg_segment",
                        column: x => x.segment_id,
                        principalTable: "route_segments",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "event_occurrences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    occurrence_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    occurrence_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("event_occurrences_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_occ_event",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "feedbacks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    visit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    traveler_id = table.Column<Guid>(type: "uuid", nullable: true),
                    experience_id = table.Column<Guid>(type: "uuid", nullable: true),
                    feedback_text = table.Column<string>(type: "text", nullable: true),
                    is_flagged = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    flagged_reason = table.Column<string>(type: "text", nullable: true),
                    is_approved = table.Column<bool>(type: "boolean", nullable: true, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("feedbacks_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_feedback_visit",
                        column: x => x.visit_id,
                        principalTable: "visits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ratings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    visit_id = table.Column<Guid>(type: "uuid", nullable: true),
                    traveler_id = table.Column<Guid>(type: "uuid", nullable: true),
                    experience_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rating = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("ratings_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_rating_visit",
                        column: x => x.visit_id,
                        principalTable: "visits",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "suggestion_interactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    suggestion_id = table.Column<Guid>(type: "uuid", nullable: true),
                    interaction_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    interacted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("suggestion_interactions_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fk_interact_sugg",
                        column: x => x.suggestion_id,
                        principalTable: "journey_suggestions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_user_id",
                table: "audit_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "categories_name_key",
                table: "categories",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "categories_slug_key",
                table: "categories",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_event_occurrences_event_id",
                table: "event_occurrences",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "IX_events_experience_id",
                table: "events",
                column: "experience_id");

            migrationBuilder.CreateIndex(
                name: "IX_experience_details_featured_by_user_id",
                table: "experience_details",
                column: "featured_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_experience_details_verified_by_user_id",
                table: "experience_details",
                column: "verified_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_experience_photos_experience_id",
                table: "experience_photos",
                column: "experience_id");

            migrationBuilder.CreateIndex(
                name: "feedbacks_visit_id_key",
                table: "feedbacks",
                column: "visit_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_journey_suggestions_experience_id",
                table: "journey_suggestions",
                column: "experience_id");

            migrationBuilder.CreateIndex(
                name: "IX_journey_suggestions_journey_id",
                table: "journey_suggestions",
                column: "journey_id");

            migrationBuilder.CreateIndex(
                name: "IX_journey_suggestions_segment_id",
                table: "journey_suggestions",
                column: "segment_id");

            migrationBuilder.CreateIndex(
                name: "IX_journey_waypoints_experience_id",
                table: "journey_waypoints",
                column: "experience_id");

            migrationBuilder.CreateIndex(
                name: "IX_journey_waypoints_journey_id",
                table: "journey_waypoints",
                column: "journey_id");

            migrationBuilder.CreateIndex(
                name: "IX_journeys_traveler_id",
                table: "journeys",
                column: "traveler_id");

            migrationBuilder.CreateIndex(
                name: "IX_micro_experiences_category_id",
                table: "micro_experiences",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_micro_experiences_uploaded_by_user_id",
                table: "micro_experiences",
                column: "uploaded_by_user_id");

            migrationBuilder.CreateIndex(
                name: "micro_experiences_slug_key",
                table: "micro_experiences",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notifications_user_id",
                table: "notifications",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ratings_visit_id_key",
                table: "ratings",
                column: "visit_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_user_id",
                table: "refresh_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "refresh_tokens_token_hash_key",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_route_segments_journey_id",
                table: "route_segments",
                column: "journey_id");

            migrationBuilder.CreateIndex(
                name: "IX_suggestion_interactions_suggestion_id",
                table: "suggestion_interactions",
                column: "suggestion_id");

            migrationBuilder.CreateIndex(
                name: "system_configs_config_key_key",
                table: "system_configs",
                column: "config_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transactions_user_id",
                table: "transactions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_favorites_experience_id",
                table: "user_favorites",
                column: "experience_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_favorites_user_id",
                table: "user_favorites",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_packages_package_id",
                table: "user_packages",
                column: "package_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_packages_user_id",
                table: "user_packages",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "user_profiles_user_id_key",
                table: "user_profiles",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "users_email_key",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "users_phone_key",
                table: "users",
                column: "phone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_visits_experience_id",
                table: "visits",
                column: "experience_id");

            migrationBuilder.CreateIndex(
                name: "IX_visits_journey_id",
                table: "visits",
                column: "journey_id");

            migrationBuilder.CreateIndex(
                name: "IX_visits_traveler_id",
                table: "visits",
                column: "traveler_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "email_otps");

            migrationBuilder.DropTable(
                name: "event_occurrences");

            migrationBuilder.DropTable(
                name: "experience_details");

            migrationBuilder.DropTable(
                name: "experience_metrics");

            migrationBuilder.DropTable(
                name: "experience_photos");

            migrationBuilder.DropTable(
                name: "feedbacks");

            migrationBuilder.DropTable(
                name: "journey_waypoints");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "ratings");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "suggestion_interactions");

            migrationBuilder.DropTable(
                name: "system_configs");

            migrationBuilder.DropTable(
                name: "transactions");

            migrationBuilder.DropTable(
                name: "user_favorites");

            migrationBuilder.DropTable(
                name: "user_packages");

            migrationBuilder.DropTable(
                name: "user_profiles");

            migrationBuilder.DropTable(
                name: "events");

            migrationBuilder.DropTable(
                name: "visits");

            migrationBuilder.DropTable(
                name: "journey_suggestions");

            migrationBuilder.DropTable(
                name: "packages");

            migrationBuilder.DropTable(
                name: "micro_experiences");

            migrationBuilder.DropTable(
                name: "route_segments");

            migrationBuilder.DropTable(
                name: "categories");

            migrationBuilder.DropTable(
                name: "journeys");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
