using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Online.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameOpenIddictTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_open_iddict_authorizations_open_iddict_applications_application",
                table: "OpenIddictAuthorizations");

            migrationBuilder.DropForeignKey(
                name: "fk_open_iddict_tokens_open_iddict_applications_application_id",
                table: "OpenIddictTokens");

            migrationBuilder.DropForeignKey(
                name: "fk_open_iddict_tokens_open_iddict_authorizations_authorization_id",
                table: "OpenIddictTokens");

            migrationBuilder.DropPrimaryKey(
                name: "pk_open_iddict_tokens",
                table: "OpenIddictTokens");

            migrationBuilder.DropPrimaryKey(
                name: "pk_open_iddict_scopes",
                table: "OpenIddictScopes");

            migrationBuilder.DropPrimaryKey(
                name: "pk_open_iddict_authorizations",
                table: "OpenIddictAuthorizations");

            migrationBuilder.DropPrimaryKey(
                name: "pk_open_iddict_applications",
                table: "OpenIddictApplications");

            migrationBuilder.RenameTable(
                name: "OpenIddictTokens",
                newName: "openiddict_token");

            migrationBuilder.RenameTable(
                name: "OpenIddictScopes",
                newName: "openiddict_scope");

            migrationBuilder.RenameTable(
                name: "OpenIddictAuthorizations",
                newName: "openiddict_authorization");

            migrationBuilder.RenameTable(
                name: "OpenIddictApplications",
                newName: "openiddict_application");

            migrationBuilder.RenameIndex(
                name: "ix_open_iddict_tokens_reference_id",
                table: "openiddict_token",
                newName: "ix_openiddict_token_reference_id");

            migrationBuilder.RenameIndex(
                name: "ix_open_iddict_tokens_authorization_id",
                table: "openiddict_token",
                newName: "ix_openiddict_token_authorization_id");

            migrationBuilder.RenameIndex(
                name: "ix_open_iddict_tokens_application_id_status_subject_type",
                table: "openiddict_token",
                newName: "ix_openiddict_token_application_id_status_subject_type");

            migrationBuilder.RenameIndex(
                name: "ix_open_iddict_scopes_name",
                table: "openiddict_scope",
                newName: "ix_openiddict_scope_name");

            migrationBuilder.RenameIndex(
                name: "ix_open_iddict_authorizations_application_id_status_subject_type",
                table: "openiddict_authorization",
                newName: "ix_openiddict_authorization_application_id_status_subject_type");

            migrationBuilder.RenameIndex(
                name: "ix_open_iddict_applications_client_id",
                table: "openiddict_application",
                newName: "ix_openiddict_application_client_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_openiddict_token",
                table: "openiddict_token",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_openiddict_scope",
                table: "openiddict_scope",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_openiddict_authorization",
                table: "openiddict_authorization",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_openiddict_application",
                table: "openiddict_application",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_openiddict_authorization_openiddict_application_application",
                table: "openiddict_authorization",
                column: "application_id",
                principalTable: "openiddict_application",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_openiddict_token_openiddict_application_application_id",
                table: "openiddict_token",
                column: "application_id",
                principalTable: "openiddict_application",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_openiddict_token_openiddict_authorization_authorization_id",
                table: "openiddict_token",
                column: "authorization_id",
                principalTable: "openiddict_authorization",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_openiddict_authorization_openiddict_application_application",
                table: "openiddict_authorization");

            migrationBuilder.DropForeignKey(
                name: "fk_openiddict_token_openiddict_application_application_id",
                table: "openiddict_token");

            migrationBuilder.DropForeignKey(
                name: "fk_openiddict_token_openiddict_authorization_authorization_id",
                table: "openiddict_token");

            migrationBuilder.DropPrimaryKey(
                name: "pk_openiddict_token",
                table: "openiddict_token");

            migrationBuilder.DropPrimaryKey(
                name: "pk_openiddict_scope",
                table: "openiddict_scope");

            migrationBuilder.DropPrimaryKey(
                name: "pk_openiddict_authorization",
                table: "openiddict_authorization");

            migrationBuilder.DropPrimaryKey(
                name: "pk_openiddict_application",
                table: "openiddict_application");

            migrationBuilder.RenameTable(
                name: "openiddict_token",
                newName: "OpenIddictTokens");

            migrationBuilder.RenameTable(
                name: "openiddict_scope",
                newName: "OpenIddictScopes");

            migrationBuilder.RenameTable(
                name: "openiddict_authorization",
                newName: "OpenIddictAuthorizations");

            migrationBuilder.RenameTable(
                name: "openiddict_application",
                newName: "OpenIddictApplications");

            migrationBuilder.RenameIndex(
                name: "ix_openiddict_token_reference_id",
                table: "OpenIddictTokens",
                newName: "ix_open_iddict_tokens_reference_id");

            migrationBuilder.RenameIndex(
                name: "ix_openiddict_token_authorization_id",
                table: "OpenIddictTokens",
                newName: "ix_open_iddict_tokens_authorization_id");

            migrationBuilder.RenameIndex(
                name: "ix_openiddict_token_application_id_status_subject_type",
                table: "OpenIddictTokens",
                newName: "ix_open_iddict_tokens_application_id_status_subject_type");

            migrationBuilder.RenameIndex(
                name: "ix_openiddict_scope_name",
                table: "OpenIddictScopes",
                newName: "ix_open_iddict_scopes_name");

            migrationBuilder.RenameIndex(
                name: "ix_openiddict_authorization_application_id_status_subject_type",
                table: "OpenIddictAuthorizations",
                newName: "ix_open_iddict_authorizations_application_id_status_subject_type");

            migrationBuilder.RenameIndex(
                name: "ix_openiddict_application_client_id",
                table: "OpenIddictApplications",
                newName: "ix_open_iddict_applications_client_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_open_iddict_tokens",
                table: "OpenIddictTokens",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_open_iddict_scopes",
                table: "OpenIddictScopes",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_open_iddict_authorizations",
                table: "OpenIddictAuthorizations",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_open_iddict_applications",
                table: "OpenIddictApplications",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_open_iddict_authorizations_open_iddict_applications_application",
                table: "OpenIddictAuthorizations",
                column: "application_id",
                principalTable: "OpenIddictApplications",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_open_iddict_tokens_open_iddict_applications_application_id",
                table: "OpenIddictTokens",
                column: "application_id",
                principalTable: "OpenIddictApplications",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_open_iddict_tokens_open_iddict_authorizations_authorization_id",
                table: "OpenIddictTokens",
                column: "authorization_id",
                principalTable: "OpenIddictAuthorizations",
                principalColumn: "id");
        }
    }
}
