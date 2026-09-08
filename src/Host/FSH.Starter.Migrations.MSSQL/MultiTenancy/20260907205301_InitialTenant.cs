using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.MSSQL.MultiTenancy
{
    /// <inheritdoc />
    public partial class InitialTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "tenant");

            migrationBuilder.CreateTable(
                name: "TenantExpiryNotices",
                schema: "tenant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    NoticeType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ValidUptoUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantExpiryNotices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantProvisionings",
                schema: "tenant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CurrentStep = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JobId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantProvisionings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                schema: "tenant",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ConnectionString = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AdminEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ValidUpto = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Issuer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Plan = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    QuotaLimits = table.Column<string>(type: "json", nullable: false),
                    Identifier = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantThemes",
                schema: "tenant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PrimaryColor = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: false),
                    SecondaryColor = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: false),
                    TertiaryColor = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: false),
                    BackgroundColor = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: false),
                    SurfaceColor = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: false),
                    ErrorColor = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: false),
                    WarningColor = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: false),
                    SuccessColor = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: false),
                    InfoColor = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: false),
                    DarkPrimaryColor = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: false),
                    DarkSecondaryColor = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: false),
                    DarkTertiaryColor = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: false),
                    DarkBackgroundColor = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: false),
                    DarkSurfaceColor = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: false),
                    DarkErrorColor = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: false),
                    DarkWarningColor = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: false),
                    DarkSuccessColor = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: false),
                    DarkInfoColor = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: false),
                    LogoUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    LogoDarkUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    FaviconUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    FontFamily = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HeadingFontFamily = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FontSizeBase = table.Column<double>(type: "float", nullable: false),
                    LineHeightBase = table.Column<double>(type: "float", nullable: false),
                    BorderRadius = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DefaultElevation = table.Column<int>(type: "int", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LastModifiedOnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantThemes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantProvisioningSteps",
                schema: "tenant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProvisioningId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Step = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantProvisioningSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantProvisioningSteps_TenantProvisionings_ProvisioningId",
                        column: x => x.ProvisioningId,
                        principalSchema: "tenant",
                        principalTable: "TenantProvisionings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_tenant_expiry_notices_tenant_type_validupto",
                schema: "tenant",
                table: "TenantExpiryNotices",
                columns: new[] { "TenantId", "NoticeType", "ValidUptoUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantProvisioningSteps_ProvisioningId",
                schema: "tenant",
                table: "TenantProvisioningSteps",
                column: "ProvisioningId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Identifier",
                schema: "tenant",
                table: "Tenants",
                column: "Identifier",
                unique: true);

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
                name: "TenantExpiryNotices",
                schema: "tenant");

            migrationBuilder.DropTable(
                name: "TenantProvisioningSteps",
                schema: "tenant");

            migrationBuilder.DropTable(
                name: "Tenants",
                schema: "tenant");

            migrationBuilder.DropTable(
                name: "TenantThemes",
                schema: "tenant");

            migrationBuilder.DropTable(
                name: "TenantProvisionings",
                schema: "tenant");
        }
    }
}
