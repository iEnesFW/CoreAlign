using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase46OrderRevisions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "current_revision_id",
                table: "orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "applied_revision_count",
                table: "orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "current_revision_id", table: "orders");
            migrationBuilder.DropColumn(name: "applied_revision_count", table: "orders");
        }
    }
}
