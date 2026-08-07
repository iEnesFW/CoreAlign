using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Realigns the EF model with the column Phase12 actually created. journal_entries.source_type
    /// has been varchar(32) since day one, but the entity's enum lost its string conversion, so EF
    /// bound it as an integer and Postgres rejected every read and write with 42883
    /// (character varying = integer) — the GL outbox route could never post a single entry.
    /// The DDL below is a no-op on a healthy database and only repairs a drifted one.
    /// </summary>
    public partial class Phase143JournalSourceTypeString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
DECLARE
    col_type text;
BEGIN
    SELECT data_type INTO col_type
    FROM information_schema.columns
    WHERE table_name = 'journal_entries' AND column_name = 'source_type';

    IF col_type = 'integer' THEN
        ALTER TABLE journal_entries
            ALTER COLUMN source_type TYPE character varying(32) USING source_type::text;
    END IF;

    ALTER TABLE journal_entries ALTER COLUMN source_type DROP NOT NULL;
    ALTER TABLE journal_entries ALTER COLUMN source_type DROP DEFAULT;

    -- The Phase12 default was the empty string, which is not a valid enum name and would throw on
    -- read; nothing should carry it, but leave no landmine behind.
    UPDATE journal_entries SET source_type = NULL WHERE source_type = '';
END $$;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE journal_entries ALTER COLUMN source_type TYPE character varying(32);");
        }
    }
}
