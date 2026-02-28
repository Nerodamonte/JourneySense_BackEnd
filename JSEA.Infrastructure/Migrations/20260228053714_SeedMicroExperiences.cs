using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JSEA_Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedMicroExperiences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var now = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            migrationBuilder.Sql($@"
                INSERT INTO micro_experiences (id, category_id, name, slug, address, city, country, status, created_at, updated_at)
                VALUES
                    ('a1000000-0000-0000-0000-000000000001'::uuid, '00000000-0000-0000-0000-000000000001'::uuid, 'Cà phê View Sài Gòn', 'ca-phe-view-sai-gon', '123 Nguyễn Huệ, Quận 1', 'Hồ Chí Minh', 'Việt Nam', 'Verified', '{now}'::timestamptz, '{now}'::timestamptz),
                    ('a1000000-0000-0000-0000-000000000002'::uuid, '00000000-0000-0000-0000-000000000001'::uuid, 'Bưu điện Trung tâm Sài Gòn', 'buu-dien-trung-tam-sai-gon', '2 Công xã Paris, Bến Nghé, Quận 1', 'Hồ Chí Minh', 'Việt Nam', 'Verified', '{now}'::timestamptz, '{now}'::timestamptz),
                    ('a1000000-0000-0000-0000-000000000003'::uuid, '00000000-0000-0000-0000-000000000001'::uuid, 'Nhà thờ Đức Bà Sài Gòn', 'nha-tho-duc-ba-sai-gon', '1 Công xã Paris, Bến Nghé, Quận 1', 'Hồ Chí Minh', 'Việt Nam', 'Verified', '{now}'::timestamptz, '{now}'::timestamptz),
                    ('a1000000-0000-0000-0000-000000000004'::uuid, '00000000-0000-0000-0000-000000000001'::uuid, 'Phở 24 Lý Tự Trọng', 'pho-24-ly-tu-trong', '5 Lý Tự Trọng, Quận 1', 'Hồ Chí Minh', 'Việt Nam', 'ActiveUnverified', '{now}'::timestamptz, '{now}'::timestamptz),
                    ('a1000000-0000-0000-0000-000000000005'::uuid, '00000000-0000-0000-0000-000000000001'::uuid, 'Chợ Bến Thành', 'cho-ben-thanh', 'Lê Lợi, Phường Bến Thành, Quận 1', 'Hồ Chí Minh', 'Việt Nam', 'Verified', '{now}'::timestamptz, '{now}'::timestamptz)
                ON CONFLICT (id) DO NOTHING;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM micro_experiences WHERE id IN (
                    'a1000000-0000-0000-0000-000000000001'::uuid,
                    'a1000000-0000-0000-0000-000000000002'::uuid,
                    'a1000000-0000-0000-0000-000000000003'::uuid,
                    'a1000000-0000-0000-0000-000000000004'::uuid,
                    'a1000000-0000-0000-0000-000000000005'::uuid
                );
            ");
        }
    }
}
