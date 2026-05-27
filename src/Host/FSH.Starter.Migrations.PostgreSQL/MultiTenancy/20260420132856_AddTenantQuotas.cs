using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.MultiTenancy
{
    /// <inheritdoc />
    public partial class AddTenantQuotas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Plan",
                schema: "tenant",
                table: "Tenants",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QuotaLimits",
                schema: "tenant",
                table: "Tenants",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Plan",
                schema: "tenant",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "QuotaLimits",
                schema: "tenant",
                table: "Tenants");
        }
    }
}
