using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations.Application
{
    /// <inheritdoc />
    public partial class Task8 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomerDetailsInfo",
                schema: "LeadData",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContactId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineOfBusinessId = table.Column<int>(type: "integer", nullable: true),
                    CustomerCompanyId = table.Column<int>(type: "integer", nullable: true),
                    CustomerProductId = table.Column<int>(type: "integer", nullable: true),
                    NumberOfLivesId = table.Column<int>(type: "integer", nullable: true),
                    SaORSiORIdv = table.Column<string>(type: "text", nullable: true),
                    DeductibleSI = table.Column<string>(type: "text", nullable: true),
                    ModeOfPaymentId = table.Column<int>(type: "integer", nullable: true),
                    GrossPremium = table.Column<string>(type: "text", nullable: true),
                    NetPremium = table.Column<string>(type: "text", nullable: true),
                    ODPremium = table.Column<string>(type: "text", nullable: true),
                    AddOnPremium = table.Column<string>(type: "text", nullable: true),
                    TPPremium = table.Column<string>(type: "text", nullable: true),
                    PremiumForCommission = table.Column<decimal>(type: "numeric", nullable: true),
                    VehicleNumber = table.Column<string>(type: "text", nullable: true),
                    NCB = table.Column<string>(type: "text", nullable: true),
                    LifePayingTerm = table.Column<string>(type: "text", nullable: true),
                    LifeTermPoilcy = table.Column<string>(type: "text", nullable: true),
                    ISPORMarketing = table.Column<string>(type: "text", nullable: true),
                    TeamLead = table.Column<string>(type: "text", nullable: true),
                    PolicyNumber = table.Column<string>(type: "text", nullable: true),
                    PolicyStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PolicyExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RenewalRemainderDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RenewalFlag = table.Column<string>(type: "text", nullable: true),
                    PolicyStatusId = table.Column<int>(type: "integer", nullable: true),
                    PolicyIssueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Insured1Name = table.Column<string>(type: "text", nullable: true),
                    Insured1DOB = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Insured2Name = table.Column<string>(type: "text", nullable: true),
                    Insured2DOB = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Insured3Name = table.Column<string>(type: "text", nullable: true),
                    Insured3DOB = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Insured4Name = table.Column<string>(type: "text", nullable: true),
                    Insured4DOB = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Insured5Name = table.Column<string>(type: "text", nullable: true),
                    Insured5DOB = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CommissionReceivable = table.Column<decimal>(type: "numeric", nullable: true),
                    CommissionPayable = table.Column<decimal>(type: "numeric", nullable: true),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    LastModifiedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerDetailsInfo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductDetailsInfo",
                schema: "LeadData",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductName = table.Column<string>(type: "text", nullable: true),
                    ProductCode = table.Column<string>(type: "text", nullable: true),
                    VendorId = table.Column<Guid>(type: "uuid", nullable: true),
                    UnitPrice = table.Column<decimal>(type: "numeric", nullable: true),
                    Tax = table.Column<string>(type: "text", nullable: true),
                    Taxable = table.Column<bool>(type: "boolean", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    LastModifiedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductDetailsInfo", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerDetailsInfo",
                schema: "LeadData");

            migrationBuilder.DropTable(
                name: "ProductDetailsInfo",
                schema: "LeadData");
        }
    }
}
