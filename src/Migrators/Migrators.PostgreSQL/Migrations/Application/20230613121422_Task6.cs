using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations.Application
{
    /// <inheritdoc />
    public partial class Task6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
           

            migrationBuilder.AlterColumn<string>(
                name: "ZipCode",
                schema: "LeadData",
                table: "LeadDetailsInfo",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Website",
                schema: "LeadData",
                table: "LeadDetailsInfo",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Twitter",
                schema: "LeadData",
                table: "LeadDetailsInfo",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                schema: "LeadData",
                table: "LeadDetailsInfo",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Street",
                schema: "LeadData",
                table: "LeadDetailsInfo",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "State",
                schema: "LeadData",
                table: "LeadDetailsInfo",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "SkypeId",
                schema: "LeadData",
                table: "LeadDetailsInfo",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "SecondEmail",
                schema: "LeadData",
                table: "LeadDetailsInfo",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Rating",
                schema: "LeadData",
                table: "LeadDetailsInfo",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                schema: "LeadData",
                table: "LeadDetailsInfo",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Mobile",
                schema: "LeadData",
                table: "LeadDetailsInfo",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "LeadStatus",
                schema: "LeadData",
                table: "LeadDetailsInfo",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "LeadSource",
                schema: "LeadData",
                table: "LeadDetailsInfo",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "LeadImage",
                schema: "LeadData",
                table: "LeadDetailsInfo",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                schema: "LeadData",
                table: "LeadDetailsInfo",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Industry",
                schema: "LeadData",
                table: "LeadDetailsInfo",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                schema: "LeadData",
                table: "LeadDetailsInfo",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Fax",
                schema: "LeadData",
                table: "LeadDetailsInfo",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                schema: "LeadData",
                table: "LeadDetailsInfo",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                schema: "LeadData",
                table: "LeadDetailsInfo",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Country",
                schema: "LeadData",
                table: "LeadDetailsInfo",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "CompanyName",
                schema: "LeadData",
                table: "LeadDetailsInfo",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "City",
                schema: "LeadData",
                table: "LeadDetailsInfo",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateTable(
                name: "ContactDetailsInfo",
                schema: "LeadData",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContactOwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: true),
                    LastName = table.Column<string>(type: "text", nullable: true),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Department = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    HomePhone = table.Column<string>(type: "text", nullable: true),
                    Fax = table.Column<string>(type: "text", nullable: true),
                    Mobile = table.Column<string>(type: "text", nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SkypeId = table.Column<string>(type: "text", nullable: true),
                    SecondEmail = table.Column<string>(type: "text", nullable: true),
                    Twitter = table.Column<string>(type: "text", nullable: true),
                    MailingStreet = table.Column<string>(type: "text", nullable: true),
                    OtherStreet = table.Column<string>(type: "text", nullable: true),
                    MailingCity = table.Column<string>(type: "text", nullable: true),
                    OtherCity = table.Column<string>(type: "text", nullable: true),
                    MailingState = table.Column<string>(type: "text", nullable: true),
                    OtherState = table.Column<string>(type: "text", nullable: true),
                    MailingZipcode = table.Column<string>(type: "text", nullable: true),
                    OtherZipCode = table.Column<string>(type: "text", nullable: true),
                    MailingCountry = table.Column<string>(type: "text", nullable: true),
                    OtherCountry = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ContactImage = table.Column<string>(type: "text", nullable: true),
                    LeadSource = table.Column<string>(type: "text", nullable: true),
                    EmailOptOut = table.Column<bool>(type: "boolean", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: true),
                    Assistant = table.Column<string>(type: "text", nullable: true),
                    AssistantNumber = table.Column<string>(type: "text", nullable: true),
                    Source = table.Column<string>(type: "text", nullable: true),
                    LeadId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportTo = table.Column<Guid>(type: "uuid", nullable: false),
                    OtherPhone = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_ContactDetailsInfo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceDetailsInfo",
                schema: "LeadData",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceOwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContactId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExciseDuty = table.Column<decimal>(type: "numeric", nullable: false),
                    InvoiceStatusId = table.Column<int>(type: "integer", nullable: false),
                    Subject = table.Column<string>(type: "text", nullable: true),
                    InvoiceDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SalesCommission = table.Column<decimal>(type: "numeric", nullable: false),
                    BillingStreet = table.Column<string>(type: "text", nullable: true),
                    BillingCity = table.Column<string>(type: "text", nullable: true),
                    BillingState = table.Column<string>(type: "text", nullable: true),
                    BillingCode = table.Column<string>(type: "text", nullable: true),
                    BillingCountry = table.Column<string>(type: "text", nullable: true),
                    ShippingStreet = table.Column<string>(type: "text", nullable: true),
                    ShippingCity = table.Column<string>(type: "text", nullable: true),
                    ShippingState = table.Column<string>(type: "text", nullable: true),
                    ShippingCode = table.Column<string>(type: "text", nullable: true),
                    ShippingCountry = table.Column<string>(type: "text", nullable: true),
                    InvoiceItemsJson = table.Column<string>(type: "jsonb", nullable: false),
                    SubTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalDiscount = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalTax = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalAdjustment = table.Column<decimal>(type: "numeric", nullable: false),
                    GrandTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    QuoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerInsuranceId = table.Column<Guid>(type: "uuid", nullable: false),
                    LeadId = table.Column<Guid>(type: "uuid", nullable: false),
                    TermsConditions = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_InvoiceDetailsInfo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PersonalDetailsInfo",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: true),
                    LastName = table.Column<string>(type: "text", nullable: true),
                    AliasName = table.Column<string>(type: "text", nullable: true),
                    Role = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Website = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Mobile = table.Column<string>(type: "text", nullable: true),
                    Fax = table.Column<string>(type: "text", nullable: true),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    Street = table.Column<string>(type: "text", nullable: true),
                    City = table.Column<string>(type: "text", nullable: true),
                    State = table.Column<string>(type: "text", nullable: true),
                    Zipcode = table.Column<string>(type: "text", nullable: true),
                    Country = table.Column<string>(type: "text", nullable: true),
                    Language = table.Column<string>(type: "text", nullable: true),
                    CountryLocale = table.Column<string>(type: "text", nullable: true),
                    TimeZone = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<int>(type: "integer", nullable: false),
                    SecurityQuestion = table.Column<string>(type: "text", nullable: true),
                    SecurityAnswer = table.Column<string>(type: "text", nullable: true),
                    Profile = table.Column<string>(type: "text", nullable: true),
                    Designation = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_PersonalDetailsInfo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QuotationDetailsInfo",
                schema: "LeadData",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Team = table.Column<string>(type: "text", nullable: true),
                    DealId = table.Column<Guid>(type: "uuid", nullable: false),
                    Subject = table.Column<string>(type: "text", nullable: true),
                    ValidDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ContactId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Carrier = table.Column<string>(type: "text", nullable: true),
                    QuoteStage = table.Column<string>(type: "text", nullable: true),
                    BillingStreet = table.Column<string>(type: "text", nullable: true),
                    BillingCity = table.Column<string>(type: "text", nullable: true),
                    BillingState = table.Column<string>(type: "text", nullable: true),
                    BillingCode = table.Column<string>(type: "text", nullable: true),
                    BillingCountry = table.Column<string>(type: "text", nullable: true),
                    ShippingStreet = table.Column<string>(type: "text", nullable: true),
                    ShippingCity = table.Column<string>(type: "text", nullable: true),
                    ShippingState = table.Column<string>(type: "text", nullable: true),
                    ShippingCode = table.Column<string>(type: "text", nullable: true),
                    ShippingCountry = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    TermsConditions = table.Column<string>(type: "text", nullable: true),
                    QuoteOwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    LeadId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuoteItemsJson = table.Column<string>(type: "jsonb", nullable: false),
                    SubTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalDiscount = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalTax = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalAdjustment = table.Column<decimal>(type: "numeric", nullable: false),
                    GrandTotal = table.Column<decimal>(type: "numeric", nullable: false),
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
                    table.PrimaryKey("PK_QuotationDetailsInfo", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContactDetailsInfo",
                schema: "LeadData");

            migrationBuilder.DropTable(
                name: "InvoiceDetailsInfo",
                schema: "LeadData");

            migrationBuilder.DropTable(
                name: "PersonalDetailsInfo",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "QuotationDetailsInfo",
                schema: "LeadData");

            migrationBuilder.AlterColumn<string>(
                name: "ZipCode",
                schema: "LeadData",
                table: "LeadDetailsInfo",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Website",
                schema: "LeadData",
                table: "LeadDetailsInfo",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Twitter",
                schema: "LeadData",
                table: "LeadDetailsInfo",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                schema: "LeadData",
                table: "LeadDetailsInfo",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Street",
                schema: "LeadData",
                table: "LeadDetailsInfo",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "State",
                schema: "LeadData",
                table: "LeadDetailsInfo",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SkypeId",
                schema: "LeadData",
                table: "LeadDetailsInfo",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SecondEmail",
                schema: "LeadData",
                table: "LeadDetailsInfo",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Rating",
                schema: "LeadData",
                table: "LeadDetailsInfo",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                schema: "LeadData",
                table: "LeadDetailsInfo",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Mobile",
                schema: "LeadData",
                table: "LeadDetailsInfo",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LeadStatus",
                schema: "LeadData",
                table: "LeadDetailsInfo",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LeadSource",
                schema: "LeadData",
                table: "LeadDetailsInfo",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LeadImage",
                schema: "LeadData",
                table: "LeadDetailsInfo",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                schema: "LeadData",
                table: "LeadDetailsInfo",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Industry",
                schema: "LeadData",
                table: "LeadDetailsInfo",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                schema: "LeadData",
                table: "LeadDetailsInfo",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Fax",
                schema: "LeadData",
                table: "LeadDetailsInfo",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                schema: "LeadData",
                table: "LeadDetailsInfo",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                schema: "LeadData",
                table: "LeadDetailsInfo",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Country",
                schema: "LeadData",
                table: "LeadDetailsInfo",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CompanyName",
                schema: "LeadData",
                table: "LeadDetailsInfo",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "City",
                schema: "LeadData",
                table: "LeadDetailsInfo",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

           
        }
    }
}
