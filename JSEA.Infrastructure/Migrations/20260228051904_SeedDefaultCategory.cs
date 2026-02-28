using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JSEA_Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedDefaultCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO categories (id, name, slug, description, display_order, is_active)
                VALUES ('00000000-0000-0000-0000-000000000001'::uuid, 'Ăn uống', 'an-uong', 'Danh mục mẫu cho trải nghiệm ăn uống', 1, true)
                ON CONFLICT (id) DO NOTHING;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM categories WHERE id = '00000000-0000-0000-0000-000000000001'::uuid;
            ");
        }
    }
}
