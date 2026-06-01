using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Online.Data.Migrations
{
    /// <inheritdoc />
    public partial class GuestBookingSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_slot_booking_users_user_id",
                table: "slot_booking");

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                table: "slot_booking",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "slot_booking",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_slot_booking_users_user_id",
                table: "slot_booking",
                column: "user_id",
                principalTable: "user",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_slot_booking_users_user_id",
                table: "slot_booking");

            migrationBuilder.DropColumn(
                name: "email",
                table: "slot_booking");

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                table: "slot_booking",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_slot_booking_users_user_id",
                table: "slot_booking",
                column: "user_id",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
