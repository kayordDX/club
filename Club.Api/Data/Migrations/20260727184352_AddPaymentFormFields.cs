using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Club.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentFormFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "form_action_url", table: "payment", type: "character varying(1000)", maxLength: 1000, nullable: true);

            migrationBuilder.AddColumn<string>(name: "form_fields_json", table: "payment", type: "text", nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "form_action_url", table: "payment");

            migrationBuilder.DropColumn(name: "form_fields_json", table: "payment");
        }
    }
}
