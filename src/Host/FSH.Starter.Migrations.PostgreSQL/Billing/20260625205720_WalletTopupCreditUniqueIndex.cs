using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Billing
{
    /// <inheritdoc />
    public partial class WalletTopupCreditUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ux_wallet_transactions_topup_reference",
                schema: "billing",
                table: "WalletTransactions",
                column: "ReferenceId",
                unique: true,
                filter: "\"Kind\" = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_wallet_transactions_topup_reference",
                schema: "billing",
                table: "WalletTransactions");
        }
    }
}
