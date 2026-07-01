using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Billing
{
    /// <inheritdoc />
    public partial class BillingMoneyValueObjectAdoption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // WalletTransaction.Amount and InvoiceLineItem.Amount became Money owned value objects, and
            // BillingPlan.AnnualPrice became an optional Money. The amount columns are preserved; only the
            // per-row currency columns are new. Currency is denormalized from the owning aggregate, so
            // existing rows are backfilled from the parent's currency before the NOT NULL columns are enforced.
            // (Wallet.Balance/Invoice.SubtotalAmount/BillingPlan.MonthlyBasePrice reuse their existing
            // "Currency" columns via OwnsOne, so those adoptions are pure no-ops with no schema change here.)

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                schema: "billing",
                table: "WalletTransactions",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AmountCurrency",
                schema: "billing",
                table: "InvoiceLineItems",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AnnualPriceCurrency",
                schema: "billing",
                table: "Plans",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE billing."WalletTransactions" t
                SET "Currency" = w."Currency"
                FROM billing."Wallets" w
                WHERE t."WalletId" = w."Id";
                """);

            migrationBuilder.Sql(
                """
                UPDATE billing."InvoiceLineItems" li
                SET "AmountCurrency" = i."Currency"
                FROM billing."Invoices" i
                WHERE li."InvoiceId" = i."Id";
                """);

            migrationBuilder.Sql(
                """
                UPDATE billing."Plans"
                SET "AnnualPriceCurrency" = "Currency"
                WHERE "AnnualPrice" IS NOT NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                schema: "billing",
                table: "WalletTransactions",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(8)",
                oldMaxLength: 8,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AmountCurrency",
                schema: "billing",
                table: "InvoiceLineItems",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(8)",
                oldMaxLength: 8,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Currency",
                schema: "billing",
                table: "WalletTransactions");

            migrationBuilder.DropColumn(
                name: "AnnualPriceCurrency",
                schema: "billing",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "AmountCurrency",
                schema: "billing",
                table: "InvoiceLineItems");
        }
    }
}
