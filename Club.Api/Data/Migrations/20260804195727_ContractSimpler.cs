using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Club.Data.Migrations
{
    /// <inheritdoc />
    public partial class ContractSimpler : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_contract_business_business_id",
                table: "contract");

            migrationBuilder.DropTable(
                name: "contract_field_config");

            migrationBuilder.DropTable(
                name: "contract_field");

            migrationBuilder.DropIndex(
                name: "ix_contract_business_id",
                table: "contract");

            migrationBuilder.AddColumn<DateTime>(
                name: "end_date",
                table: "contract",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "facility_id",
                table: "contract",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "contract",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "price",
                table: "contract",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "start_date",
                table: "contract",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            // Existing contracts were previously tied to a business. Backfill their
            // new required facility_id from a facility of that business before the FK
            // is added, otherwise the default 0 fails the foreign key check.
            migrationBuilder.Sql(
                """
                UPDATE contract
                SET facility_id = sub.facility_id
                FROM (
                    SELECT c.id AS contract_id, MIN(f.id) AS facility_id
                    FROM contract c
                    JOIN outlet o ON o.business_id = c.business_id
                    JOIN facility f ON f.outlet_id = o.id
                    GROUP BY c.id
                ) sub
                WHERE contract.id = sub.contract_id;

                UPDATE contract
                SET facility_id = (SELECT id FROM facility ORDER BY id LIMIT 1)
                WHERE facility_id = 0
                  AND EXISTS (SELECT 1 FROM facility);
                """);

            migrationBuilder.RenameColumn(
                name: "business_id",
                table: "contract",
                newName: "frequency");

            migrationBuilder.CreateTable(
                name: "booking_item",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    price = table.Column<decimal>(type: "numeric", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    total_price = table.Column<decimal>(type: "numeric", nullable: false),
                    booking_id = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    last_modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_modified_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking_item", x => x.id);
                    table.ForeignKey(
                        name: "fk_booking_item_booking_booking_id",
                        column: x => x.booking_id,
                        principalTable: "booking",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "contract_history",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    contract_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    price = table.Column<decimal>(type: "numeric", nullable: false),
                    frequency = table.Column<int>(type: "integer", nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    facility_id = table.Column<int>(type: "integer", nullable: false),
                    created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    last_modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_modified_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contract_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_contract_history_facility_facility_id",
                        column: x => x.facility_id,
                        principalTable: "facility",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "slot_config_type",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    business_id = table.Column<int>(type: "integer", nullable: false),
                    created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    last_modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_modified_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_slot_config_type", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "slot_config",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    slot_config_type_id = table.Column<int>(type: "integer", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    weekday_id = table.Column<int>(type: "integer", nullable: false),
                    group_count = table.Column<int>(type: "integer", nullable: false),
                    interval = table.Column<int>(type: "integer", nullable: false),
                    created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    last_modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_modified_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_slot_config", x => x.id);
                    table.ForeignKey(
                        name: "fk_slot_config_slot_config_type_slot_config_type_id",
                        column: x => x.slot_config_type_id,
                        principalTable: "slot_config_type",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_contract_facility_id",
                table: "contract",
                column: "facility_id");

            migrationBuilder.CreateIndex(
                name: "ix_booking_item_booking_id",
                table: "booking_item",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "ix_contract_history_facility_id",
                table: "contract_history",
                column: "facility_id");

            migrationBuilder.CreateIndex(
                name: "ix_slot_config_slot_config_type_id",
                table: "slot_config",
                column: "slot_config_type_id");

            migrationBuilder.AddForeignKey(
                name: "fk_contract_facility_facility_id",
                table: "contract",
                column: "facility_id",
                principalTable: "facility",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_contract_facility_facility_id",
                table: "contract");

            migrationBuilder.DropTable(
                name: "booking_item");

            migrationBuilder.DropTable(
                name: "contract_history");

            migrationBuilder.DropTable(
                name: "slot_config");

            migrationBuilder.DropTable(
                name: "slot_config_type");

            migrationBuilder.DropIndex(
                name: "ix_contract_facility_id",
                table: "contract");

            migrationBuilder.DropColumn(
                name: "end_date",
                table: "contract");

            migrationBuilder.DropColumn(
                name: "facility_id",
                table: "contract");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "contract");

            migrationBuilder.DropColumn(
                name: "price",
                table: "contract");

            migrationBuilder.DropColumn(
                name: "start_date",
                table: "contract");

            migrationBuilder.RenameColumn(
                name: "frequency",
                table: "contract",
                newName: "business_id");

            migrationBuilder.CreateTable(
                name: "contract_field",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    business_id = table.Column<int>(type: "integer", nullable: false),
                    created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified)),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    field_validation = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    last_modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_modified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contract_field", x => x.id);
                    table.ForeignKey(
                        name: "fk_contract_field_business_business_id",
                        column: x => x.business_id,
                        principalTable: "business",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "contract_field_config",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    contract_field_id = table.Column<int>(type: "integer", nullable: false),
                    contract_id = table.Column<int>(type: "integer", nullable: false),
                    contract_config_id = table.Column<int>(type: "integer", nullable: false),
                    created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified)),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    last_modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_modified_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contract_field_config", x => x.id);
                    table.ForeignKey(
                        name: "fk_contract_field_config_contract_contract_id",
                        column: x => x.contract_id,
                        principalTable: "contract",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_contract_field_config_contract_field_contract_field_id",
                        column: x => x.contract_field_id,
                        principalTable: "contract_field",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_contract_business_id",
                table: "contract",
                column: "business_id");

            migrationBuilder.CreateIndex(
                name: "ix_contract_field_business_id",
                table: "contract_field",
                column: "business_id");

            migrationBuilder.CreateIndex(
                name: "ix_contract_field_config_contract_field_id",
                table: "contract_field_config",
                column: "contract_field_id");

            migrationBuilder.CreateIndex(
                name: "ix_contract_field_config_contract_id",
                table: "contract_field_config",
                column: "contract_id");

            migrationBuilder.AddForeignKey(
                name: "fk_contract_business_business_id",
                table: "contract",
                column: "business_id",
                principalTable: "business",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
