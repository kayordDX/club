using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Online.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOutletToUserRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_user_role",
                table: "user_role");

            migrationBuilder.AddColumn<int>(
                name: "outlet_id",
                table: "user_role",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "pk_user_role",
                table: "user_role",
                columns: new[] { "user_id", "role_id", "outlet_id" });

            migrationBuilder.CreateIndex(
                name: "ix_user_role_outlet_id",
                table: "user_role",
                column: "outlet_id");

            migrationBuilder.AddForeignKey(
                name: "fk_user_role_outlet_outlet_id",
                table: "user_role",
                column: "outlet_id",
                principalTable: "outlet",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_user_role_outlet_outlet_id",
                table: "user_role");

            migrationBuilder.DropPrimaryKey(
                name: "pk_user_role",
                table: "user_role");

            migrationBuilder.DropIndex(
                name: "ix_user_role_outlet_id",
                table: "user_role");

            migrationBuilder.DropColumn(
                name: "outlet_id",
                table: "user_role");

            migrationBuilder.AddPrimaryKey(
                name: "pk_user_role",
                table: "user_role",
                columns: new[] { "user_id", "role_id" });
        }
    }
}
