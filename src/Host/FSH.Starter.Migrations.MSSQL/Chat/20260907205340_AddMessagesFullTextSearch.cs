using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Starter.Migrations.MSSQL.Chat
{
    /// <inheritdoc />
    public partial class AddMessagesFullTextSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Counterpart to the PostgreSQL generated tsvector column + GIN index, which likewise
            // live outside the EF model. Guarded on IsFullTextInstalled so the migration still
            // applies to an instance without the Full-Text Search feature (the official
            // mcr.microsoft.com/mssql/server container is one) — SearchMessagesQueryHandler probes
            // sys.fulltext_indexes and falls back to a LIKE scan when the index is absent.
            //
            // suppressTransaction: CREATE FULLTEXT CATALOG cannot run inside a user transaction,
            // and EF wraps migrations in one by default. EXEC keeps each DDL statement in its own
            // batch so the conditional guard is legal.
            migrationBuilder.Sql(
                """
                IF SERVERPROPERTY('IsFullTextInstalled') = 1
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = 'fsh_chat_ft')
                        EXEC(N'CREATE FULLTEXT CATALOG fsh_chat_ft');

                    IF NOT EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('chat.Messages'))
                        EXEC(N'CREATE FULLTEXT INDEX ON chat.[Messages]([Body] LANGUAGE 1033)
                               KEY INDEX [PK_Messages] ON fsh_chat_ft WITH CHANGE_TRACKING AUTO');
                END
                """,
                suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('chat.Messages'))
                    EXEC(N'DROP FULLTEXT INDEX ON chat.[Messages]');

                IF EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = 'fsh_chat_ft')
                    EXEC(N'DROP FULLTEXT CATALOG fsh_chat_ft');
                """,
                suppressTransaction: true);
        }
    }
}
