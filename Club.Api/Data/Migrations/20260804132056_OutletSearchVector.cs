using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace Club.Data.Migrations
{
    /// <inheritdoc />
    public partial class OutletSearchVector : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "search_vector",
                table: "outlet",
                type: "tsvector",
                nullable: false,
                computedColumnSql: "to_tsvector('english', coalesce(name, '') || ' ' || coalesce(display_name, '') || ' ' || coalesce(description, '') || ' ' || coalesce(address, '') || ' ' || coalesce(tags, ''))",
                stored: true
            );

            migrationBuilder.CreateIndex(name: "ix_outlet_search_vector", table: "outlet", column: "search_vector").Annotation("Npgsql:IndexMethod", "gin");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "ix_outlet_search_vector", table: "outlet");

            migrationBuilder.DropColumn(name: "search_vector", table: "outlet");
        }
    }
}
