using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Club.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentTrackingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "error_message",
                table: "payment",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "provider_name",
                table: "payment",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "provider_reference",
                table: "payment",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "redirect_url",
                table: "payment",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "transaction_id",
                table: "payment",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_payment_transaction_id",
                table: "payment",
                column: "transaction_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_payment_transaction_id",
                table: "payment");

            migrationBuilder.DropColumn(
                name: "error_message",
                table: "payment");

            migrationBuilder.DropColumn(
                name: "provider_name",
                table: "payment");

            migrationBuilder.DropColumn(
                name: "provider_reference",
                table: "payment");

            migrationBuilder.DropColumn(
                name: "redirect_url",
                table: "payment");

            migrationBuilder.DropColumn(
                name: "transaction_id",
                table: "payment");
        }
    }
}
