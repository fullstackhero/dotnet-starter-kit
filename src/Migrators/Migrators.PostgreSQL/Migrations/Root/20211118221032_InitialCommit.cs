using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations.Root;

public partial class InitialCommit : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Tenants",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "text", nullable: true),
                Key = table.Column<string>(type: "text", nullable: true),
                AdminEmail = table.Column<string>(type: "text", nullable: true),
                ConnectionString = table.Column<string>(type: "text", nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                ValidUpto = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: false),
                LastModifiedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                DeletedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Tenants", x => x.Id);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Tenants");
    }
}