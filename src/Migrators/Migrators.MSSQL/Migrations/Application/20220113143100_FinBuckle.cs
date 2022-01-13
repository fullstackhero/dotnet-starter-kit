using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.MSSQL.Migrations.Application;

public partial class FinBuckle : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "UserNameIndex",
            schema: "IDENTITY",
            table: "Users");

        migrationBuilder.DropPrimaryKey(
            name: "PK_UserLogins",
            schema: "IDENTITY",
            table: "UserLogins");

        migrationBuilder.DropIndex(
            name: "RoleNameIndex",
            schema: "IDENTITY",
            table: "Roles");

        migrationBuilder.DropColumn(
            name: "Tenant",
            schema: "IDENTITY",
            table: "Users");

        migrationBuilder.DropColumn(
            name: "Tenant",
            schema: "IDENTITY",
            table: "Roles");

        migrationBuilder.DropColumn(
            name: "Tenant",
            schema: "IDENTITY",
            table: "RoleClaims");

        migrationBuilder.DropColumn(
            name: "Tenant",
            table: "Products");

        migrationBuilder.DropColumn(
            name: "Tenant",
            table: "Brands");

        migrationBuilder.AddColumn<string>(
            name: "TenantId",
            schema: "IDENTITY",
            table: "UserTokens",
            type: "nvarchar(64)",
            maxLength: 64,
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<string>(
            name: "TenantId",
            schema: "IDENTITY",
            table: "Users",
            type: "nvarchar(64)",
            maxLength: 64,
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<string>(
            name: "TenantId",
            schema: "IDENTITY",
            table: "UserRoles",
            type: "nvarchar(64)",
            maxLength: 64,
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<string>(
            name: "Id",
            schema: "IDENTITY",
            table: "UserLogins",
            type: "nvarchar(450)",
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<string>(
            name: "TenantId",
            schema: "IDENTITY",
            table: "UserLogins",
            type: "nvarchar(64)",
            maxLength: 64,
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<string>(
            name: "TenantId",
            schema: "IDENTITY",
            table: "UserClaims",
            type: "nvarchar(64)",
            maxLength: 64,
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<string>(
            name: "TenantId",
            schema: "IDENTITY",
            table: "Roles",
            type: "nvarchar(64)",
            maxLength: 64,
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<string>(
            name: "TenantId",
            schema: "IDENTITY",
            table: "RoleClaims",
            type: "nvarchar(64)",
            maxLength: 64,
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<string>(
            name: "TenantId",
            table: "Products",
            type: "nvarchar(64)",
            maxLength: 64,
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddColumn<string>(
            name: "TenantId",
            table: "Brands",
            type: "nvarchar(64)",
            maxLength: 64,
            nullable: false,
            defaultValue: string.Empty);

        migrationBuilder.AddPrimaryKey(
            name: "PK_UserLogins",
            schema: "IDENTITY",
            table: "UserLogins",
            column: "Id");

        migrationBuilder.CreateIndex(
            name: "UserNameIndex",
            schema: "IDENTITY",
            table: "Users",
            columns: new[] { "NormalizedUserName", "TenantId" },
            unique: true,
            filter: "[NormalizedUserName] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_UserLogins_LoginProvider_ProviderKey_TenantId",
            schema: "IDENTITY",
            table: "UserLogins",
            columns: new[] { "LoginProvider", "ProviderKey", "TenantId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "RoleNameIndex",
            schema: "IDENTITY",
            table: "Roles",
            columns: new[] { "NormalizedName", "TenantId" },
            unique: true,
            filter: "[NormalizedName] IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "UserNameIndex",
            schema: "IDENTITY",
            table: "Users");

        migrationBuilder.DropPrimaryKey(
            name: "PK_UserLogins",
            schema: "IDENTITY",
            table: "UserLogins");

        migrationBuilder.DropIndex(
            name: "IX_UserLogins_LoginProvider_ProviderKey_TenantId",
            schema: "IDENTITY",
            table: "UserLogins");

        migrationBuilder.DropIndex(
            name: "RoleNameIndex",
            schema: "IDENTITY",
            table: "Roles");

        migrationBuilder.DropColumn(
            name: "TenantId",
            schema: "IDENTITY",
            table: "UserTokens");

        migrationBuilder.DropColumn(
            name: "TenantId",
            schema: "IDENTITY",
            table: "Users");

        migrationBuilder.DropColumn(
            name: "TenantId",
            schema: "IDENTITY",
            table: "UserRoles");

        migrationBuilder.DropColumn(
            name: "Id",
            schema: "IDENTITY",
            table: "UserLogins");

        migrationBuilder.DropColumn(
            name: "TenantId",
            schema: "IDENTITY",
            table: "UserLogins");

        migrationBuilder.DropColumn(
            name: "TenantId",
            schema: "IDENTITY",
            table: "UserClaims");

        migrationBuilder.DropColumn(
            name: "TenantId",
            schema: "IDENTITY",
            table: "Roles");

        migrationBuilder.DropColumn(
            name: "TenantId",
            schema: "IDENTITY",
            table: "RoleClaims");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "Products");

        migrationBuilder.DropColumn(
            name: "TenantId",
            table: "Brands");

        migrationBuilder.AddColumn<string>(
            name: "Tenant",
            schema: "IDENTITY",
            table: "Users",
            type: "nvarchar(max)",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Tenant",
            schema: "IDENTITY",
            table: "Roles",
            type: "nvarchar(450)",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Tenant",
            schema: "IDENTITY",
            table: "RoleClaims",
            type: "nvarchar(max)",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Tenant",
            table: "Products",
            type: "nvarchar(max)",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Tenant",
            table: "Brands",
            type: "nvarchar(max)",
            nullable: true);

        migrationBuilder.AddPrimaryKey(
            name: "PK_UserLogins",
            schema: "IDENTITY",
            table: "UserLogins",
            columns: new[] { "LoginProvider", "ProviderKey" });

        migrationBuilder.CreateIndex(
            name: "UserNameIndex",
            schema: "IDENTITY",
            table: "Users",
            column: "NormalizedUserName",
            unique: true,
            filter: "[NormalizedUserName] IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "RoleNameIndex",
            schema: "IDENTITY",
            table: "Roles",
            columns: new[] { "NormalizedName", "Tenant" },
            unique: true,
            filter: "[NormalizedName] IS NOT NULL AND [Tenant] IS NOT NULL");
    }
}
