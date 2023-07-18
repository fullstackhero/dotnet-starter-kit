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
                name: "PushNotificationInfo_AppId",
                schema: "MultiTenancy",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PushNotificationInfo_AuthKey",
                schema: "MultiTenancy",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PushNotificationInfo_IconUrl",
                schema: "MultiTenancy",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PushNotificationInfo_Name",
                schema: "MultiTenancy",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PushNotificationInfo_Provider",
                schema: "MultiTenancy",
                table: "Tenants",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PushNotificationInfo_AppId",
                schema: "MultiTenancy",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "PushNotificationInfo_AuthKey",
                schema: "MultiTenancy",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "PushNotificationInfo_IconUrl",
                schema: "MultiTenancy",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "PushNotificationInfo_Name",
                schema: "MultiTenancy",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "PushNotificationInfo_Provider",
                schema: "MultiTenancy",
                table: "Tenants");
        }
    }
}
