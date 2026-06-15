using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Snapshot resync only — designer file regenerated against the current model
    /// while the physical schema is already at parity from prior phase migrations.
    /// Up/Down are intentional noops; do not add DDL here. If a true drift is
    /// detected later, author a new dated migration rather than mutating this one.
    /// </summary>
    public partial class Phase50SnapshotResync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("-- noop: snapshot resync only; physical schema already at parity with model from prior phase migrations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("-- noop: snapshot resync only");
        }
    }
}
