using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Club.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingFacility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(name: "facility_id", table: "booking", type: "integer", nullable: true);

            // Use surviving slot/extra links only when they identify a single facility.
            // Bookings with no evidence (or multiple facilities) remain unknown.
            migrationBuilder.Sql(
                """
                WITH booking_facilities AS (
                    SELECT scb.booking_id, s.facility_id
                    FROM slot_contract_booking scb
                    JOIN slot_contract sc ON sc.id = scb.slot_contract_id
                    JOIN slot s ON s.id = sc.slot_id
                    UNION
                    SELECT eb.booking_id, e.facility_id
                    FROM extra_booking eb
                    JOIN extra e ON e.id = eb.extra_id
                ), resolved AS (
                    SELECT booking_id, MIN(facility_id) AS facility_id
                    FROM booking_facilities
                    GROUP BY booking_id
                    HAVING COUNT(DISTINCT facility_id) = 1
                )
                UPDATE booking b
                SET facility_id = resolved.facility_id
                FROM resolved
                WHERE b.id = resolved.booking_id;
                """
            );

            migrationBuilder.CreateIndex(name: "ix_booking_facility_id", table: "booking", column: "facility_id");

            migrationBuilder.AddForeignKey(
                name: "fk_booking_facility_facility_id",
                table: "booking",
                column: "facility_id",
                principalTable: "facility",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "fk_booking_facility_facility_id", table: "booking");

            migrationBuilder.DropIndex(name: "ix_booking_facility_id", table: "booking");

            migrationBuilder.DropColumn(name: "facility_id", table: "booking");
        }
    }
}
