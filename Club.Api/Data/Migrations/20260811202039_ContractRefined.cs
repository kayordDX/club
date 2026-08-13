using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Club.Data.Migrations
{
    /// <inheritdoc />
    public partial class ContractRefined : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "contract_history");

            migrationBuilder.DropTable(name: "contract_outlet");

            migrationBuilder.CreateTable(
                name: "contract_facility",
                columns: table => new
                {
                    contract_id = table.Column<int>(type: "integer", nullable: false),
                    facility_id = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contract_facility", x => new { x.contract_id, x.facility_id });
                    table.ForeignKey(
                        name: "fk_contract_facility_contract_contract_id",
                        column: x => x.contract_id,
                        principalTable: "contract",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "fk_contract_facility_facility_facility_id",
                        column: x => x.facility_id,
                        principalTable: "facility",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "token_type",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    is_extra = table.Column<bool>(type: "boolean", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_token_type", x => x.id);
                }
            );

            migrationBuilder.CreateTable(
                name: "wallet",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<int>(type: "integer", nullable: false),
                    currency = table.Column<string>(type: "text", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wallet", x => x.id);
                }
            );

            migrationBuilder.CreateTable(
                name: "wallet_balance",
                columns: table => new
                {
                    wallet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    balance = table.Column<decimal>(type: "numeric", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wallet_balance", x => x.wallet_id);
                }
            );

            migrationBuilder.CreateTable(
                name: "wallet_transaction_status",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wallet_transaction_status", x => x.id);
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
                name: "wallet_transaction",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    wallet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    wallet_transaction_status_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reference_id = table.Column<string>(type: "text", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wallet_transaction", x => x.id);
                    table.ForeignKey(
                        name: "fk_wallet_transaction_wallet_transaction_status_wallet_transac",
                        column: x => x.wallet_transaction_status_id,
                        principalTable: "wallet_transaction_status",
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

            migrationBuilder.CreateIndex(name: "ix_contract_facility_facility_id", table: "contract_facility", column: "facility_id");

            migrationBuilder.CreateIndex(name: "ix_contract_token_token_id", table: "contract_token", column: "token_id");

            migrationBuilder.CreateIndex(name: "ix_token_token_type_id", table: "token", column: "token_type_id");

            migrationBuilder.CreateIndex(name: "ix_token_type_facility_facility_id", table: "token_type_facility", column: "facility_id");

            migrationBuilder.CreateIndex(name: "ix_wallet_token_token_type_id", table: "wallet_token", column: "token_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_wallet_transaction_wallet_transaction_status_id",
                table: "wallet_transaction",
                column: "wallet_transaction_status_id"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "contract_facility");

            migrationBuilder.DropTable(name: "contract_token");

            migrationBuilder.DropTable(name: "token_type_facility");

            migrationBuilder.DropTable(name: "wallet_balance");

            migrationBuilder.DropTable(name: "wallet_token");

            migrationBuilder.DropTable(name: "wallet_transaction");

            migrationBuilder.DropTable(name: "token");

            migrationBuilder.DropTable(name: "wallet");

            migrationBuilder.DropTable(name: "wallet_transaction_status");

            migrationBuilder.DropTable(name: "token_type");

            migrationBuilder.CreateTable(
                name: "contract_history",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    facility_id = table.Column<int>(type: "integer", nullable: false),
                    contract_id = table.Column<int>(type: "integer", nullable: false),
                    created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    frequency = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    last_modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_modified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "text", nullable: false),
                    price = table.Column<decimal>(type: "numeric", nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contract_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_contract_history_facility_facility_id",
                        column: x => x.facility_id,
                        principalTable: "facility",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "contract_outlet",
                columns: table => new
                {
                    id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    contract_id = table.Column<int>(type: "integer", nullable: false),
                    outlet_id = table.Column<int>(type: "integer", nullable: false),
                    contract_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    contract_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    last_modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_modified_by = table.Column<Guid>(type: "uuid", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contract_outlet", x => x.id);
                    table.ForeignKey(
                        name: "fk_contract_outlet_contract_contract_id",
                        column: x => x.contract_id,
                        principalTable: "contract",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "fk_contract_outlet_outlet_outlet_id",
                        column: x => x.outlet_id,
                        principalTable: "outlet",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(name: "ix_contract_history_facility_id", table: "contract_history", column: "facility_id");

            migrationBuilder.CreateIndex(name: "ix_contract_outlet_contract_id", table: "contract_outlet", column: "contract_id");

            migrationBuilder.CreateIndex(name: "ix_contract_outlet_outlet_id", table: "contract_outlet", column: "outlet_id");
        }
    }
}
