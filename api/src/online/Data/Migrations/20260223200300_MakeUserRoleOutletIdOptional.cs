using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Online.Data.Migrations
{
    /// <inheritdoc />
    public partial class MakeUserRoleOutletIdOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_user_role_outlet_outlet_id",
                table: "user_role");

            migrationBuilder.DropPrimaryKey(
                name: "pk_user_role",
                table: "user_role");

            migrationBuilder.AlterColumn<int>(
                name: "outlet_id",
                table: "user_role",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "id",
                table: "user_role",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "pk_user_role",
                table: "user_role",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "ix_user_role_user_id_role_id_outlet_id",
                table: "user_role",
                columns: new[] { "user_id", "role_id", "outlet_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_user_role_outlet_outlet_id",
                table: "user_role",
                column: "outlet_id",
                principalTable: "outlet",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
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
                name: "ix_user_role_user_id_role_id_outlet_id",
                table: "user_role");

            migrationBuilder.DropColumn(
                name: "id",
                table: "user_role");

            migrationBuilder.AlterColumn<int>(
                name: "outlet_id",
                table: "user_role",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "pk_user_role",
                table: "user_role",
                columns: new[] { "user_id", "role_id", "outlet_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_user_role_outlet_outlet_id",
                table: "user_role",
                column: "outlet_id",
                principalTable: "outlet",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
