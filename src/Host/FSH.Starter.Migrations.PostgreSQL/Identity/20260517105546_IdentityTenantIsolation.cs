using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Identity
{
    /// <inheritdoc />
    public partial class IdentityTenantIsolation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "identity",
                table: "UserSessions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "identity",
                table: "PasswordHistory",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "identity",
                table: "UserSessions");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "identity",
                table: "PasswordHistory");
        }
    }
}
