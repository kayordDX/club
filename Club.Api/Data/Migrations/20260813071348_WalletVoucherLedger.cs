using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Club.Data.Migrations
{
    /// <inheritdoc />
    public partial class WalletVoucherLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "fk_contract_facility_facility_id", table: "contract");

            migrationBuilder.DropTable(name: "contract_token");

            migrationBuilder.DropTable(name: "token_type_facility");

            migrationBuilder.DropTable(name: "wallet_token");

            migrationBuilder.DropTable(name: "token");

            migrationBuilder.DropTable(name: "token_type");

            migrationBuilder.DropIndex(name: "ix_contract_facility_id", table: "contract");

            migrationBuilder.DropColumn(name: "facility_id", table: "contract");

            migrationBuilder.AddColumn<int>(name: "wallet_transaction_type_id", table: "wallet_transaction", type: "integer", nullable: false, defaultValue: 0);

            // wallet.user_id changes type integer -> uuid. PostgreSQL cannot cast
            // integer to uuid directly; the table holds no releasable data yet (no
            // consumers or seed write to it), so drop and recreate the column.
            migrationBuilder.DropColumn(name: "user_id", table: "wallet");

            migrationBuilder.AddColumn<Guid>(name: "user_id", table: "wallet", type: "uuid", nullable: false);

            // wallet.is_active changes type integer -> boolean. Preserve the
            // existing meaning (non-zero == true) via an explicit cast.
            migrationBuilder.Sql(
                """
                ALTER TABLE wallet ALTER COLUMN is_active TYPE boolean USING (is_active <> 0);
                """
            );

            migrationBuilder.AlterColumn<DateTime>(
                name: "created",
                table: "contract",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified)
            );

            migrationBuilder.CreateTable(
                name: "voucher",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    is_extra = table.Column<bool>(type: "boolean", nullable: false),
                    redemption_kind = table.Column<int>(type: "integer", nullable: false),
                    discount_mode = table.Column<int>(type: "integer", nullable: true),
                    discount_value = table.Column<decimal>(type: "numeric", nullable: true),
                    max_discount_amount = table.Column<decimal>(type: "numeric", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_voucher", x => x.id);
                }
            );

            migrationBuilder.CreateTable(
                name: "wallet_transaction_type",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wallet_transaction_type", x => x.id);
                }
            );

            migrationBuilder.CreateTable(
                name: "contract_voucher",
                columns: table => new
                {
                    contract_id = table.Column<int>(type: "integer", nullable: false),
                    voucher_id = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false, defaultValue: 1m),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contract_voucher", x => new { x.contract_id, x.voucher_id });
                    table.ForeignKey(
                        name: "fk_contract_voucher_contract_contract_id",
                        column: x => x.contract_id,
                        principalTable: "contract",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "fk_contract_voucher_voucher_voucher_id",
                        column: x => x.voucher_id,
                        principalTable: "voucher",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "voucher_facility",
                columns: table => new
                {
                    voucher_id = table.Column<int>(type: "integer", nullable: false),
                    facility_id = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_voucher_facility", x => new { x.voucher_id, x.facility_id });
                    table.ForeignKey(
                        name: "fk_voucher_facility_facility_facility_id",
                        column: x => x.facility_id,
                        principalTable: "facility",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "fk_voucher_facility_voucher_voucher_id",
                        column: x => x.voucher_id,
                        principalTable: "voucher",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "wallet_voucher_grant",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    wallet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_contract_id = table.Column<int>(type: "integer", nullable: false),
                    voucher_id = table.Column<int>(type: "integer", nullable: false),
                    amount_granted = table.Column<decimal>(type: "numeric", nullable: false),
                    amount_remaining = table.Column<decimal>(type: "numeric", nullable: false),
                    granted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expiry_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wallet_voucher_grant", x => x.id);
                    table.ForeignKey(
                        name: "fk_wallet_voucher_grant_user_contract_user_contract_id",
                        column: x => x.user_contract_id,
                        principalTable: "user_contract",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "fk_wallet_voucher_grant_voucher_voucher_id",
                        column: x => x.voucher_id,
                        principalTable: "voucher",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "fk_wallet_voucher_grant_wallet_wallet_id",
                        column: x => x.wallet_id,
                        principalTable: "wallet",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(name: "ix_wallet_transaction_wallet_id", table: "wallet_transaction", column: "wallet_id");

            migrationBuilder.CreateIndex(
                name: "ix_wallet_transaction_wallet_transaction_type_id",
                table: "wallet_transaction",
                column: "wallet_transaction_type_id"
            );

            migrationBuilder.CreateIndex(name: "ix_wallet_user_id", table: "wallet", column: "user_id", unique: true);

            migrationBuilder.CreateIndex(name: "ix_contract_voucher_voucher_id", table: "contract_voucher", column: "voucher_id");

            migrationBuilder.CreateIndex(name: "ix_voucher_facility_facility_id", table: "voucher_facility", column: "facility_id");

            migrationBuilder.CreateIndex(name: "ix_wallet_voucher_grant_user_contract_id", table: "wallet_voucher_grant", column: "user_contract_id");

            migrationBuilder.CreateIndex(name: "ix_wallet_voucher_grant_voucher_id", table: "wallet_voucher_grant", column: "voucher_id");

            migrationBuilder.CreateIndex(name: "ix_wallet_voucher_grant_wallet_id", table: "wallet_voucher_grant", column: "wallet_id");

            migrationBuilder.AddForeignKey(
                name: "fk_wallet_users_user_id",
                table: "wallet",
                column: "user_id",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade
            );

            migrationBuilder.AddForeignKey(
                name: "fk_wallet_balance_wallet_wallet_id",
                table: "wallet_balance",
                column: "wallet_id",
                principalTable: "wallet",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade
            );

            migrationBuilder.AddForeignKey(
                name: "fk_wallet_transaction_wallet_transaction_type_wallet_transaction_type_id",
                table: "wallet_transaction",
                column: "wallet_transaction_type_id",
                principalTable: "wallet_transaction_type",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict
            );

            migrationBuilder.AddForeignKey(
                name: "fk_wallet_transaction_wallet_wallet_id",
                table: "wallet_transaction",
                column: "wallet_id",
                principalTable: "wallet",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade
            );

            // The backfill default added for wallet_transaction_type_id is no
            // longer needed once the FK is in place (0 is not a valid type).
            migrationBuilder.Sql(
                """
                ALTER TABLE wallet_transaction ALTER COLUMN wallet_transaction_type_id DROP DEFAULT;
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "fk_wallet_users_user_id", table: "wallet");

            migrationBuilder.DropForeignKey(name: "fk_wallet_balance_wallet_wallet_id", table: "wallet_balance");

            migrationBuilder.DropForeignKey(name: "fk_wallet_transaction_wallet_transaction_type_wallet_transaction_type_id", table: "wallet_transaction");

            migrationBuilder.DropForeignKey(name: "fk_wallet_transaction_wallet_wallet_id", table: "wallet_transaction");

            migrationBuilder.DropTable(name: "contract_voucher");

            migrationBuilder.DropTable(name: "voucher_facility");

            migrationBuilder.DropTable(name: "wallet_transaction_type");

            migrationBuilder.DropTable(name: "wallet_voucher_grant");

            migrationBuilder.DropTable(name: "voucher");

            migrationBuilder.DropIndex(name: "ix_wallet_transaction_wallet_id", table: "wallet_transaction");

            migrationBuilder.DropIndex(name: "ix_wallet_transaction_wallet_transaction_type_id", table: "wallet_transaction");

            migrationBuilder.DropIndex(name: "ix_wallet_user_id", table: "wallet");

            migrationBuilder.DropColumn(name: "wallet_transaction_type_id", table: "wallet_transaction");

            migrationBuilder.DropColumn(name: "user_id", table: "wallet");

            migrationBuilder.AddColumn<int>(name: "user_id", table: "wallet", type: "integer", nullable: false);

            migrationBuilder.Sql(
                """
                ALTER TABLE wallet ALTER COLUMN is_active TYPE integer USING (is_active::int);
                """
            );

            migrationBuilder.AlterColumn<DateTime>(
                name: "created",
                table: "contract",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone"
            );

            migrationBuilder.AddColumn<int>(name: "facility_id", table: "contract", type: "integer", nullable: false, defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "token_type",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    description = table.Column<string>(type: "text", nullable: false),
                    is_extra = table.Column<bool>(type: "boolean", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_token_type", x => x.id);
                }
            );

            migrationBuilder.CreateTable(
                name: "token",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    token_type_id = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_token", x => x.id);
                    table.ForeignKey(
                        name: "fk_token_token_type_token_type_id",
                        column: x => x.token_type_id,
                        principalTable: "token_type",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "token_type_facility",
                columns: table => new
                {
                    token_type_id = table.Column<int>(type: "integer", nullable: false),
                    facility_id = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_token_type_facility", x => new { x.token_type_id, x.facility_id });
                    table.ForeignKey(
                        name: "fk_token_type_facility_facility_facility_id",
                        column: x => x.facility_id,
                        principalTable: "facility",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "fk_token_type_facility_token_type_token_type_id",
                        column: x => x.token_type_id,
                        principalTable: "token_type",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "wallet_token",
                columns: table => new
                {
                    wallet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_type_id = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wallet_token", x => new { x.wallet_id, x.token_type_id });
                    table.ForeignKey(
                        name: "fk_wallet_token_token_type_token_type_id",
                        column: x => x.token_type_id,
                        principalTable: "token_type",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "fk_wallet_token_wallet_wallet_id",
                        column: x => x.wallet_id,
                        principalTable: "wallet",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "contract_token",
                columns: table => new
                {
                    contract_id = table.Column<int>(type: "integer", nullable: false),
                    token_id = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contract_token", x => new { x.contract_id, x.token_id });
                    table.ForeignKey(
                        name: "fk_contract_token_contract_contract_id",
                        column: x => x.contract_id,
                        principalTable: "contract",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "fk_contract_token_token_token_id",
                        column: x => x.token_id,
                        principalTable: "token",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(name: "ix_contract_facility_id", table: "contract", column: "facility_id");

            migrationBuilder.CreateIndex(name: "ix_contract_token_token_id", table: "contract_token", column: "token_id");

            migrationBuilder.CreateIndex(name: "ix_token_token_type_id", table: "token", column: "token_type_id");

            migrationBuilder.CreateIndex(name: "ix_token_type_facility_facility_id", table: "token_type_facility", column: "facility_id");

            migrationBuilder.CreateIndex(name: "ix_wallet_token_token_type_id", table: "wallet_token", column: "token_type_id");

            migrationBuilder.AddForeignKey(
                name: "fk_contract_facility_facility_id",
                table: "contract",
                column: "facility_id",
                principalTable: "facility",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict
            );
        }
    }
}
