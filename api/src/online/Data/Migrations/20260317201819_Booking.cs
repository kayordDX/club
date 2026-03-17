using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Online.Data.Migrations
{
    /// <inheritdoc />
    public partial class Booking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_extra_booking_booking_status_booking_status_id",
                table: "extra_booking");

            migrationBuilder.DropForeignKey(
                name: "fk_extra_booking_facility_extra_facility_extra_id",
                table: "extra_booking");

            migrationBuilder.DropForeignKey(
                name: "fk_extra_booking_payment_payment_id",
                table: "extra_booking");

            migrationBuilder.DropForeignKey(
                name: "fk_extra_booking_slot_slot_id",
                table: "extra_booking");

            migrationBuilder.DropForeignKey(
                name: "fk_extra_booking_users_user_id",
                table: "extra_booking");

            migrationBuilder.DropTable(
                name: "bill");

            migrationBuilder.DropTable(
                name: "slot_booking");

            migrationBuilder.DropPrimaryKey(
                name: "pk_extra_booking",
                table: "extra_booking");

            migrationBuilder.DropIndex(
                name: "ix_extra_booking_booking_status_id",
                table: "extra_booking");

            migrationBuilder.DropIndex(
                name: "ix_extra_booking_payment_id",
                table: "extra_booking");

            migrationBuilder.DropIndex(
                name: "ix_extra_booking_user_id",
                table: "extra_booking");

            migrationBuilder.DropColumn(
                name: "id",
                table: "extra_booking");

            migrationBuilder.DropColumn(
                name: "payment_id",
                table: "extra_booking");

            migrationBuilder.DropColumn(
                name: "status_date",
                table: "extra_booking");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "extra_booking");

            migrationBuilder.RenameColumn(
                name: "facility_extra_id",
                table: "extra_booking",
                newName: "booking_id");

            migrationBuilder.RenameColumn(
                name: "booking_status_id",
                table: "extra_booking",
                newName: "extra_id");

            migrationBuilder.RenameIndex(
                name: "ix_extra_booking_facility_extra_id",
                table: "extra_booking",
                newName: "ix_extra_booking_booking_id");

            migrationBuilder.AddColumn<DateTime>(
                name: "created",
                table: "payment",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                table: "payment",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_modified",
                table: "payment",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "last_modified_by",
                table: "payment",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "payment_type_id",
                table: "payment",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<Guid>(
                name: "slot_id",
                table: "extra_booking",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddPrimaryKey(
                name: "pk_extra_booking",
                table: "extra_booking",
                columns: new[] { "extra_id", "booking_id" });

            migrationBuilder.CreateTable(
                name: "booking",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    booking_status_id = table.Column<int>(type: "integer", nullable: false),
                    booking_status_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_paid = table.Column<bool>(type: "boolean", nullable: false),
                    amount_outstanding = table.Column<decimal>(type: "numeric", nullable: false),
                    amount_paid = table.Column<decimal>(type: "numeric", nullable: false),
                    created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    last_modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_modified_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking", x => x.id);
                    table.ForeignKey(
                        name: "fk_booking_booking_status_booking_status_id",
                        column: x => x.booking_status_id,
                        principalTable: "booking_status",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_booking_users_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "payment_booking",
                columns: table => new
                {
                    payment_id = table.Column<int>(type: "integer", nullable: false),
                    booking_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_booking", x => new { x.payment_id, x.booking_id });
                    table.ForeignKey(
                        name: "fk_payment_booking_booking_booking_id",
                        column: x => x.booking_id,
                        principalTable: "booking",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_payment_booking_payment_payment_id",
                        column: x => x.payment_id,
                        principalTable: "payment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "slot_contract_booking",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    slot_contract_id = table.Column<int>(type: "integer", nullable: false),
                    booking_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "text", nullable: true),
                    email = table.Column<string>(type: "text", nullable: true),
                    cellphone = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_slot_contract_booking", x => x.id);
                    table.ForeignKey(
                        name: "fk_slot_contract_booking_booking_booking_id",
                        column: x => x.booking_id,
                        principalTable: "booking",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_slot_contract_booking_slot_contract_slot_contract_id",
                        column: x => x.slot_contract_id,
                        principalTable: "slot_contract",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_payment_payment_type_id",
                table: "payment",
                column: "payment_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_booking_booking_status_id",
                table: "booking",
                column: "booking_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_booking_user_id",
                table: "booking",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_booking_booking_id",
                table: "payment_booking",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "ix_slot_contract_booking_booking_id",
                table: "slot_contract_booking",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "ix_slot_contract_booking_slot_contract_id",
                table: "slot_contract_booking",
                column: "slot_contract_id");

            migrationBuilder.AddForeignKey(
                name: "fk_extra_booking_booking_booking_id",
                table: "extra_booking",
                column: "booking_id",
                principalTable: "booking",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_extra_booking_extra_extra_id",
                table: "extra_booking",
                column: "extra_id",
                principalTable: "extra",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_extra_booking_slot_slot_id",
                table: "extra_booking",
                column: "slot_id",
                principalTable: "slot",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_payment_payment_type_payment_type_id",
                table: "payment",
                column: "payment_type_id",
                principalTable: "payment_type",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_extra_booking_booking_booking_id",
                table: "extra_booking");

            migrationBuilder.DropForeignKey(
                name: "fk_extra_booking_extra_extra_id",
                table: "extra_booking");

            migrationBuilder.DropForeignKey(
                name: "fk_extra_booking_slot_slot_id",
                table: "extra_booking");

            migrationBuilder.DropForeignKey(
                name: "fk_payment_payment_type_payment_type_id",
                table: "payment");

            migrationBuilder.DropTable(
                name: "payment_booking");

            migrationBuilder.DropTable(
                name: "slot_contract_booking");

            migrationBuilder.DropTable(
                name: "booking");

            migrationBuilder.DropIndex(
                name: "ix_payment_payment_type_id",
                table: "payment");

            migrationBuilder.DropPrimaryKey(
                name: "pk_extra_booking",
                table: "extra_booking");

            migrationBuilder.DropColumn(
                name: "created",
                table: "payment");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "payment");

            migrationBuilder.DropColumn(
                name: "last_modified",
                table: "payment");

            migrationBuilder.DropColumn(
                name: "last_modified_by",
                table: "payment");

            migrationBuilder.DropColumn(
                name: "payment_type_id",
                table: "payment");

            migrationBuilder.RenameColumn(
                name: "booking_id",
                table: "extra_booking",
                newName: "facility_extra_id");

            migrationBuilder.RenameColumn(
                name: "extra_id",
                table: "extra_booking",
                newName: "booking_status_id");

            migrationBuilder.RenameIndex(
                name: "ix_extra_booking_booking_id",
                table: "extra_booking",
                newName: "ix_extra_booking_facility_extra_id");

            migrationBuilder.AlterColumn<Guid>(
                name: "slot_id",
                table: "extra_booking",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "id",
                table: "extra_booking",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<int>(
                name: "payment_id",
                table: "extra_booking",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "status_date",
                table: "extra_booking",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                table: "extra_booking",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "pk_extra_booking",
                table: "extra_booking",
                column: "id");

            migrationBuilder.CreateTable(
                name: "bill",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    payment_id = table.Column<int>(type: "integer", nullable: false),
                    payment_type_id = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    last_modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_modified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    received_by = table.Column<Guid>(type: "uuid", nullable: false),
                    received_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
                    booking_status_id = table.Column<int>(type: "integer", nullable: false),
                    payment_id = table.Column<int>(type: "integer", nullable: true),
                    slot_contract_id = table.Column<int>(type: "integer", nullable: false),
                    slot_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    booking_status_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    last_modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_modified_by = table.Column<Guid>(type: "uuid", nullable: true)
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
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_extra_booking_booking_status_id",
                table: "extra_booking",
                column: "booking_status_id");

            migrationBuilder.CreateIndex(
                name: "ix_extra_booking_payment_id",
                table: "extra_booking",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "ix_extra_booking_user_id",
                table: "extra_booking",
                column: "user_id");

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

            migrationBuilder.AddForeignKey(
                name: "fk_extra_booking_booking_status_booking_status_id",
                table: "extra_booking",
                column: "booking_status_id",
                principalTable: "booking_status",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_extra_booking_facility_extra_facility_extra_id",
                table: "extra_booking",
                column: "facility_extra_id",
                principalTable: "facility_extra",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_extra_booking_payment_payment_id",
                table: "extra_booking",
                column: "payment_id",
                principalTable: "payment",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_extra_booking_slot_slot_id",
                table: "extra_booking",
                column: "slot_id",
                principalTable: "slot",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_extra_booking_users_user_id",
                table: "extra_booking",
                column: "user_id",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
