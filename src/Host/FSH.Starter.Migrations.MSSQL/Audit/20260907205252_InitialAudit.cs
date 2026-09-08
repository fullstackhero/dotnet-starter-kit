using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.MSSQL.Audit
{
    /// <inheritdoc />
    public partial class InitialAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "audit");

            migrationBuilder.CreateTable(
                name: "AuditRecords",
                schema: "audit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    Severity = table.Column<byte>(type: "tinyint", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TraceId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    SpanId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    RequestId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tags = table.Column<long>(type: "bigint", nullable: false),
                    PayloadJson = table.Column<string>(type: "json", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditRecords_CorrelationId",
                schema: "audit",
                table: "AuditRecords",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditRecords_Tenant_EventType_OccurredAt",
                schema: "audit",
                table: "AuditRecords",
                columns: new[] { "TenantId", "EventType", "OccurredAtUtc" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_AuditRecords_Tenant_OccurredAt",
                schema: "audit",
                table: "AuditRecords",
                columns: new[] { "TenantId", "OccurredAtUtc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_AuditRecords_TraceId",
                schema: "audit",
                table: "AuditRecords",
                column: "TraceId");

            // Counterpart to the PostgreSQL jsonb_path_ops GIN index. EF Core has no API for JSON
            // indexes, so this lives outside the model — the provider conventions pass removes the
            // index on SQL Server precisely so EF does not try to create a regular one over a
            // `json` column, which is illegal. Requires a clustered primary key, which
            // PK_AuditRecords provides.
            migrationBuilder.Sql("""
                CREATE JSON INDEX [IX_AuditRecords_PayloadJson_json]
                    ON [audit].[AuditRecords]([PayloadJson])
                    FOR ('$');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS [IX_AuditRecords_PayloadJson_json] ON [audit].[AuditRecords];");

            migrationBuilder.DropTable(
                name: "AuditRecords",
                schema: "audit");
        }
    }
}
