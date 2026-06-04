using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.MultiTenancy
{
    /// <inheritdoc />
    public partial class AddTenantExpiryNotices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenantExpiryNotices",
                schema: "tenant",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    NoticeType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ValidUptoUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantExpiryNotices", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ux_tenant_expiry_notices_tenant_type_validupto",
                schema: "tenant",
                table: "TenantExpiryNotices",
                columns: new[] { "TenantId", "NoticeType", "ValidUptoUtc" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantExpiryNotices",
                schema: "tenant");
        }
    }
}
