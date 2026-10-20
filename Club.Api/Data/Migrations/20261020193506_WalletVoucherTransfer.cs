using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Club.Data.Migrations
{
    /// <inheritdoc />
    public partial class WalletVoucherTransfer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "ix_wallet_voucher_grant_audit_wallet_voucher_grant_id", table: "wallet_voucher_grant_audit");

            migrationBuilder.DropCheckConstraint(name: "ck_wallet_voucher_grant_audit_action", table: "wallet_voucher_grant_audit");

            migrationBuilder.AddColumn<Guid>(name: "recipient_user_id", table: "wallet_voucher_grant_audit", type: "uuid", nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_wallet_voucher_grant_audit_wallet_voucher_grant_id_action",
                table: "wallet_voucher_grant_audit",
                columns: new[] { "wallet_voucher_grant_id", "action" },
                unique: true
            );

            migrationBuilder.AddCheckConstraint(
                name: "ck_wallet_voucher_grant_audit_action",
                table: "wallet_voucher_grant_audit",
                sql: "action BETWEEN 1 AND 2"
            );

            migrationBuilder.AddCheckConstraint(
                name: "ck_wallet_voucher_grant_audit_transfer",
                table: "wallet_voucher_grant_audit",
                sql: "action <> 2 OR (assigning_user_id IS NOT NULL AND recipient_user_id IS NOT NULL AND assigning_user_id <> recipient_user_id AND source_type = 4)"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "ix_wallet_voucher_grant_audit_wallet_voucher_grant_id_action", table: "wallet_voucher_grant_audit");

            migrationBuilder.DropCheckConstraint(name: "ck_wallet_voucher_grant_audit_action", table: "wallet_voucher_grant_audit");

            migrationBuilder.DropCheckConstraint(name: "ck_wallet_voucher_grant_audit_transfer", table: "wallet_voucher_grant_audit");

            migrationBuilder.DropColumn(name: "recipient_user_id", table: "wallet_voucher_grant_audit");

            migrationBuilder.CreateIndex(
                name: "ix_wallet_voucher_grant_audit_wallet_voucher_grant_id",
                table: "wallet_voucher_grant_audit",
                column: "wallet_voucher_grant_id",
                unique: true,
                filter: "action = 1"
            );

            migrationBuilder.AddCheckConstraint(name: "ck_wallet_voucher_grant_audit_action", table: "wallet_voucher_grant_audit", sql: "action = 1");
        }
    }
}
