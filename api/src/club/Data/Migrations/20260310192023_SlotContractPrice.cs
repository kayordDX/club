using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Online.Data.Migrations
{
    /// <inheritdoc />
    public partial class SlotContractPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "price",
                table: "slot");

            migrationBuilder.AddColumn<bool>(
                name: "can_pay_later",
                table: "slot_contract",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "slot_contract",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "can_pay_later",
                table: "slot_contract");

            migrationBuilder.DropColumn(
                name: "description",
                table: "slot_contract");

            migrationBuilder.AddColumn<decimal>(
                name: "price",
                table: "slot",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
