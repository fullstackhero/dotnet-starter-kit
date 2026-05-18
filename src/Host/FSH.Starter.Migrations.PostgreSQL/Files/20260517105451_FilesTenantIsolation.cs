using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Files
{
    /// <inheritdoc />
    public partial class FilesTenantIsolation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_FileAsset_StorageKey",
                schema: "files",
                table: "FileAssets");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "files",
                table: "FileAssets",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "UX_FileAsset_StorageKey",
                schema: "files",
                table: "FileAssets",
                columns: new[] { "StorageKey", "TenantId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_FileAsset_StorageKey",
                schema: "files",
                table: "FileAssets");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "files",
                table: "FileAssets");

            migrationBuilder.CreateIndex(
                name: "UX_FileAsset_StorageKey",
                schema: "files",
                table: "FileAssets",
                column: "StorageKey",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }
    }
}
