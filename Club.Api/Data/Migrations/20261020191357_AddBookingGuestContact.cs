using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Club.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingGuestContact : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "guest_email", table: "booking", type: "character varying(256)", maxLength: 256, nullable: true);

            migrationBuilder.AddColumn<string>(name: "guest_name", table: "booking", type: "character varying(200)", maxLength: 200, nullable: true);

            migrationBuilder.AddColumn<string>(name: "guest_phone", table: "booking", type: "character varying(50)", maxLength: 50, nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "guest_email", table: "booking");

            migrationBuilder.DropColumn(name: "guest_name", table: "booking");

            migrationBuilder.DropColumn(name: "guest_phone", table: "booking");
        }
    }
}
