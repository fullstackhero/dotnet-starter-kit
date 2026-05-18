using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Tickets
{
    /// <inheritdoc />
    public partial class TicketsTenantIsolation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_Number",
                schema: "tickets",
                table: "Tickets");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "tickets",
                table: "Tickets",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "tickets",
                table: "TicketComments",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_Number",
                schema: "tickets",
                table: "Tickets",
                columns: new[] { "Number", "TenantId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_Number",
                schema: "tickets",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "tickets",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "tickets",
                table: "TicketComments");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_Number",
                schema: "tickets",
                table: "Tickets",
                column: "Number",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }
    }
}
