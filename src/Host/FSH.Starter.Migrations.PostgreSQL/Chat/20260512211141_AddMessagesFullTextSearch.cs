using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.PostgreSQL.Chat
{
    /// <summary>
    /// Adds a generated tsvector column on chat.Messages and a GIN index so the search endpoint
    /// can run native Postgres full-text queries. Kept out of the EF model (raw SQL only) because
    /// EF doesn't track GENERATED columns and we never write to it from C#.
    /// </summary>
    public partial class AddMessagesFullTextSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);
            migrationBuilder.Sql(@"
ALTER TABLE chat.""Messages""
    ADD COLUMN ""BodyTsv"" tsvector
    GENERATED ALWAYS AS (to_tsvector('english', coalesce(""Body"", ''))) STORED;

CREATE INDEX ""IX_Messages_BodyTsv""
    ON chat.""Messages""
    USING gin (""BodyTsv"");
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);
            migrationBuilder.Sql(@"
DROP INDEX IF EXISTS chat.""IX_Messages_BodyTsv"";
ALTER TABLE chat.""Messages"" DROP COLUMN IF EXISTS ""BodyTsv"";
");
        }
    }
}
