using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Playground.Migrations.PostgreSQL.MultiTenancy
{
    /// <inheritdoc />
    public partial class AddTenantTheme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenantThemes",
                schema: "tenant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PrimaryColor = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    SecondaryColor = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    TertiaryColor = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    BackgroundColor = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    SurfaceColor = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    ErrorColor = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    WarningColor = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    SuccessColor = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    InfoColor = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    DarkPrimaryColor = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    DarkSecondaryColor = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    DarkTertiaryColor = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    DarkBackgroundColor = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    DarkSurfaceColor = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    DarkErrorColor = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    DarkWarningColor = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    DarkSuccessColor = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    DarkInfoColor = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    LogoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LogoDarkUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FaviconUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FontFamily = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    HeadingFontFamily = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FontSizeBase = table.Column<double>(type: "double precision", nullable: false),
                    LineHeightBase = table.Column<double>(type: "double precision", nullable: false),
                    BorderRadius = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DefaultElevation = table.Column<int>(type: "integer", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    LastModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantThemes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantThemes_TenantId",
                schema: "tenant",
                table: "TenantThemes",
                column: "TenantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantThemes",
                schema: "tenant");
        }
    }
}
