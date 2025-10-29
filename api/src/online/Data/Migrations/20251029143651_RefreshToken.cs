using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Online.Data.Migrations
{
    /// <inheritdoc />
    public partial class RefreshToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_resouce_facility_facility_id",
                table: "resouce");

            migrationBuilder.DropPrimaryKey(
                name: "pk_resouce",
                table: "resouce");

            migrationBuilder.DropColumn(
                name: "refresh_token",
                table: "user");

            migrationBuilder.DropColumn(
                name: "refresh_token_expires_at_utc",
                table: "user");

            migrationBuilder.RenameTable(
                name: "resouce",
                newName: "resource");

            migrationBuilder.RenameIndex(
                name: "ix_resouce_facility_id",
                table: "resource",
                newName: "ix_resource_facility_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_resource",
                table: "resource",
                column: "id");

            migrationBuilder.CreateTable(
                name: "refresh_token",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    token = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_token", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_token_users_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_refresh_token_token",
                table: "refresh_token",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_token_user_id",
                table: "refresh_token",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_resource_facility_facility_id",
                table: "resource",
                column: "facility_id",
                principalTable: "facility",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_resource_facility_facility_id",
                table: "resource");

            migrationBuilder.DropTable(
                name: "refresh_token");

            migrationBuilder.DropPrimaryKey(
                name: "pk_resource",
                table: "resource");

            migrationBuilder.RenameTable(
                name: "resource",
                newName: "resouce");

            migrationBuilder.RenameIndex(
                name: "ix_resource_facility_id",
                table: "resouce",
                newName: "ix_resouce_facility_id");

            migrationBuilder.AddColumn<string>(
                name: "refresh_token",
                table: "user",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "refresh_token_expires_at_utc",
                table: "user",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "pk_resouce",
                table: "resouce",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_resouce_facility_facility_id",
                table: "resouce",
                column: "facility_id",
                principalTable: "facility",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
