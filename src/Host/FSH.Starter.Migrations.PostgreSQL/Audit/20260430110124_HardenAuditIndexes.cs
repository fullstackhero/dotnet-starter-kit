using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Audit
{
    /// <inheritdoc />
    public partial class HardenAuditIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AuditRecords_EventType",
                schema: "audit",
                table: "AuditRecords");

            migrationBuilder.DropIndex(
                name: "IX_AuditRecords_OccurredAtUtc",
                schema: "audit",
                table: "AuditRecords");

            migrationBuilder.DropIndex(
                name: "IX_AuditRecords_TenantId",
                schema: "audit",
                table: "AuditRecords");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateIndex(
                name: "IX_AuditRecords_CorrelationId",
                schema: "audit",
                table: "AuditRecords",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditRecords_PayloadJson_gin",
                schema: "audit",
                table: "AuditRecords",
                column: "PayloadJson")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "jsonb_path_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditRecords_Source_trgm",
                schema: "audit",
                table: "AuditRecords",
                column: "Source")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

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

            migrationBuilder.CreateIndex(
                name: "IX_AuditRecords_UserName_trgm",
                schema: "audit",
                table: "AuditRecords",
                column: "UserName")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AuditRecords_CorrelationId",
                schema: "audit",
                table: "AuditRecords");

            migrationBuilder.DropIndex(
                name: "IX_AuditRecords_PayloadJson_gin",
                schema: "audit",
                table: "AuditRecords");

            migrationBuilder.DropIndex(
                name: "IX_AuditRecords_Source_trgm",
                schema: "audit",
                table: "AuditRecords");

            migrationBuilder.DropIndex(
                name: "IX_AuditRecords_Tenant_EventType_OccurredAt",
                schema: "audit",
                table: "AuditRecords");

            migrationBuilder.DropIndex(
                name: "IX_AuditRecords_Tenant_OccurredAt",
                schema: "audit",
                table: "AuditRecords");

            migrationBuilder.DropIndex(
                name: "IX_AuditRecords_TraceId",
                schema: "audit",
                table: "AuditRecords");

            migrationBuilder.DropIndex(
                name: "IX_AuditRecords_UserName_trgm",
                schema: "audit",
                table: "AuditRecords");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateIndex(
                name: "IX_AuditRecords_EventType",
                schema: "audit",
                table: "AuditRecords",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_AuditRecords_OccurredAtUtc",
                schema: "audit",
                table: "AuditRecords",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AuditRecords_TenantId",
                schema: "audit",
                table: "AuditRecords",
                column: "TenantId");
        }
    }
}
