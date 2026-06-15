using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase60Marketplace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "average_rating",
                table: "project_templates",
                type: "numeric(3,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "download_count",
                table: "project_templates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "published_at_utc",
                table: "project_templates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "published_by_user_id",
                table: "project_templates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rejection_reason",
                table: "project_templates",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "review_count",
                table: "project_templates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "submitted_at_utc",
                table: "project_templates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "submitted_by_tenant_id",
                table: "project_templates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "visibility",
                table: "project_templates",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "project_template_installs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    marketplace_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    installed_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    installed_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    installed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_project_template_installs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "project_template_reviews",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reviewer_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rating_stars = table.Column<int>(type: "integer", nullable: false),
                    comment_md = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    reviewed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    concurrency_token = table.Column<long>(type: "bigint", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_project_template_reviews", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_project_templates_visibility",
                table: "project_templates",
                column: "visibility");

            migrationBuilder.CreateIndex(
                name: "ix_project_templates_visibility_category_is_active",
                table: "project_templates",
                columns: new[] { "visibility", "category", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_project_templates_visibility_download_count",
                table: "project_templates",
                columns: new[] { "visibility", "download_count" });

            migrationBuilder.CreateIndex(
                name: "ix_project_template_installs_marketplace_template_id_tenant_id",
                table: "project_template_installs",
                columns: new[] { "marketplace_template_id", "tenant_id" });

            migrationBuilder.CreateIndex(
                name: "ix_project_template_installs_tenant_id_installed_at_utc",
                table: "project_template_installs",
                columns: new[] { "tenant_id", "installed_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_project_template_reviews_template_id",
                table: "project_template_reviews",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "ix_project_template_reviews_template_id_reviewed_at_utc",
                table: "project_template_reviews",
                columns: new[] { "template_id", "reviewed_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_project_template_reviews_template_id_tenant_id_reviewer_use~",
                table: "project_template_reviews",
                columns: new[] { "template_id", "tenant_id", "reviewer_user_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "project_template_installs");

            migrationBuilder.DropTable(
                name: "project_template_reviews");

            migrationBuilder.DropIndex(
                name: "ix_project_templates_visibility",
                table: "project_templates");

            migrationBuilder.DropIndex(
                name: "ix_project_templates_visibility_category_is_active",
                table: "project_templates");

            migrationBuilder.DropIndex(
                name: "ix_project_templates_visibility_download_count",
                table: "project_templates");

            migrationBuilder.DropColumn(
                name: "average_rating",
                table: "project_templates");

            migrationBuilder.DropColumn(
                name: "download_count",
                table: "project_templates");

            migrationBuilder.DropColumn(
                name: "published_at_utc",
                table: "project_templates");

            migrationBuilder.DropColumn(
                name: "published_by_user_id",
                table: "project_templates");

            migrationBuilder.DropColumn(
                name: "rejection_reason",
                table: "project_templates");

            migrationBuilder.DropColumn(
                name: "review_count",
                table: "project_templates");

            migrationBuilder.DropColumn(
                name: "submitted_at_utc",
                table: "project_templates");

            migrationBuilder.DropColumn(
                name: "submitted_by_tenant_id",
                table: "project_templates");

            migrationBuilder.DropColumn(
                name: "visibility",
                table: "project_templates");
        }
    }
}
