using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Chat
{
    /// <inheritdoc />
    public partial class AddMessageMentions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MessageMentions",
                schema: "chat",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    MentionedUserId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    StartIndex = table.Column<int>(type: "integer", nullable: false),
                    Length = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageMentions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessageMentions_Messages_MessageId",
                        column: x => x.MessageId,
                        principalSchema: "chat",
                        principalTable: "Messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MessageMentions_MentionedUserId",
                schema: "chat",
                table: "MessageMentions",
                column: "MentionedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageMentions_MessageId",
                schema: "chat",
                table: "MessageMentions",
                column: "MessageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MessageMentions",
                schema: "chat");
        }
    }
}
