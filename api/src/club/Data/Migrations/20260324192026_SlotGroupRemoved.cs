using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Online.Data.Migrations
{
    /// <inheritdoc />
    public partial class SlotGroupRemoved : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "can_pay_later",
                table: "slot");

            migrationBuilder.DropColumn(
                name: "group_id",
                table: "slot");

            migrationBuilder.DropColumn(
                name: "requires_login",
                table: "slot");

            migrationBuilder.AddColumn<int>(
                name: "max_bookings",
                table: "slot",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "max_bookings",
                table: "slot");

            migrationBuilder.AddColumn<bool>(
                name: "can_pay_later",
                table: "slot",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "group_id",
                table: "slot",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "requires_login",
                table: "slot",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
