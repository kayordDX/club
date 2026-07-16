using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Club.Data.TickerQMigrations
{
    /// <inheritdoc />
    public partial class TickerQAnother : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_system_paused",
                schema: "ticker",
                table: "CronTickers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_system_paused",
                schema: "ticker",
                table: "CronTickers");
        }
    }
}
