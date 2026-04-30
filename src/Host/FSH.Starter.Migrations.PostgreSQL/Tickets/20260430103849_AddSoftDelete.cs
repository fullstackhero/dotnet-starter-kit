using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Tickets
{
    /// <inheritdoc />
    public partial class AddSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_Number",
                schema: "tickets",
                table: "Tickets");

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                schema: "tickets",
                table: "Tickets",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedOnUtc",
                schema: "tickets",
                table: "Tickets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "tickets",
                table: "Tickets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                schema: "tickets",
                table: "TicketComments",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedOnUtc",
                schema: "tickets",
                table: "TicketComments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "tickets",
                table: "TicketComments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_IsDeleted",
                schema: "tickets",
                table: "Tickets",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_Number",
                schema: "tickets",
                table: "Tickets",
                column: "Number",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_TicketComments_IsDeleted",
                schema: "tickets",
                table: "TicketComments",
                column: "IsDeleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_IsDeleted",
                schema: "tickets",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_Number",
                schema: "tickets",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_TicketComments_IsDeleted",
                schema: "tickets",
                table: "TicketComments");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                schema: "tickets",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "DeletedOnUtc",
                schema: "tickets",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "tickets",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                schema: "tickets",
                table: "TicketComments");

            migrationBuilder.DropColumn(
                name: "DeletedOnUtc",
                schema: "tickets",
                table: "TicketComments");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "tickets",
                table: "TicketComments");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_Number",
                schema: "tickets",
                table: "Tickets",
                column: "Number",
                unique: true);
        }
    }
}
