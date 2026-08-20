using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase145PurchaseOrderLineAwaitingInspection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE purchase_order_lines
    ADD COLUMN IF NOT EXISTS quantity_awaiting_inspection numeric(18,4) NOT NULL DEFAULT 0;");

            migrationBuilder.Sql(@"
UPDATE purchase_order_lines pol
SET quantity_awaiting_inspection = pol.quantity_awaiting_inspection + held.qty,
    quantity_received = GREATEST(0, pol.quantity_received - held.qty)
FROM (
    SELECT grl.purchase_order_line_id AS line_id,
           SUM(LEAST(grl.quantity_received, pol2.quantity_received)) AS qty
    FROM goods_receipt_lines grl
    JOIN goods_receipts grn ON grn.id = grl.goods_receipt_id
    JOIN purchase_order_lines pol2 ON pol2.id = grl.purchase_order_line_id
    WHERE grn.qc_status = 1 AND grn.status <> 'Reversed'
    GROUP BY grl.purchase_order_line_id
) held
WHERE pol.id = held.line_id AND held.qty > 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE purchase_order_lines
SET quantity_received = quantity_received + quantity_awaiting_inspection
WHERE quantity_awaiting_inspection > 0;");

            migrationBuilder.Sql(@"
ALTER TABLE purchase_order_lines DROP COLUMN IF EXISTS quantity_awaiting_inspection;");
        }
    }
}
