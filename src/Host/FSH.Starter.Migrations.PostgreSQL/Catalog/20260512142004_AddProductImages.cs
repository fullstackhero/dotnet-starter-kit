using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Catalog
{
    /// <inheritdoc />
    public partial class AddProductImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Create the new table (must happen BEFORE we drop the column so we can copy data).
            migrationBuilder.CreateTable(
                name: "ProductImages",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileAssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    Url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    IsThumbnail = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductImages_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "catalog",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_ProductId_Thumbnail",
                schema: "catalog",
                table: "ProductImages",
                column: "ProductId",
                unique: true,
                filter: "\"IsThumbnail\" = TRUE");

            // 2. Preserve existing data: every Product with a non-null ImageUrl gets a
            //    ProductImages row marked as the thumbnail. FileAssetId stays null because
            //    these legacy URLs predate the Files module (could be external URLs or local
            //    paths the dashboard pasted via the old text input).
            migrationBuilder.Sql(
                """
                INSERT INTO catalog."ProductImages"
                    ("Id", "ProductId", "FileAssetId", "Url", "IsThumbnail", "SortOrder", "CreatedAtUtc")
                SELECT
                    gen_random_uuid(),
                    p."Id",
                    NULL,
                    p."ImageUrl",
                    TRUE,
                    0,
                    COALESCE(p."UpdatedAtUtc", p."CreatedAtUtc")
                FROM catalog."Products" p
                WHERE p."ImageUrl" IS NOT NULL AND length(trim(p."ImageUrl")) > 0;
                """);

            // 3. Drop the old column now that data is preserved.
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                schema: "catalog",
                table: "Products");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 1. Restore the column.
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                schema: "catalog",
                table: "Products",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            // 2. Copy each product's thumbnail URL back into the column. The MAX trick prevents
            //    duplicate-row errors if the partial unique index was bypassed somehow.
            migrationBuilder.Sql(
                """
                UPDATE catalog."Products" p
                SET "ImageUrl" = sub."Url"
                FROM (
                    SELECT "ProductId", MAX("Url") AS "Url"
                    FROM catalog."ProductImages"
                    WHERE "IsThumbnail" = TRUE
                    GROUP BY "ProductId"
                ) sub
                WHERE p."Id" = sub."ProductId";
                """);

            // 3. Drop the new table.
            migrationBuilder.DropTable(
                name: "ProductImages",
                schema: "catalog");
        }
    }
}
