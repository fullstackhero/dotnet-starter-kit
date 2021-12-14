using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.Oracle.Migrations.Application;

public partial class initial : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "IDENTITY");

        migrationBuilder.CreateTable(
            name: "AuditTrails",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                UserId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                Type = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                TableName = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                DateTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                OldValues = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                NewValues = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                AffectedColumns = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                PrimaryKey = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AuditTrails", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Brands",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                Name = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                Description = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                Tenant = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                CreatedBy = table.Column<Guid>(type: "RAW(16)", nullable: false),
                CreatedOn = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                LastModifiedBy = table.Column<Guid>(type: "RAW(16)", nullable: false),
                LastModifiedOn = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                DeletedOn = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                DeletedBy = table.Column<Guid>(type: "RAW(16)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Brands", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Roles",
            schema: "IDENTITY",
            columns: table => new
            {
                Id = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                Description = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                Tenant = table.Column<string>(type: "NVARCHAR2(450)", nullable: true),
                Name = table.Column<string>(type: "NVARCHAR2(256)", maxLength: 256, nullable: true),
                NormalizedName = table.Column<string>(type: "NVARCHAR2(256)", maxLength: 256, nullable: true),
                ConcurrencyStamp = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Roles", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Users",
            schema: "IDENTITY",
            columns: table => new
            {
                Id = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                FirstName = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                LastName = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                ImageUrl = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                IsActive = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                RefreshToken = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                RefreshTokenExpiryTime = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                Tenant = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                ObjectId = table.Column<string>(type: "NVARCHAR2(256)", maxLength: 256, nullable: true),
                UserName = table.Column<string>(type: "NVARCHAR2(256)", maxLength: 256, nullable: true),
                NormalizedUserName = table.Column<string>(type: "NVARCHAR2(256)", maxLength: 256, nullable: true),
                Email = table.Column<string>(type: "NVARCHAR2(256)", maxLength: 256, nullable: true),
                NormalizedEmail = table.Column<string>(type: "NVARCHAR2(256)", maxLength: 256, nullable: true),
                EmailConfirmed = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                PasswordHash = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                SecurityStamp = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                ConcurrencyStamp = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                PhoneNumber = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                PhoneNumberConfirmed = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                TwoFactorEnabled = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                LockoutEnd = table.Column<DateTimeOffset>(type: "TIMESTAMP(7) WITH TIME ZONE", nullable: true),
                LockoutEnabled = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                AccessFailedCount = table.Column<int>(type: "NUMBER(10)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Products",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                Name = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                Description = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                Rate = table.Column<decimal>(type: "DECIMAL(18, 2)", nullable: false),
                Tenant = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                ImagePath = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                BrandId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                CreatedBy = table.Column<Guid>(type: "RAW(16)", nullable: false),
                CreatedOn = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                LastModifiedBy = table.Column<Guid>(type: "RAW(16)", nullable: false),
                LastModifiedOn = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                DeletedOn = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                DeletedBy = table.Column<Guid>(type: "RAW(16)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Products", x => x.Id);
                table.ForeignKey(
                    name: "FK_Products_Brands_BrandId",
                    column: x => x.BrandId,
                    principalTable: "Brands",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "RoleClaims",
            schema: "IDENTITY",
            columns: table => new
            {
                Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                    .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                Description = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                Tenant = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                Group = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                CreatedBy = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                CreatedOn = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                LastModifiedBy = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                LastModifiedOn = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                RoleId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                ClaimType = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                ClaimValue = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RoleClaims", x => x.Id);
                table.ForeignKey(
                    name: "FK_RoleClaims_Roles_RoleId",
                    column: x => x.RoleId,
                    principalSchema: "IDENTITY",
                    principalTable: "Roles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "UserClaims",
            schema: "IDENTITY",
            columns: table => new
            {
                Id = table.Column<int>(type: "NUMBER(10)", nullable: false)
                    .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                UserId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                ClaimType = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                ClaimValue = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserClaims", x => x.Id);
                table.ForeignKey(
                    name: "FK_UserClaims_Users_UserId",
                    column: x => x.UserId,
                    principalSchema: "IDENTITY",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "UserLogins",
            schema: "IDENTITY",
            columns: table => new
            {
                LoginProvider = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                ProviderKey = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                ProviderDisplayName = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                UserId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserLogins", x => new { x.LoginProvider, x.ProviderKey });
                table.ForeignKey(
                    name: "FK_UserLogins_Users_UserId",
                    column: x => x.UserId,
                    principalSchema: "IDENTITY",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "UserRoles",
            schema: "IDENTITY",
            columns: table => new
            {
                UserId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                RoleId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                table.ForeignKey(
                    name: "FK_UserRoles_Roles_RoleId",
                    column: x => x.RoleId,
                    principalSchema: "IDENTITY",
                    principalTable: "Roles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_UserRoles_Users_UserId",
                    column: x => x.UserId,
                    principalSchema: "IDENTITY",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "UserTokens",
            schema: "IDENTITY",
            columns: table => new
            {
                UserId = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                LoginProvider = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                Name = table.Column<string>(type: "NVARCHAR2(450)", nullable: false),
                Value = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                table.ForeignKey(
                    name: "FK_UserTokens_Users_UserId",
                    column: x => x.UserId,
                    principalSchema: "IDENTITY",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Products_BrandId",
            table: "Products",
            column: "BrandId");

        migrationBuilder.CreateIndex(
            name: "IX_RoleClaims_RoleId",
            schema: "IDENTITY",
            table: "RoleClaims",
            column: "RoleId");

        migrationBuilder.CreateIndex(
            name: "RoleNameIndex",
            schema: "IDENTITY",
            table: "Roles",
            columns: new[] { "NormalizedName", "Tenant" },
            unique: true,
            filter: "\"NormalizedName\" IS NOT NULL AND \"Tenant\" IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_UserClaims_UserId",
            schema: "IDENTITY",
            table: "UserClaims",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_UserLogins_UserId",
            schema: "IDENTITY",
            table: "UserLogins",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_UserRoles_RoleId",
            schema: "IDENTITY",
            table: "UserRoles",
            column: "RoleId");

        migrationBuilder.CreateIndex(
            name: "EmailIndex",
            schema: "IDENTITY",
            table: "Users",
            column: "NormalizedEmail");

        migrationBuilder.CreateIndex(
            name: "UserNameIndex",
            schema: "IDENTITY",
            table: "Users",
            column: "NormalizedUserName",
            unique: true,
            filter: "\"NormalizedUserName\" IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "AuditTrails");

        migrationBuilder.DropTable(
            name: "Products");

        migrationBuilder.DropTable(
            name: "RoleClaims",
            schema: "IDENTITY");

        migrationBuilder.DropTable(
            name: "UserClaims",
            schema: "IDENTITY");

        migrationBuilder.DropTable(
            name: "UserLogins",
            schema: "IDENTITY");

        migrationBuilder.DropTable(
            name: "UserRoles",
            schema: "IDENTITY");

        migrationBuilder.DropTable(
            name: "UserTokens",
            schema: "IDENTITY");

        migrationBuilder.DropTable(
            name: "Brands");

        migrationBuilder.DropTable(
            name: "Roles",
            schema: "IDENTITY");

        migrationBuilder.DropTable(
            name: "Users",
            schema: "IDENTITY");
    }
}
