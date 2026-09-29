using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Club.Data.Migrations
{
    /// <inheritdoc />
    public partial class WalletVoucherGrantProvenance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "wallet_voucher_grant_audit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    wallet_voucher_grant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<int>(type: "integer", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    source_type = table.Column<int>(type: "integer", nullable: false),
                    assigning_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_user_contract_id = table.Column<int>(type: "integer", nullable: true),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wallet_voucher_grant_audit", x => x.id);
                    table.CheckConstraint("ck_wallet_voucher_grant_audit_action", "action = 1");
                    table.CheckConstraint("ck_wallet_voucher_grant_audit_contract", "(source_type = 1) = (source_user_contract_id IS NOT NULL)");
                    table.CheckConstraint("ck_wallet_voucher_grant_audit_source", "source_type BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "fk_wallet_voucher_grant_audit_wallet_voucher_grant_wallet_vouc",
                        column: x => x.wallet_voucher_grant_id,
                        principalTable: "wallet_voucher_grant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "ix_wallet_voucher_grant_audit_source_user_contract_id",
                table: "wallet_voucher_grant_audit",
                column: "source_user_contract_id"
            );

            migrationBuilder.CreateIndex(
                name: "ix_wallet_voucher_grant_audit_wallet_voucher_grant_id",
                table: "wallet_voucher_grant_audit",
                column: "wallet_voucher_grant_id",
                unique: true,
                filter: "action = 1"
            );

            // Legacy grants identify the source contract, but not the assigning actor.
            migrationBuilder.Sql(
                """
                INSERT INTO wallet_voucher_grant_audit
                    (id, wallet_voucher_grant_id, action, timestamp, source_type, source_user_contract_id)
                SELECT gen_random_uuid(), id, 1, granted_at, 1, user_contract_id
                FROM wallet_voucher_grant;

                CREATE FUNCTION reject_wallet_voucher_grant_audit_mutation() RETURNS trigger
                LANGUAGE plpgsql AS $$
                BEGIN
                    RAISE EXCEPTION 'Voucher grant audits are append-only';
                END;
                $$;
                CREATE TRIGGER wallet_voucher_grant_audit_append_only
                    BEFORE UPDATE OR DELETE OR TRUNCATE ON wallet_voucher_grant_audit
                    FOR EACH STATEMENT EXECUTE FUNCTION reject_wallet_voucher_grant_audit_mutation();
                """
            );

            migrationBuilder.DropForeignKey(name: "fk_wallet_voucher_grant_user_contract_user_contract_id", table: "wallet_voucher_grant");
            migrationBuilder.DropIndex(name: "ix_wallet_voucher_grant_user_contract_id", table: "wallet_voucher_grant");
            migrationBuilder.DropColumn(name: "user_contract_id", table: "wallet_voucher_grant");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException(
                "Cannot restore mandatory contract provenance for standalone grants or deleted sources. Restore a pre-migration backup instead."
            );
        }
    }
}
