using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Catalog
{
    /// <inheritdoc />
    public partial class ProductImageValueGenerationNever : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No-op migration. ProductImage.Id is generated client-side via
            // Guid.CreateVersion7(); flipping ValueGeneratedNever() in the EF
            // configuration changes only the model snapshot, not the database
            // schema. Up/Down are intentionally empty so EF records the model
            // change without emitting DDL.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op — see Up(). Reverting the snapshot change requires no DDL.
        }
    }
}
