using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.MSSQL.Chat
{
    /// <inheritdoc />
    public partial class AddMessageChronologicalSortKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdSort",
                schema: "chat",
                table: "Messages",
                type: "char(36)",
                nullable: true,
                computedColumnSql: "CONVERT(char(36), [Id])",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ChannelId_IdSort",
                schema: "chat",
                table: "Messages",
                columns: new[] { "ChannelId", "IdSort" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ParentMessageId_IdSort",
                schema: "chat",
                table: "Messages",
                columns: new[] { "ParentMessageId", "IdSort" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Messages_ChannelId_IdSort",
                schema: "chat",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_ParentMessageId_IdSort",
                schema: "chat",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "IdSort",
                schema: "chat",
                table: "Messages");
        }
    }
}
