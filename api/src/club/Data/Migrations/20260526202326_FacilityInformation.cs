using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Online.Data.Migrations
{
    /// <inheritdoc />
    public partial class FacilityInformation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "contact",
                table: "outlet",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "outlet",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "outlet",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "operating_hours",
                table: "outlet",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tags",
                table: "outlet",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "contact",
                table: "facility",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "facility",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "operating_hours",
                table: "facility",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rules",
                table: "facility",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "contact",
                table: "outlet");

            migrationBuilder.DropColumn(
                name: "description",
                table: "outlet");

            migrationBuilder.DropColumn(
                name: "email",
                table: "outlet");

            migrationBuilder.DropColumn(
                name: "operating_hours",
                table: "outlet");

            migrationBuilder.DropColumn(
                name: "tags",
                table: "outlet");

            migrationBuilder.DropColumn(
                name: "contact",
                table: "facility");

            migrationBuilder.DropColumn(
                name: "email",
                table: "facility");

            migrationBuilder.DropColumn(
                name: "operating_hours",
                table: "facility");

            migrationBuilder.DropColumn(
                name: "rules",
                table: "facility");
        }
    }
}
