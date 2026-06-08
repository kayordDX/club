using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Club.Data.Migrations
{
    /// <inheritdoc />
    public partial class FacilityRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_user_role_outlet_outlet_id",
                table: "user_role");

            migrationBuilder.RenameColumn(
                name: "outlet_id",
                table: "user_role",
                newName: "facility_id");

            migrationBuilder.RenameIndex(
                name: "ix_user_role_user_id_role_id_outlet_id",
                table: "user_role",
                newName: "ix_user_role_user_id_role_id_facility_id");

            migrationBuilder.RenameIndex(
                name: "ix_user_role_outlet_id",
                table: "user_role",
                newName: "ix_user_role_facility_id");

            migrationBuilder.AddForeignKey(
                name: "fk_user_role_facility_facility_id",
                table: "user_role",
                column: "facility_id",
                principalTable: "facility",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_user_role_facility_facility_id",
                table: "user_role");

            migrationBuilder.RenameColumn(
                name: "facility_id",
                table: "user_role",
                newName: "outlet_id");

            migrationBuilder.RenameIndex(
                name: "ix_user_role_user_id_role_id_facility_id",
                table: "user_role",
                newName: "ix_user_role_user_id_role_id_outlet_id");

            migrationBuilder.RenameIndex(
                name: "ix_user_role_facility_id",
                table: "user_role",
                newName: "ix_user_role_outlet_id");

            migrationBuilder.AddForeignKey(
                name: "fk_user_role_outlet_outlet_id",
                table: "user_role",
                column: "outlet_id",
                principalTable: "outlet",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
