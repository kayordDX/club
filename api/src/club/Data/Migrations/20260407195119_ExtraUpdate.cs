using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Online.Data.Migrations
{
    /// <inheritdoc />
    public partial class ExtraUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "facility_extra");

            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "extra",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "facility_id",
                table: "extra",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "is_available",
                table: "extra",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_online",
                table: "extra",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "price",
                table: "extra",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "ix_extra_facility_id",
                table: "extra",
                column: "facility_id");

            migrationBuilder.AddForeignKey(
                name: "fk_extra_facility_facility_id",
                table: "extra",
                column: "facility_id",
                principalTable: "facility",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_extra_facility_facility_id",
                table: "extra");

            migrationBuilder.DropIndex(
                name: "ix_extra_facility_id",
                table: "extra");

            migrationBuilder.DropColumn(
                name: "code",
                table: "extra");

            migrationBuilder.DropColumn(
                name: "facility_id",
                table: "extra");

            migrationBuilder.DropColumn(
                name: "is_available",
                table: "extra");

            migrationBuilder.DropColumn(
                name: "is_online",
                table: "extra");

            migrationBuilder.DropColumn(
                name: "price",
                table: "extra");

            migrationBuilder.CreateTable(
                name: "facility_extra",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    booking_status_id = table.Column<int>(type: "integer", nullable: false),
                    extra_id = table.Column<int>(type: "integer", nullable: false),
                    facility_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified)),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_available = table.Column<bool>(type: "boolean", nullable: false),
                    is_online = table.Column<bool>(type: "boolean", nullable: false),
                    last_modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_modified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    price = table.Column<decimal>(type: "numeric", nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_facility_extra", x => x.id);
                    table.ForeignKey(
                        name: "fk_facility_extra_booking_status_booking_status_id",
                        column: x => x.booking_status_id,
                        principalTable: "booking_status",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_facility_extra_extra_extra_id",
                        column: x => x.extra_id,
                        principalTable: "extra",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_facility_extra_facility_facility_id",
                        column: x => x.facility_id,
                        principalTable: "facility",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_facility_extra_users_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_facility_extra_booking_status_id",
                table: "facility_extra",
                column: "booking_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_facility_extra_extra_id",
                table: "facility_extra",
                column: "extra_id");

            migrationBuilder.CreateIndex(
                name: "ix_facility_extra_facility_id",
                table: "facility_extra",
                column: "facility_id");

            migrationBuilder.CreateIndex(
                name: "ix_facility_extra_user_id",
                table: "facility_extra",
                column: "user_id");
        }
    }
}
