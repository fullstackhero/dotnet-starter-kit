using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Webhooks
{
    /// <inheritdoc />
    public partial class RenameWebhookSecretHashToProtectedSecret : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SecretHash",
                schema: "webhooks",
                table: "Subscriptions",
                newName: "ProtectedSecret");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ProtectedSecret",
                schema: "webhooks",
                table: "Subscriptions",
                newName: "SecretHash");
        }
    }
}
