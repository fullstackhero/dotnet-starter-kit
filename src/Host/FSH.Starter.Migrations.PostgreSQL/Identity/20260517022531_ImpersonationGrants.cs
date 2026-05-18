using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Identity
{
    /// <inheritdoc />
    public partial class ImpersonationGrants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImpersonationGrants",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Jti = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ActorUserId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ActorUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ActorTenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ImpersonatedUserId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ImpersonatedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ImpersonatedTenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedByUserId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    RevokedByUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    RevokeReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ClientId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImpersonationGrants", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImpersonationGrants_ActorUserId_StartedAtUtc",
                schema: "identity",
                table: "ImpersonationGrants",
                columns: new[] { "ActorUserId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ImpersonationGrants_ImpersonatedTenantId_StartedAtUtc",
                schema: "identity",
                table: "ImpersonationGrants",
                columns: new[] { "ImpersonatedTenantId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ImpersonationGrants_Jti",
                schema: "identity",
                table: "ImpersonationGrants",
                column: "Jti",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImpersonationGrants",
                schema: "identity");
        }
    }
}
