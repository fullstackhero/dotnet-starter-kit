using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Migrations.PostgreSQL.Audit
{
    /// <inheritdoc />
    public partial class AddAuditIndexesForPerformance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AuditRecords_OccurredAtUtc",
                schema: "audit",
                table: "AuditRecords");

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                schema: "audit",
                table: "AuditRecords",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.CreateIndex(
                name: "IX_AuditRecords_CorrelationId",
                schema: "audit",
                table: "AuditRecords",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditRecords_PayloadJson",
                schema: "audit",
                table: "AuditRecords",
                column: "PayloadJson")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "jsonb_path_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditRecords_Severity",
                schema: "audit",
                table: "AuditRecords",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_AuditRecords_Source",
                schema: "audit",
                table: "AuditRecords",
                column: "Source");

            migrationBuilder.CreateIndex(
                name: "IX_AuditRecords_Tags",
                schema: "audit",
                table: "AuditRecords",
                column: "Tags");

            migrationBuilder.CreateIndex(
                name: "IX_AuditRecords_TenantId_OccurredAtUtc",
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
                name: "IX_AuditRecords_UserId",
                schema: "audit",
                table: "AuditRecords",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditRecords_UserId_OccurredAtUtc",
                schema: "audit",
                table: "AuditRecords",
                columns: new[] { "UserId", "OccurredAtUtc" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AuditRecords_CorrelationId",
                schema: "audit",
                table: "AuditRecords");

            migrationBuilder.DropIndex(
                name: "IX_AuditRecords_PayloadJson",
                schema: "audit",
                table: "AuditRecords");

            migrationBuilder.DropIndex(
                name: "IX_AuditRecords_Severity",
                schema: "audit",
                table: "AuditRecords");

            migrationBuilder.DropIndex(
                name: "IX_AuditRecords_Source",
                schema: "audit",
                table: "AuditRecords");

            migrationBuilder.DropIndex(
                name: "IX_AuditRecords_Tags",
                schema: "audit",
                table: "AuditRecords");

            migrationBuilder.DropIndex(
                name: "IX_AuditRecords_TenantId_OccurredAtUtc",
                schema: "audit",
                table: "AuditRecords");

            migrationBuilder.DropIndex(
                name: "IX_AuditRecords_TraceId",
                schema: "audit",
                table: "AuditRecords");

            migrationBuilder.DropIndex(
                name: "IX_AuditRecords_UserId",
                schema: "audit",
                table: "AuditRecords");

            migrationBuilder.DropIndex(
                name: "IX_AuditRecords_UserId_OccurredAtUtc",
                schema: "audit",
                table: "AuditRecords");

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                schema: "audit",
                table: "AuditRecords",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_AuditRecords_OccurredAtUtc",
                schema: "audit",
                table: "AuditRecords",
                column: "OccurredAtUtc");
        }
    }
}
