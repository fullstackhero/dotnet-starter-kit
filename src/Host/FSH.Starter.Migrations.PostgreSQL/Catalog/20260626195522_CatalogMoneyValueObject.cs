using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Catalog
{
    /// <inheritdoc />
    public partial class CatalogMoneyValueObject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No-op: Product.Price moved from the Catalog-local Money to the promoted
            // FSH.Framework.Core.Domain.Money, but the OwnsOne mapping keeps the same
            // "PriceAmount" (numeric(18,4)) and "Currency" (varchar(3)) columns, so the
            // relational schema is unchanged. This migration only carries the model snapshot.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: see Up — the value-object namespace move introduced no schema change to revert.
        }
    }
}
