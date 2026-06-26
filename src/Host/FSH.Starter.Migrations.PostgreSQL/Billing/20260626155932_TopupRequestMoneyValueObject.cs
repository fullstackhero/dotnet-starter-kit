using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Billing
{
    /// <inheritdoc />
    public partial class TopupRequestMoneyValueObject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No-op: TopupRequest.Amount became a Money owned value object, but the OwnsOne
            // mapping keeps the same "Amount" (numeric(18,4)) and "Currency" (varchar(8)) columns,
            // so the relational schema is unchanged. This migration only carries the model snapshot.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: see Up — the value-object refactor introduced no schema change to revert.
        }
    }
}
