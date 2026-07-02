using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase111QcHold : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE products ADD COLUMN IF NOT EXISTS requires_inspection boolean NOT NULL DEFAULT false;");
            migrationBuilder.Sql("ALTER TABLE goods_receipts ADD COLUMN IF NOT EXISTS qc_status integer NOT NULL DEFAULT 0;");
            migrationBuilder.Sql("ALTER TABLE goods_receipts ADD COLUMN IF NOT EXISTS qc_decision_at_utc timestamp with time zone NULL;");
            migrationBuilder.Sql("ALTER TABLE goods_receipts ADD COLUMN IF NOT EXISTS qc_decided_by_user_id uuid NULL;");
            migrationBuilder.Sql("ALTER TABLE goods_receipts ADD COLUMN IF NOT EXISTS qc_rejection_reason character varying(500) NULL;");

            migrationBuilder.Sql("ALTER TABLE goods_receipts DROP CONSTRAINT IF EXISTS ck_goods_receipts_qc_status;");
            migrationBuilder.Sql("ALTER TABLE goods_receipts ADD CONSTRAINT ck_goods_receipts_qc_status CHECK (qc_status IN (0,1,2,3));");

            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_goods_receipts_tenant_id_qc_status ON goods_receipts (tenant_id, qc_status);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_goods_receipts_tenant_id_qc_status;");
            migrationBuilder.Sql("ALTER TABLE goods_receipts DROP CONSTRAINT IF EXISTS ck_goods_receipts_qc_status;");
            migrationBuilder.Sql("ALTER TABLE goods_receipts DROP COLUMN IF EXISTS qc_rejection_reason;");
            migrationBuilder.Sql("ALTER TABLE goods_receipts DROP COLUMN IF EXISTS qc_decided_by_user_id;");
            migrationBuilder.Sql("ALTER TABLE goods_receipts DROP COLUMN IF EXISTS qc_decision_at_utc;");
            migrationBuilder.Sql("ALTER TABLE goods_receipts DROP COLUMN IF EXISTS qc_status;");
            migrationBuilder.Sql("ALTER TABLE products DROP COLUMN IF EXISTS requires_inspection;");
        }
    }
}
