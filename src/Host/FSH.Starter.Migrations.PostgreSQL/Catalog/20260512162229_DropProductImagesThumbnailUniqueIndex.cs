using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Catalog
{
    /// <inheritdoc />
    public partial class DropProductImagesThumbnailUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductImages_ProductId_Thumbnail",
                schema: "catalog",
                table: "ProductImages");

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_ProductId",
                schema: "catalog",
                table: "ProductImages",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductImages_ProductId",
                schema: "catalog",
                table: "ProductImages");

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_ProductId_Thumbnail",
                schema: "catalog",
                table: "ProductImages",
                column: "ProductId",
                unique: true,
                filter: "\"IsThumbnail\" = TRUE");
        }
    }
}
