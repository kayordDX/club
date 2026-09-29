using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Club.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260916100000_AddVoucherPaymentType")]
public class AddVoucherPaymentType : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            INSERT INTO payment_type (id, name) VALUES (4, 'Voucher')
            ON CONFLICT (id) DO UPDATE SET name = EXCLUDED.name;
            SELECT setval(pg_get_serial_sequence('payment_type', 'id'), (SELECT MAX(id) FROM payment_type));
            """
        );
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DELETE FROM payment_type WHERE id = 4 AND name = 'Voucher';");
    }
}
