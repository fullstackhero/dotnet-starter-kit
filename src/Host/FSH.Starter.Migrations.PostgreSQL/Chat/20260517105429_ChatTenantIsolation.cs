using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Chat
{
    /// <inheritdoc />
    public partial class ChatTenantIsolation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_MessageReactions_Message_User_Emoji",
                schema: "chat",
                table: "MessageReactions");

            migrationBuilder.DropIndex(
                name: "IX_Channels_DirectKey",
                schema: "chat",
                table: "Channels");

            migrationBuilder.DropIndex(
                name: "IX_Channels_Slug",
                schema: "chat",
                table: "Channels");

            migrationBuilder.DropIndex(
                name: "IX_ChannelMembers_UserId_ChannelId",
                schema: "chat",
                table: "ChannelMembers");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "chat",
                table: "Messages",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "chat",
                table: "MessageReactions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "chat",
                table: "MessageMentions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "chat",
                table: "MessageAttachments",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "chat",
                table: "Channels",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                schema: "chat",
                table: "ChannelMembers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "UX_MessageReactions_Message_User_Emoji",
                schema: "chat",
                table: "MessageReactions",
                columns: new[] { "MessageId", "UserId", "Emoji", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Channels_DirectKey",
                schema: "chat",
                table: "Channels",
                columns: new[] { "DirectKey", "TenantId" },
                unique: true,
                filter: "\"Type\" = 0 AND \"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_Channels_Slug",
                schema: "chat",
                table: "Channels",
                columns: new[] { "Slug", "TenantId" },
                unique: true,
                filter: "\"Slug\" IS NOT NULL AND \"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelMembers_UserId_ChannelId",
                schema: "chat",
                table: "ChannelMembers",
                columns: new[] { "UserId", "ChannelId", "TenantId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_MessageReactions_Message_User_Emoji",
                schema: "chat",
                table: "MessageReactions");

            migrationBuilder.DropIndex(
                name: "IX_Channels_DirectKey",
                schema: "chat",
                table: "Channels");

            migrationBuilder.DropIndex(
                name: "IX_Channels_Slug",
                schema: "chat",
                table: "Channels");

            migrationBuilder.DropIndex(
                name: "IX_ChannelMembers_UserId_ChannelId",
                schema: "chat",
                table: "ChannelMembers");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "chat",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "chat",
                table: "MessageReactions");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "chat",
                table: "MessageMentions");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "chat",
                table: "MessageAttachments");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "chat",
                table: "Channels");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "chat",
                table: "ChannelMembers");

            migrationBuilder.CreateIndex(
                name: "UX_MessageReactions_Message_User_Emoji",
                schema: "chat",
                table: "MessageReactions",
                columns: new[] { "MessageId", "UserId", "Emoji" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Channels_DirectKey",
                schema: "chat",
                table: "Channels",
                column: "DirectKey",
                unique: true,
                filter: "\"Type\" = 0 AND \"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_Channels_Slug",
                schema: "chat",
                table: "Channels",
                column: "Slug",
                unique: true,
                filter: "\"Slug\" IS NOT NULL AND \"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelMembers_UserId_ChannelId",
                schema: "chat",
                table: "ChannelMembers",
                columns: new[] { "UserId", "ChannelId" },
                unique: true);
        }
    }
}
