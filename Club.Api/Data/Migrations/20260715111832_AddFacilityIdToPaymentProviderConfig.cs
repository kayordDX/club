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
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_payment_provider_config_provider_key;");

            migrationBuilder.Sql("ALTER TABLE payment_provider_config ADD COLUMN IF NOT EXISTS facility_id integer NOT NULL DEFAULT 0;");

            migrationBuilder.Sql("ALTER TABLE payment_provider_config ADD COLUMN IF NOT EXISTS type character varying(50) NOT NULL DEFAULT '';");

            // Remove existing global config rows that have no valid facility association.
            // They will be re-created per-facility by the seeder after migration.
            migrationBuilder.Sql("DELETE FROM payment_provider_config;");

            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX IF NOT EXISTS ix_payment_provider_config_facility_id_provider_key
                ON payment_provider_config (facility_id, provider_key);
                """);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint
                        WHERE conname = 'fk_payment_provider_config_facility_facility_id'
                    ) THEN
                        ALTER TABLE payment_provider_config
                        ADD CONSTRAINT fk_payment_provider_config_facility_facility_id
                        FOREIGN KEY (facility_id) REFERENCES facility (id) ON DELETE CASCADE;
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE payment_provider_config
                DROP CONSTRAINT IF EXISTS fk_payment_provider_config_facility_facility_id;
                """);

            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_payment_provider_config_facility_id_provider_key;");

            migrationBuilder.DropColumn(
                name: "facility_id",
                table: "payment_provider_config");

            migrationBuilder.DropColumn(
                name: "type",
                table: "payment_provider_config");

            migrationBuilder.CreateIndex(
                name: "ix_payment_provider_config_provider_key",
                table: "payment_provider_config",
                column: "provider_key",
                unique: true);
        }
    }
}
