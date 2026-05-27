using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Webhooks
{
    /// <inheritdoc />
    public partial class InitialWebhooks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "webhooks");

            migrationBuilder.CreateTable(
                name: "Deliveries",
                schema: "webhooks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    HttpStatusCode = table.Column<int>(type: "integer", nullable: false),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    AttemptedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Deliveries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                schema: "webhooks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    EventsCsv = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                    SecretHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Deliveries_AttemptedAtUtc",
                schema: "webhooks",
                table: "Deliveries",
                column: "AttemptedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Deliveries_SubscriptionId",
                schema: "webhooks",
                table: "Deliveries",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_IsActive",
                schema: "webhooks",
                table: "Subscriptions",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Deliveries",
                schema: "webhooks");

            migrationBuilder.DropTable(
                name: "Subscriptions",
                schema: "webhooks");
        }
    }
}
