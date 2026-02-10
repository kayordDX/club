using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Online.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_facility_site_site_id",
                table: "facility");

            migrationBuilder.DropTable(
                name: "site");

            migrationBuilder.RenameColumn(
                name: "site_id",
                table: "facility",
                newName: "outlet_id");

            migrationBuilder.RenameIndex(
                name: "ix_facility_site_id",
                table: "facility",
                newName: "ix_facility_outlet_id");

            migrationBuilder.AddColumn<DateTime>(
                name: "created",
                table: "resource",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "created_by",
                table: "resource",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "resource",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_modified",
                table: "resource",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_modified_by",
                table: "resource",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "facility",
                type: "character varying(250)",
                maxLength: 250,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<DateTime>(
                name: "created",
                table: "facility",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "created_by",
                table: "facility",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "facility",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_modified",
                table: "facility",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_modified_by",
                table: "facility",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "booking_status",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking_status", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "business",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    last_modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_business", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outlet_type",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    last_modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outlet_type", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "payment_status",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_status", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "payment_type",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_type", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "role_type",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    is_super_admin = table.Column<bool>(type: "boolean", nullable: false),
                    created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    last_modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_type", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "slot",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    resource_id = table.Column<int>(type: "integer", nullable: true),
                    start_datetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_datetime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    slot_group_id = table.Column<int>(type: "integer", nullable: false),
                    facility_id = table.Column<int>(type: "integer", nullable: true),
                    created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified)),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    last_modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_slot", x => x.id);
                    table.ForeignKey(
                        name: "fk_slot_facility_facility_id",
                        column: x => x.facility_id,
                        principalTable: "facility",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_slot_resource_resource_id",
                        column: x => x.resource_id,
                        principalTable: "resource",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "validation",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_validation", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "contract",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    business_id = table.Column<int>(type: "integer", nullable: false),
                    created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified)),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    last_modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contract", x => x.id);
                    table.ForeignKey(
                        name: "fk_contract_business_business_id",
                        column: x => x.business_id,
                        principalTable: "business",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "contract_config",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    field_validation = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    business_id = table.Column<int>(type: "integer", nullable: false),
                    created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified)),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    last_modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contract_config", x => x.id);
                    table.ForeignKey(
                        name: "fk_contract_config_business_business_id",
                        column: x => x.business_id,
                        principalTable: "business",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "outlet",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    business_id = table.Column<int>(type: "integer", nullable: false),
                    vat_number = table.Column<string>(type: "text", nullable: false),
                    logo = table.Column<string>(type: "text", nullable: true),
                    address = table.Column<string>(type: "text", nullable: true),
                    company = table.Column<string>(type: "text", nullable: true),
                    registration = table.Column<string>(type: "text", nullable: true),
                    display_name = table.Column<string>(type: "text", nullable: false),
                    outlet_type_id = table.Column<int>(type: "integer", nullable: true),
                    is_active = table.Column<int>(type: "integer", nullable: false),
                    created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    last_modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outlet", x => x.id);
                    table.ForeignKey(
                        name: "fk_outlet_business_business_id",
                        column: x => x.business_id,
                        principalTable: "business",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_outlet_outlet_type_outlet_type_id",
                        column: x => x.outlet_type_id,
                        principalTable: "outlet_type",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "payment",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    payment_status_id = table.Column<int>(type: "integer", nullable: false),
                    payment_status_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment", x => x.id);
                    table.ForeignKey(
                        name: "fk_payment_payment_status_payment_status_id",
                        column: x => x.payment_status_id,
                        principalTable: "payment_status",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "slot_contract",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    slot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<int>(type: "integer", nullable: false),
                    price = table.Column<decimal>(type: "numeric", nullable: false),
                    validation_id = table.Column<int>(type: "integer", nullable: false),
                    created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    last_modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_slot_contract", x => x.id);
                    table.ForeignKey(
                        name: "fk_slot_contract_contract_contract_id",
                        column: x => x.contract_id,
                        principalTable: "contract",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_slot_contract_slot_slot_id",
                        column: x => x.slot_id,
                        principalTable: "slot",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_slot_contract_validation_validation_id",
                        column: x => x.validation_id,
                        principalTable: "validation",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_contract",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    contract_id = table.Column<int>(type: "integer", nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    price = table.Column<decimal>(type: "numeric", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    last_modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_contract", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_contract_contract_contract_id",
                        column: x => x.contract_id,
                        principalTable: "contract",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_contract_users_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "contract_contract_config",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    contract_id = table.Column<int>(type: "integer", nullable: false),
                    contract_config_id = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified)),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    last_modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contract_contract_config", x => x.id);
                    table.ForeignKey(
                        name: "fk_contract_contract_config_contract_config_contract_config_id",
                        column: x => x.contract_config_id,
                        principalTable: "contract_config",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_contract_contract_config_contract_contract_id",
                        column: x => x.contract_id,
                        principalTable: "contract",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "contract_outlet",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    contract_id = table.Column<int>(type: "integer", nullable: false),
                    outlet_id = table.Column<int>(type: "integer", nullable: false),
                    contract_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    contract_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    last_modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contract_outlet", x => x.id);
                    table.ForeignKey(
                        name: "fk_contract_outlet_contract_contract_id",
                        column: x => x.contract_id,
                        principalTable: "contract",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_contract_outlet_outlet_outlet_id",
                        column: x => x.outlet_id,
                        principalTable: "outlet",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "extra",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    outlet_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_extra", x => x.id);
                    table.ForeignKey(
                        name: "fk_extra_outlet_outlet_id",
                        column: x => x.outlet_id,
                        principalTable: "outlet",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bill",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    payment_id = table.Column<int>(type: "integer", nullable: false),
                    payment_type_id = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    received_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    received_by = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    last_modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bill", x => x.id);
                    table.ForeignKey(
                        name: "fk_bill_payment_payment_id",
                        column: x => x.payment_id,
                        principalTable: "payment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_bill_payment_type_payment_type_id",
                        column: x => x.payment_type_id,
                        principalTable: "payment_type",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_bill_users_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "slot_booking",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    slot_contract_id = table.Column<int>(type: "integer", nullable: false),
                    booking_status_id = table.Column<int>(type: "integer", nullable: false),
                    booking_status_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<int>(type: "integer", nullable: true),
                    slot_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    last_modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_slot_booking", x => x.id);
                    table.ForeignKey(
                        name: "fk_slot_booking_booking_status_booking_status_id",
                        column: x => x.booking_status_id,
                        principalTable: "booking_status",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_slot_booking_payment_payment_id",
                        column: x => x.payment_id,
                        principalTable: "payment",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_slot_booking_slot_contract_slot_contract_id",
                        column: x => x.slot_contract_id,
                        principalTable: "slot_contract",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_slot_booking_slot_slot_id",
                        column: x => x.slot_id,
                        principalTable: "slot",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_slot_booking_users_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "facility_extra",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    facility_id = table.Column<int>(type: "integer", nullable: false),
                    extra_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    code = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    price = table.Column<decimal>(type: "numeric", nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    booking_status_id = table.Column<int>(type: "integer", nullable: false),
                    is_available = table.Column<bool>(type: "boolean", nullable: false),
                    is_online = table.Column<bool>(type: "boolean", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified)),
                    created_by = table.Column<string>(type: "text", nullable: true),
                    last_modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_modified_by = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_facility_extra", x => x.id);
                    table.ForeignKey(
                        name: "fk_facility_extra_booking_status_booking_status_id",
                        column: x => x.booking_status_id,
                        principalTable: "booking_status",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_facility_extra_extra_extra_id",
                        column: x => x.extra_id,
                        principalTable: "extra",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_facility_extra_facility_facility_id",
                        column: x => x.facility_id,
                        principalTable: "facility",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_facility_extra_users_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "extra_booking",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    facility_extra_id = table.Column<int>(type: "integer", nullable: false),
                    slot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_status_id = table.Column<int>(type: "integer", nullable: false),
                    status_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    payment_id = table.Column<int>(type: "integer", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_extra_booking", x => x.id);
                    table.ForeignKey(
                        name: "fk_extra_booking_booking_status_booking_status_id",
                        column: x => x.booking_status_id,
                        principalTable: "booking_status",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_extra_booking_facility_extra_facility_extra_id",
                        column: x => x.facility_extra_id,
                        principalTable: "facility_extra",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_extra_booking_payment_payment_id",
                        column: x => x.payment_id,
                        principalTable: "payment",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_extra_booking_slot_slot_id",
                        column: x => x.slot_id,
                        principalTable: "slot",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_extra_booking_users_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_bill_payment_id",
                table: "bill",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "ix_bill_payment_type_id",
                table: "bill",
                column: "payment_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_bill_user_id",
                table: "bill",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_contract_business_id",
                table: "contract",
                column: "business_id");

            migrationBuilder.CreateIndex(
                name: "ix_contract_config_business_id",
                table: "contract_config",
                column: "business_id");

            migrationBuilder.CreateIndex(
                name: "ix_contract_contract_config_contract_config_id",
                table: "contract_contract_config",
                column: "contract_config_id");

            migrationBuilder.CreateIndex(
                name: "ix_contract_contract_config_contract_id",
                table: "contract_contract_config",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "ix_contract_outlet_contract_id",
                table: "contract_outlet",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "ix_contract_outlet_outlet_id",
                table: "contract_outlet",
                column: "outlet_id");

            migrationBuilder.CreateIndex(
                name: "ix_extra_outlet_id",
                table: "extra",
                column: "outlet_id");

            migrationBuilder.CreateIndex(
                name: "ix_extra_booking_booking_status_id",
                table: "extra_booking",
                column: "booking_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_extra_booking_facility_extra_id",
                table: "extra_booking",
                column: "facility_extra_id");

            migrationBuilder.CreateIndex(
                name: "ix_extra_booking_payment_id",
                table: "extra_booking",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "ix_extra_booking_slot_id",
                table: "extra_booking",
                column: "slot_id");

            migrationBuilder.CreateIndex(
                name: "ix_extra_booking_user_id",
                table: "extra_booking",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_facility_extra_booking_status_id",
                table: "facility_extra",
                column: "booking_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_facility_extra_extra_id",
                table: "facility_extra",
                column: "extra_id");

            migrationBuilder.CreateIndex(
                name: "ix_facility_extra_facility_id",
                table: "facility_extra",
                column: "facility_id");

            migrationBuilder.CreateIndex(
                name: "ix_facility_extra_user_id",
                table: "facility_extra",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_outlet_business_id",
                table: "outlet",
                column: "business_id");

            migrationBuilder.CreateIndex(
                name: "ix_outlet_outlet_type_id",
                table: "outlet",
                column: "outlet_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_payment_status_id",
                table: "payment",
                column: "payment_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_slot_facility_id",
                table: "slot",
                column: "facility_id");

            migrationBuilder.CreateIndex(
                name: "ix_slot_resource_id",
                table: "slot",
                column: "resource_id");

            migrationBuilder.CreateIndex(
                name: "ix_slot_booking_booking_status_id",
                table: "slot_booking",
                column: "booking_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_slot_booking_payment_id",
                table: "slot_booking",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "ix_slot_booking_slot_contract_id",
                table: "slot_booking",
                column: "slot_contract_id");

            migrationBuilder.CreateIndex(
                name: "ix_slot_booking_slot_id",
                table: "slot_booking",
                column: "slot_id");

            migrationBuilder.CreateIndex(
                name: "ix_slot_booking_user_id",
                table: "slot_booking",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_slot_contract_contract_id",
                table: "slot_contract",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "ix_slot_contract_slot_id",
                table: "slot_contract",
                column: "slot_id");

            migrationBuilder.CreateIndex(
                name: "ix_slot_contract_validation_id",
                table: "slot_contract",
                column: "validation_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_contract_contract_id",
                table: "user_contract",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_contract_user_id",
                table: "user_contract",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "fk_facility_outlet_outlet_id",
                table: "facility",
                column: "outlet_id",
                principalTable: "outlet",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_facility_outlet_outlet_id",
                table: "facility");

            migrationBuilder.DropTable(
                name: "bill");

            migrationBuilder.DropTable(
                name: "contract_contract_config");

            migrationBuilder.DropTable(
                name: "contract_outlet");

            migrationBuilder.DropTable(
                name: "extra_booking");

            migrationBuilder.DropTable(
                name: "role_type");

            migrationBuilder.DropTable(
                name: "slot_booking");

            migrationBuilder.DropTable(
                name: "user_contract");

            migrationBuilder.DropTable(
                name: "payment_type");

            migrationBuilder.DropTable(
                name: "contract_config");

            migrationBuilder.DropTable(
                name: "facility_extra");

            migrationBuilder.DropTable(
                name: "payment");

            migrationBuilder.DropTable(
                name: "slot_contract");

            migrationBuilder.DropTable(
                name: "booking_status");

            migrationBuilder.DropTable(
                name: "extra");

            migrationBuilder.DropTable(
                name: "payment_status");

            migrationBuilder.DropTable(
                name: "contract");

            migrationBuilder.DropTable(
                name: "slot");

            migrationBuilder.DropTable(
                name: "validation");

            migrationBuilder.DropTable(
                name: "outlet");

            migrationBuilder.DropTable(
                name: "business");

            migrationBuilder.DropTable(
                name: "outlet_type");

            migrationBuilder.DropColumn(
                name: "created",
                table: "resource");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "resource");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "resource");

            migrationBuilder.DropColumn(
                name: "last_modified",
                table: "resource");

            migrationBuilder.DropColumn(
                name: "last_modified_by",
                table: "resource");

            migrationBuilder.DropColumn(
                name: "created",
                table: "facility");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "facility");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "facility");

            migrationBuilder.DropColumn(
                name: "last_modified",
                table: "facility");

            migrationBuilder.DropColumn(
                name: "last_modified_by",
                table: "facility");

            migrationBuilder.RenameColumn(
                name: "outlet_id",
                table: "facility",
                newName: "site_id");

            migrationBuilder.RenameIndex(
                name: "ix_facility_outlet_id",
                table: "facility",
                newName: "ix_facility_site_id");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "facility",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(250)",
                oldMaxLength: 250);

            migrationBuilder.CreateTable(
                name: "site",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_site", x => x.id);
                });

            migrationBuilder.AddForeignKey(
                name: "fk_facility_site_site_id",
                table: "facility",
                column: "site_id",
                principalTable: "site",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
