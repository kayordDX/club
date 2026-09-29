using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Club.Data.Migrations
{
    /// <inheritdoc />
    public partial class VoucherItemEligibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "voucher_contract",
                columns: table => new
                {
                    voucher_id = table.Column<int>(type: "integer", nullable: false),
                    contract_id = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_voucher_contract", x => new { x.voucher_id, x.contract_id });
                    table.ForeignKey(
                        name: "fk_voucher_contract_contract_contract_id",
                        column: x => x.contract_id,
                        principalTable: "contract",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "fk_voucher_contract_voucher_voucher_id",
                        column: x => x.voucher_id,
                        principalTable: "voucher",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "voucher_extra",
                columns: table => new
                {
                    voucher_id = table.Column<int>(type: "integer", nullable: false),
                    extra_id = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_voucher_extra", x => new { x.voucher_id, x.extra_id });
                    table.ForeignKey(
                        name: "fk_voucher_extra_extra_extra_id",
                        column: x => x.extra_id,
                        principalTable: "extra",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "fk_voucher_extra_voucher_voucher_id",
                        column: x => x.voucher_id,
                        principalTable: "voucher",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(name: "ix_voucher_contract_contract_id", table: "voucher_contract", column: "contract_id");

            migrationBuilder.CreateIndex(name: "ix_voucher_extra_extra_id", table: "voucher_extra", column: "extra_id");

            // Issuance associations are not redemption permissions. No legacy item eligibility is known.
            // Intentionally leave both lists empty: existing entitlements must be configured before use.
            migrationBuilder.Sql(
                """
                DO $$
                DECLARE affected integer;
                BEGIN
                    SELECT count(*) INTO affected FROM voucher WHERE redemption_kind = 1;
                    RAISE NOTICE '% existing entitlement vouchers now have empty item eligibility; configure contracts/extras before redemption', affected;
                END $$;
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "voucher_contract");

            migrationBuilder.DropTable(name: "voucher_extra");
        }
    }
}
