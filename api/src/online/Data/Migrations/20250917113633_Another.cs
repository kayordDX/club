using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Online.Data.Migrations
{
    /// <inheritdoc />
    public partial class Another : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_claim_user_user_id",
                table: "claim");

            migrationBuilder.DropForeignKey(
                name: "fk_user_login_user_user_id",
                table: "user_login");

            migrationBuilder.DropForeignKey(
                name: "fk_user_role_user_user_id",
                table: "user_role");

            migrationBuilder.DropForeignKey(
                name: "fk_user_token_user_user_id",
                table: "user_token");

            migrationBuilder.AddForeignKey(
                name: "fk_claim_asp_net_users_user_id",
                table: "claim",
                column: "user_id",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_user_login_asp_net_users_user_id",
                table: "user_login",
                column: "user_id",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_user_role_asp_net_users_user_id",
                table: "user_role",
                column: "user_id",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_user_token_asp_net_users_user_id",
                table: "user_token",
                column: "user_id",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_claim_asp_net_users_user_id",
                table: "claim");

            migrationBuilder.DropForeignKey(
                name: "fk_user_login_asp_net_users_user_id",
                table: "user_login");

            migrationBuilder.DropForeignKey(
                name: "fk_user_role_asp_net_users_user_id",
                table: "user_role");

            migrationBuilder.DropForeignKey(
                name: "fk_user_token_asp_net_users_user_id",
                table: "user_token");

            migrationBuilder.AddForeignKey(
                name: "fk_claim_user_user_id",
                table: "claim",
                column: "user_id",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_user_login_user_user_id",
                table: "user_login",
                column: "user_id",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_user_role_user_user_id",
                table: "user_role",
                column: "user_id",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_user_token_user_user_id",
                table: "user_token",
                column: "user_id",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
