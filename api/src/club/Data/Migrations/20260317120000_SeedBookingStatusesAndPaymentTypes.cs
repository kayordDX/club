using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Online.Data.Migrations;

public partial class SeedBookingStatusesAndPaymentTypes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            INSERT INTO booking_status (id, name)
            VALUES (1, 'Pending')
            ON CONFLICT (id) DO UPDATE SET name = EXCLUDED.name;

            INSERT INTO booking_status (id, name)
            VALUES (2, 'Confirmed')
            ON CONFLICT (id) DO UPDATE SET name = EXCLUDED.name;

            INSERT INTO booking_status (id, name)
            VALUES (3, 'Cancelled')
            ON CONFLICT (id) DO UPDATE SET name = EXCLUDED.name;

            SELECT setval('booking_status_id_seq', 4, false);

            INSERT INTO payment_type (name)
            SELECT 'Pay on arrival'
            WHERE NOT EXISTS (SELECT 1 FROM payment_type WHERE name = 'Pay on arrival');

            INSERT INTO payment_type (name)
            SELECT 'Credit card'
            WHERE NOT EXISTS (SELECT 1 FROM payment_type WHERE name = 'Credit card');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DELETE FROM booking_status
            WHERE name IN ('Pending', 'Confirmed', 'Cancelled');

            DELETE FROM payment_type
            WHERE name IN ('Pay on arrival', 'Credit card');
            """);
    }
}
