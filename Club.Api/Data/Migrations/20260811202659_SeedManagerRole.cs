using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Club.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedManagerRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO role (id, name, normalized_name, concurrency_stamp)
                VALUES (gen_random_uuid(), 'manager', 'MANAGER', NULL)
                ON CONFLICT (normalized_name) DO NOTHING;
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM role
                WHERE name = 'manager';
                """
            );
        }
    }
}
