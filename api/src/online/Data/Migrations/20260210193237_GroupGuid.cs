using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Online.Data.Migrations
{
    /// <inheritdoc />
    public partial class GroupGuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "slot_group_id",
                table: "slot");

            migrationBuilder.AddColumn<Guid>(
                name: "group_id",
                table: "slot",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "group_id",
                table: "slot");

            migrationBuilder.AddColumn<int>(
                name: "slot_group_id",
                table: "slot",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
