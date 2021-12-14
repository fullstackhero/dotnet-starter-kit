using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.Oracle.Migrations.Root;

public partial class initial : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Tenants",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                Name = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                Key = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                AdminEmail = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                ConnectionString = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                IsActive = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                ValidUpto = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                Issuer = table.Column<string>(type: "NVARCHAR2(256)", maxLength: 256, nullable: true),
                CreatedBy = table.Column<Guid>(type: "RAW(16)", nullable: false),
                CreatedOn = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                LastModifiedBy = table.Column<Guid>(type: "RAW(16)", nullable: false),
                LastModifiedOn = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                DeletedOn = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                DeletedBy = table.Column<Guid>(type: "RAW(16)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Tenants", x => x.Id);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Tenants");
    }
}
