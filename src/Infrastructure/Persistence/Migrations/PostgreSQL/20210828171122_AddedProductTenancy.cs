using Microsoft.EntityFrameworkCore.Migrations;

namespace DN.WebApi.Infrastructure.Persistence.Migrations.PostgreSQL
{
    public partial class AddedProductTenancy : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MiddleName",
                schema: "Identity",
                table: "Users");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "Products",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Products");

            migrationBuilder.AddColumn<string>(
                name: "MiddleName",
                schema: "Identity",
                table: "Users",
                type: "text",
                nullable: true);
        }
    }
}