using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.SqLite.Migrations.TenantDb;

public partial class Initial : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "MultiTenancy");

        migrationBuilder.CreateTable(
            name: "Tenants",
            schema: "MultiTenancy",
            columns: table => new
            {
                Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                Identifier = table.Column<string>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", nullable: false),
                ConnectionString = table.Column<string>(type: "TEXT", nullable: false),
                AdminEmail = table.Column<string>(type: "TEXT", nullable: false),
                IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                ValidUpto = table.Column<DateTime>(type: "TEXT", nullable: false),
                Issuer = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Tenants", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Tenants_Identifier",
            schema: "MultiTenancy",
            table: "Tenants",
            column: "Identifier",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Tenants",
            schema: "MultiTenancy");
    }
}
