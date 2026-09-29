using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Club.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentVoucherLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "payment_voucher",
                columns: table => new
                {
                    payment_id = table.Column<int>(type: "integer", nullable: false),
                    wallet_voucher_grant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    units = table.Column<decimal>(type: "numeric", nullable: false),
                    slot_contract_booking_id = table.Column<int>(type: "integer", nullable: true),
                    extra_id = table.Column<int>(type: "integer", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_voucher", x => x.payment_id);
                    table.ForeignKey(
                        name: "fk_payment_voucher_payment_payment_id",
                        column: x => x.payment_id,
                        principalTable: "payment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "fk_payment_voucher_wallet_voucher_grant_wallet_voucher_grant_id",
                        column: x => x.wallet_voucher_grant_id,
                        principalTable: "wallet_voucher_grant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateIndex(name: "ix_payment_voucher_wallet_voucher_grant_id", table: "payment_voucher", column: "wallet_voucher_grant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "payment_voucher");
        }
    }
}
