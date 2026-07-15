using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Club.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFacilityIdToPaymentProviderConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_payment_provider_config_provider_key",
                table: "payment_provider_config");

            migrationBuilder.AddColumn<int>(
                name: "facility_id",
                table: "payment_provider_config",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Remove existing global config rows that have no valid facility association.
            // They will be re-created per-facility by the seeder after migration.
            migrationBuilder.Sql("DELETE FROM payment_provider_config;");

            migrationBuilder.CreateIndex(
                name: "ix_payment_provider_config_facility_id_provider_key",
                table: "payment_provider_config",
                columns: new[] { "facility_id", "provider_key" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_payment_provider_config_facility_facility_id",
                table: "payment_provider_config",
                column: "facility_id",
                principalTable: "facility",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_payment_provider_config_facility_facility_id",
                table: "payment_provider_config");

            migrationBuilder.DropIndex(
                name: "ix_payment_provider_config_facility_id_provider_key",
                table: "payment_provider_config");

            migrationBuilder.DropColumn(
                name: "facility_id",
                table: "payment_provider_config");

            migrationBuilder.CreateIndex(
                name: "ix_payment_provider_config_provider_key",
                table: "payment_provider_config",
                column: "provider_key",
                unique: true);
        }
    }
}
