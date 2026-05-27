using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Catalog
{
    /// <inheritdoc />
    public partial class CatalogTenantIsolation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_Sku",
                schema: "catalog",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_Slug",
                schema: "catalog",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Categories_Slug",
                schema: "catalog",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Brands_Slug",
                schema: "catalog",
                table: "Brands");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "catalog",
                table: "Products",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "catalog",
                table: "ProductImages",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "catalog",
                table: "Categories",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "catalog",
                table: "Brands",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Sku",
                schema: "catalog",
                table: "Products",
                columns: new[] { "Sku", "TenantId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Slug",
                schema: "catalog",
                table: "Products",
                columns: new[] { "Slug", "TenantId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Slug",
                schema: "catalog",
                table: "Categories",
                columns: new[] { "Slug", "TenantId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_Brands_Slug",
                schema: "catalog",
                table: "Brands",
                columns: new[] { "Slug", "TenantId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_Sku",
                schema: "catalog",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_Slug",
                schema: "catalog",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Categories_Slug",
                schema: "catalog",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Brands_Slug",
                schema: "catalog",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "catalog",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "catalog",
                table: "ProductImages");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "catalog",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "catalog",
                table: "Brands");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Sku",
                schema: "catalog",
                table: "Products",
                column: "Sku",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Slug",
                schema: "catalog",
                table: "Products",
                column: "Slug",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Slug",
                schema: "catalog",
                table: "Categories",
                column: "Slug",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_Brands_Slug",
                schema: "catalog",
                table: "Brands",
                column: "Slug",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }
    }
}
