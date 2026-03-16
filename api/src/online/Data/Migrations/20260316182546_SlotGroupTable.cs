using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Online.Data.Migrations
{
    /// <inheritdoc />
    public partial class SlotGroupTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "group_id",
                table: "slot",
                newName: "slot_group_id");

            migrationBuilder.AddColumn<bool>(
                name: "can_pay_later",
                table: "slot",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "requires_login",
                table: "slot",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "slot_group",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    can_book_for_guests = table.Column<bool>(type: "boolean", nullable: false),
                    resource_id = table.Column<int>(type: "integer", nullable: true),
                    facility_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_slot_group", x => x.id);
                    table.ForeignKey(
                        name: "fk_slot_group_facility_facility_id",
                        column: x => x.facility_id,
                        principalTable: "facility",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_slot_group_resource_resource_id",
                        column: x => x.resource_id,
                        principalTable: "resource",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_slot_slot_group_id",
                table: "slot",
                column: "slot_group_id");

            migrationBuilder.CreateIndex(
                name: "ix_slot_group_facility_id",
                table: "slot_group",
                column: "facility_id");

            migrationBuilder.CreateIndex(
                name: "ix_slot_group_resource_id",
                table: "slot_group",
                column: "resource_id");

            migrationBuilder.AddForeignKey(
                name: "fk_slot_slot_group_slot_group_id",
                table: "slot",
                column: "slot_group_id",
                principalTable: "slot_group",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_slot_slot_group_slot_group_id",
                table: "slot");

            migrationBuilder.DropTable(
                name: "slot_group");

            migrationBuilder.DropIndex(
                name: "ix_slot_slot_group_id",
                table: "slot");

            migrationBuilder.DropColumn(
                name: "can_pay_later",
                table: "slot");

            migrationBuilder.DropColumn(
                name: "requires_login",
                table: "slot");

            migrationBuilder.RenameColumn(
                name: "slot_group_id",
                table: "slot",
                newName: "group_id");
        }
    }
}
