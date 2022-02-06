using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.Oracle.Migrations.Tenant
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "MULTITENANCY");

            migrationBuilder.CreateTable(
                name: "Tenants",
                schema: "MULTITENANCY",
                columns: table => new
                {
                    Id = table.Column<string>(type: "NVARCHAR2(64)", maxLength: 64, nullable: false),
                    Identifier = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                    Name = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    ConnectionString = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    AdminEmail = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    IsActive = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    ValidUpto = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    Issuer = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Identifier",
                schema: "MULTITENANCY",
                table: "Tenants",
                column: "Identifier",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tenants",
                schema: "MULTITENANCY");
        }
    }
}
