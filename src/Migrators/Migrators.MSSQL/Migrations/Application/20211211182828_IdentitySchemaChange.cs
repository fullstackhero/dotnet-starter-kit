using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.MSSQL.Migrations.Application;

public partial class IdentitySchemaChange : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "IDENTITY");

        migrationBuilder.RenameTable(
            name: "UserTokens",
            schema: "Identity",
            newName: "UserTokens",
            newSchema: "IDENTITY");

        migrationBuilder.RenameTable(
            name: "Users",
            schema: "Identity",
            newName: "Users",
            newSchema: "IDENTITY");

        migrationBuilder.RenameTable(
            name: "UserRoles",
            schema: "Identity",
            newName: "UserRoles",
            newSchema: "IDENTITY");

        migrationBuilder.RenameTable(
            name: "UserLogins",
            schema: "Identity",
            newName: "UserLogins",
            newSchema: "IDENTITY");

        migrationBuilder.RenameTable(
            name: "UserClaims",
            schema: "Identity",
            newName: "UserClaims",
            newSchema: "IDENTITY");

        migrationBuilder.RenameTable(
            name: "Roles",
            schema: "Identity",
            newName: "Roles",
            newSchema: "IDENTITY");

        migrationBuilder.RenameTable(
            name: "RoleClaims",
            schema: "Identity",
            newName: "RoleClaims",
            newSchema: "IDENTITY");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "Identity");

        migrationBuilder.RenameTable(
            name: "UserTokens",
            schema: "IDENTITY",
            newName: "UserTokens",
            newSchema: "Identity");

        migrationBuilder.RenameTable(
            name: "Users",
            schema: "IDENTITY",
            newName: "Users",
            newSchema: "Identity");

        migrationBuilder.RenameTable(
            name: "UserRoles",
            schema: "IDENTITY",
            newName: "UserRoles",
            newSchema: "Identity");

        migrationBuilder.RenameTable(
            name: "UserLogins",
            schema: "IDENTITY",
            newName: "UserLogins",
            newSchema: "Identity");

        migrationBuilder.RenameTable(
            name: "UserClaims",
            schema: "IDENTITY",
            newName: "UserClaims",
            newSchema: "Identity");

        migrationBuilder.RenameTable(
            name: "Roles",
            schema: "IDENTITY",
            newName: "Roles",
            newSchema: "Identity");

        migrationBuilder.RenameTable(
            name: "RoleClaims",
            schema: "IDENTITY",
            newName: "RoleClaims",
            newSchema: "Identity");
    }
}
