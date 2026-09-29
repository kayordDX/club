using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Club.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260916101000_AddPartialPaymentStatus")]
public class AddPartialPaymentStatus : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            INSERT INTO payment_status (id, name) VALUES (5, 'Partial')
            ON CONFLICT (id) DO UPDATE SET name = EXCLUDED.name;
            SELECT setval(pg_get_serial_sequence('payment_status', 'id'), (SELECT MAX(id) FROM payment_status));
            """
        );
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DELETE FROM payment_status WHERE id = 5 AND name = 'Partial';");
    }
}
