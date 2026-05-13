using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Chat
{
    /// <inheritdoc />
    public partial class AddMessagePinning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPinned",
                schema: "chat",
                table: "Messages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PinnedAtUtc",
                schema: "chat",
                table: "Messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PinnedByUserId",
                schema: "chat",
                table: "Messages",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ChannelId_IsPinned",
                schema: "chat",
                table: "Messages",
                columns: new[] { "ChannelId", "IsPinned" },
                filter: "\"IsPinned\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Messages_ChannelId_IsPinned",
                schema: "chat",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "IsPinned",
                schema: "chat",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "PinnedAtUtc",
                schema: "chat",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "PinnedByUserId",
                schema: "chat",
                table: "Messages");
        }
    }
}
