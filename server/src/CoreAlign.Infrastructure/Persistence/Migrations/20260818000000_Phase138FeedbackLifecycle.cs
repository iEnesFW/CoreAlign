using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    public partial class Phase138FeedbackLifecycle : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE feedback_tickets ADD COLUMN IF NOT EXISTS concurrency_token bigint NOT NULL DEFAULT 0;
                ALTER TABLE feedback_tickets ADD COLUMN IF NOT EXISTS status_change_count integer NOT NULL DEFAULT 0;
                ALTER TABLE feedback_tickets ALTER COLUMN title TYPE character varying(200);
                ALTER TABLE feedback_tickets ALTER COLUMN page_url TYPE character varying(500);
                ALTER TABLE feedback_tickets ALTER COLUMN module TYPE character varying(100);
                ALTER TABLE feedback_tickets ALTER COLUMN created_by_name TYPE character varying(200);

                CREATE TABLE IF NOT EXISTS feedback_attachments (
                    id uuid NOT NULL,
                    feedback_ticket_id uuid NOT NULL,
                    storage_path character varying(500) NOT NULL,
                    display_file_name character varying(255) NOT NULL,
                    content_type character varying(128) NOT NULL,
                    size_bytes bigint NOT NULL,
                    uploaded_by_user_id uuid NULL,
                    display_order integer NOT NULL,
                    tenant_id uuid NOT NULL,
                    created_at_utc timestamp with time zone NOT NULL,
                    updated_at_utc timestamp with time zone NOT NULL,
                    CONSTRAINT pk_feedback_attachments PRIMARY KEY (id),
                    CONSTRAINT fk_feedback_attachments_feedback_tickets_feedback_ticket_id
                        FOREIGN KEY (feedback_ticket_id) REFERENCES feedback_tickets (id) ON DELETE CASCADE,
                    CONSTRAINT fk_feedback_attachments_tenants_tenant_id
                        FOREIGN KEY (tenant_id) REFERENCES tenants (id) ON DELETE RESTRICT);

                CREATE TABLE IF NOT EXISTS feedback_ticket_comments (
                    id uuid NOT NULL,
                    feedback_ticket_id uuid NOT NULL,
                    author_user_id uuid NULL,
                    author_name character varying(200) NULL,
                    body character varying(4000) NOT NULL,
                    is_internal boolean NOT NULL,
                    tenant_id uuid NOT NULL,
                    created_at_utc timestamp with time zone NOT NULL,
                    updated_at_utc timestamp with time zone NOT NULL,
                    CONSTRAINT pk_feedback_ticket_comments PRIMARY KEY (id),
                    CONSTRAINT fk_feedback_ticket_comments_feedback_tickets_feedback_ticket_id
                        FOREIGN KEY (feedback_ticket_id) REFERENCES feedback_tickets (id) ON DELETE CASCADE,
                    CONSTRAINT fk_feedback_ticket_comments_tenants_tenant_id
                        FOREIGN KEY (tenant_id) REFERENCES tenants (id) ON DELETE RESTRICT);

                DROP INDEX IF EXISTS ix_feedback_tickets_tenant_id;
                CREATE INDEX IF NOT EXISTS ix_feedback_tickets_tenant_id_status_created_at_utc
                    ON feedback_tickets (tenant_id, status, created_at_utc);
                CREATE INDEX IF NOT EXISTS ix_feedback_attachments_feedback_ticket_id
                    ON feedback_attachments (feedback_ticket_id);
                CREATE INDEX IF NOT EXISTS "ix_feedback_attachments_tenant_id_feedback_ticket_id_display_o~"
                    ON feedback_attachments (tenant_id, feedback_ticket_id, display_order);
                CREATE INDEX IF NOT EXISTS ix_feedback_ticket_comments_feedback_ticket_id
                    ON feedback_ticket_comments (feedback_ticket_id);
                CREATE INDEX IF NOT EXISTS "ix_feedback_ticket_comments_tenant_id_feedback_ticket_id_creat~"
                    ON feedback_ticket_comments (tenant_id, feedback_ticket_id, created_at_utc);
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO feedback_attachments (
                    id, tenant_id, feedback_ticket_id, storage_path, display_file_name,
                    content_type, size_bytes, uploaded_by_user_id, display_order,
                    created_at_utc, updated_at_utc)
                SELECT gen_random_uuid(), t.tenant_id, t.id, t.attachment_path,
                       COALESCE(t.attachment_file_name, 'attachment'),
                       COALESCE(t.attachment_content_type, 'application/octet-stream'),
                       0, t.created_by_user_id, 0, t.created_at_utc, t.updated_at_utc
                FROM feedback_tickets t
                WHERE t.attachment_path IS NOT NULL
                  AND NOT EXISTS (SELECT 1 FROM feedback_attachments a
                                  WHERE a.feedback_ticket_id = t.id
                                    AND a.storage_path = t.attachment_path);
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS feedback_ticket_comments;
                DROP TABLE IF EXISTS feedback_attachments;
                DROP INDEX IF EXISTS ix_feedback_tickets_tenant_id_status_created_at_utc;
                ALTER TABLE feedback_tickets DROP COLUMN IF EXISTS status_change_count;
                ALTER TABLE feedback_tickets DROP COLUMN IF EXISTS concurrency_token;
                CREATE INDEX IF NOT EXISTS ix_feedback_tickets_tenant_id ON feedback_tickets (tenant_id);
                """);
        }
    }
}
