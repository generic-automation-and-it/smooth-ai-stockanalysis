using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmoothAiStockAnalysis.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class SnakeCaseNamingConvention : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_users",
                table: "users");

            migrationBuilder.RenameTable(
                name: "users",
                newName: "user_record");

            migrationBuilder.RenameIndex(
                name: "ux_users_unique_identifier",
                table: "user_record",
                newName: "ix_user_record_unique_identifier");

            migrationBuilder.AddPrimaryKey(
                name: "pk_user_record",
                table: "user_record",
                column: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_user_record",
                table: "user_record");

            migrationBuilder.RenameTable(
                name: "user_record",
                newName: "users");

            migrationBuilder.RenameIndex(
                name: "ix_user_record_unique_identifier",
                table: "users",
                newName: "ux_users_unique_identifier");

            migrationBuilder.AddPrimaryKey(
                name: "pk_users",
                table: "users",
                column: "id");
        }
    }
}
