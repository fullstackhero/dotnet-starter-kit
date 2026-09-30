using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Files
{
    /// <inheritdoc />
    public partial class FileAssetLegacyKeyIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_FileAsset_LegacyKey",
                schema: "files",
                table: "FileAssets",
                column: "Id",
                filter: "\"StorageKey\" NOT LIKE 'public/%' AND \"StorageKey\" NOT LIKE 'private/%' AND \"Visibility\" = 0 AND \"Status\" = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FileAsset_LegacyKey",
                schema: "files",
                table: "FileAssets");
        }
    }
}
