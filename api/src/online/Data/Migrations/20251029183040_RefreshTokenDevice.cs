using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Online.Data.Migrations
{
    /// <inheritdoc />
    public partial class RefreshTokenDevice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "browser",
                table: "refresh_token",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "browser_version",
                table: "refresh_token",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "device",
                table: "refresh_token",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "platform",
                table: "refresh_token",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "processor",
                table: "refresh_token",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "browser",
                table: "refresh_token");

            migrationBuilder.DropColumn(
                name: "browser_version",
                table: "refresh_token");

            migrationBuilder.DropColumn(
                name: "device",
                table: "refresh_token");

            migrationBuilder.DropColumn(
                name: "platform",
                table: "refresh_token");

            migrationBuilder.DropColumn(
                name: "processor",
                table: "refresh_token");
        }
    }
}
