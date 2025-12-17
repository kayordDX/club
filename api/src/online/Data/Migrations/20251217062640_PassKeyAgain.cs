using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Online.Data.Migrations
{
    /// <inheritdoc />
    public partial class PassKeyAgain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_asp_net_user_passkeys_asp_net_users_user_id",
                table: "AspNetUserPasskeys");

            migrationBuilder.DropPrimaryKey(
                name: "pk_asp_net_user_passkeys",
                table: "AspNetUserPasskeys");

            migrationBuilder.RenameTable(
                name: "AspNetUserPasskeys",
                newName: "user_passkey");

            migrationBuilder.RenameIndex(
                name: "ix_asp_net_user_passkeys_user_id",
                table: "user_passkey",
                newName: "ix_user_passkey_user_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_user_passkey",
                table: "user_passkey",
                column: "credential_id");

            migrationBuilder.AddForeignKey(
                name: "fk_user_passkey_asp_net_users_user_id",
                table: "user_passkey",
                column: "user_id",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_user_passkey_asp_net_users_user_id",
                table: "user_passkey");

            migrationBuilder.DropPrimaryKey(
                name: "pk_user_passkey",
                table: "user_passkey");

            migrationBuilder.RenameTable(
                name: "user_passkey",
                newName: "AspNetUserPasskeys");

            migrationBuilder.RenameIndex(
                name: "ix_user_passkey_user_id",
                table: "AspNetUserPasskeys",
                newName: "ix_asp_net_user_passkeys_user_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_asp_net_user_passkeys",
                table: "AspNetUserPasskeys",
                column: "credential_id");

            migrationBuilder.AddForeignKey(
                name: "fk_asp_net_user_passkeys_asp_net_users_user_id",
                table: "AspNetUserPasskeys",
                column: "user_id",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
