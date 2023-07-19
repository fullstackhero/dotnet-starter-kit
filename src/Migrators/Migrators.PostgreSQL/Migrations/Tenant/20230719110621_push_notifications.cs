using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class push_notifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PushNotificationsSettings_AppId",
                schema: "MultiTenancy",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PushNotificationsSettings_AuthKey",
                schema: "MultiTenancy",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PushNotificationsSettings_IconUrl",
                schema: "MultiTenancy",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PushNotificationsSettings_Name",
                schema: "MultiTenancy",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PushNotificationsSettings_Provider",
                schema: "MultiTenancy",
                table: "Tenants",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PushNotificationsSettings_AppId",
                schema: "MultiTenancy",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "PushNotificationsSettings_AuthKey",
                schema: "MultiTenancy",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "PushNotificationsSettings_IconUrl",
                schema: "MultiTenancy",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "PushNotificationsSettings_Name",
                schema: "MultiTenancy",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "PushNotificationsSettings_Provider",
                schema: "MultiTenancy",
                table: "Tenants");
        }
    }
}
