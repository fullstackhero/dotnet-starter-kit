using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Chat
{
    /// <inheritdoc />
    public partial class InitialChat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "chat");

            migrationBuilder.CreateTable(
                name: "Channels",
                schema: "chat",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Slug = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: true),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsPrivate = table.Column<bool>(type: "boolean", nullable: false),
                    DirectKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastMessageAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Channels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChannelMembers",
                schema: "chat",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChannelId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    JoinedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastReadMessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsMuted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChannelMembers_Channels_ChannelId",
                        column: x => x.ChannelId,
                        principalSchema: "chat",
                        principalTable: "Channels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                schema: "chat",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChannelId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorUserId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Body = table.Column<string>(type: "text", nullable: true),
                    ParentMessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReplyCount = table.Column<int>(type: "integer", nullable: false),
                    EditedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Messages_Channels_ChannelId",
                        column: x => x.ChannelId,
                        principalSchema: "chat",
                        principalTable: "Channels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MessageAttachments",
                schema: "chat",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileAssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    Url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessageAttachments_Messages_MessageId",
                        column: x => x.MessageId,
                        principalSchema: "chat",
                        principalTable: "Messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChannelMembers_ChannelId",
                schema: "chat",
                table: "ChannelMembers",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelMembers_UserId",
                schema: "chat",
                table: "ChannelMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelMembers_UserId_ChannelId",
                schema: "chat",
                table: "ChannelMembers",
                columns: new[] { "UserId", "ChannelId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Channels_DirectKey",
                schema: "chat",
                table: "Channels",
                column: "DirectKey",
                unique: true,
                filter: "\"Type\" = 0 AND \"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_Channels_IsDeleted",
                schema: "chat",
                table: "Channels",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Channels_Slug",
                schema: "chat",
                table: "Channels",
                column: "Slug",
                unique: true,
                filter: "\"Slug\" IS NOT NULL AND \"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_MessageAttachments_MessageId",
                schema: "chat",
                table: "MessageAttachments",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ChannelId_Id",
                schema: "chat",
                table: "Messages",
                columns: new[] { "ChannelId", "Id" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ParentMessageId",
                schema: "chat",
                table: "Messages",
                column: "ParentMessageId",
                filter: "\"ParentMessageId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChannelMembers",
                schema: "chat");

            migrationBuilder.DropTable(
                name: "MessageAttachments",
                schema: "chat");

            migrationBuilder.DropTable(
                name: "Messages",
                schema: "chat");

            migrationBuilder.DropTable(
                name: "Channels",
                schema: "chat");
        }
    }
}
