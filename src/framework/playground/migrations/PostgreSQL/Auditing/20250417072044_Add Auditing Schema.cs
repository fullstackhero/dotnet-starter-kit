using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.PlayGround.Migrations.PostgreSQL.Auditing;

/// <inheritdoc />
public partial class AddAuditingSchema : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "auditing");

        migrationBuilder.CreateTable(
            name: "Trails",
            schema: "auditing",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                DateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                Operation = table.Column<int>(type: "integer", nullable: false),
                Description = table.Column<string>(type: "text", nullable: false),
                EntityName = table.Column<string>(type: "text", nullable: true),
                KeyValuesJson = table.Column<string>(type: "text", nullable: false),
                OldValuesJson = table.Column<string>(type: "text", nullable: false),
                NewValuesJson = table.Column<string>(type: "text", nullable: false),
                ModifiedPropertiesJson = table.Column<string>(type: "text", nullable: false),
                TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Trails", x => x.Id);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Trails",
            schema: "auditing");
    }
}