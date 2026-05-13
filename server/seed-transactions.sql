DO $$
DECLARE
    tnt uuid;
    r RECORD;
    o RECORD;
    inv RECORD;
    initial_qty numeric(18,4);
    running numeric(18,4);
    has_customer_txn boolean;
    has_stock_txn boolean;
BEGIN
    SELECT id INTO tnt FROM tenants LIMIT 1;
    IF tnt IS NULL THEN
        RAISE NOTICE 'No tenant found; skipping seed-transactions.';
        RETURN;
    END IF;

    SELECT EXISTS (SELECT 1 FROM customer_transactions WHERE tenant_id = tnt) INTO has_customer_txn;
    SELECT EXISTS (SELECT 1 FROM stock_transactions WHERE tenant_id = tnt) INTO has_stock_txn;

    IF has_customer_txn OR has_stock_txn THEN
        RAISE NOTICE 'Transactions already seeded for tenant %; skipping (idempotent guard).', tnt;
        RETURN;
    END IF;

    ------------------------------------------------------------
    -- STOCK TRANSACTIONS
    -- Initial = current_stock + sum(sales) - sum(cancellation reversals)
    ------------------------------------------------------------
    FOR r IN SELECT id, stock_quantity, created_at_utc FROM products WHERE tenant_id = tnt LOOP
        SELECT
            COALESCE(SUM(CASE WHEN o2.status::text IN ('Confirmed','Shipped','Closed') THEN ol2.quantity ELSE 0 END),0)
            - COALESCE(SUM(CASE WHEN o2.status::text = 'Cancelled' THEN ol2.quantity ELSE 0 END),0)
        INTO initial_qty
        FROM order_lines ol2
        JOIN orders o2 ON o2.id = ol2.order_id
        WHERE o2.tenant_id = tnt AND ol2.product_id = r.id;

        initial_qty := r.stock_quantity + initial_qty;
        running := initial_qty;

        INSERT INTO stock_transactions (id, tenant_id, product_id, occurred_at_utc, type, quantity, balance_after, order_id, reference, notes, created_at_utc, updated_at_utc)
        VALUES (gen_random_uuid(), tnt, r.id, r.created_at_utc, 'Initial', initial_qty, running, NULL, 'INITIAL', 'Initial stock entry', r.created_at_utc, r.created_at_utc);

        FOR o IN
            SELECT ol.quantity, ord.id AS order_id, ord.order_number, ord.status::text AS status, ord.order_date
            FROM order_lines ol
            JOIN orders ord ON ord.id = ol.order_id
            WHERE ord.tenant_id = tnt AND ol.product_id = r.id
              AND ord.status::text IN ('Confirmed','Shipped','Closed','Cancelled')
            ORDER BY ord.order_date
        LOOP
            IF o.status IN ('Confirmed','Shipped','Closed') THEN
                running := running - o.quantity;
                INSERT INTO stock_transactions (id, tenant_id, product_id, occurred_at_utc, type, quantity, balance_after, order_id, reference, notes, created_at_utc, updated_at_utc)
                VALUES (gen_random_uuid(), tnt, r.id, o.order_date, 'Sale', -o.quantity, running, o.order_id, o.order_number, 'Order confirmed', o.order_date, o.order_date);
            ELSIF o.status = 'Cancelled' THEN
                -- Cancelled orders in the seed had no stock effect (cancelled while Draft historically).
                -- Skip to keep balance consistent with current stock_quantity.
                NULL;
            END IF;
        END LOOP;
    END LOOP;

    ------------------------------------------------------------
    -- CUSTOMER TRANSACTIONS from invoices
    ------------------------------------------------------------
    FOR inv IN
        SELECT id, customer_id, currency, total, status::text AS status, issue_date, paid_at_utc, cancelled_at_utc, order_id, invoice_number
        FROM invoices WHERE tenant_id = tnt ORDER BY issue_date
    LOOP
        IF inv.status IN ('Issued','Paid','Cancelled') THEN
            INSERT INTO customer_transactions (id, tenant_id, customer_id, occurred_at_utc, type, amount, currency, invoice_id, order_id, reference, notes, created_at_utc, updated_at_utc)
            VALUES (gen_random_uuid(), tnt, inv.customer_id, inv.issue_date, 'InvoiceIssued', inv.total, inv.currency, inv.id, inv.order_id, inv.invoice_number, NULL, inv.issue_date, inv.issue_date);
        END IF;
        IF inv.status = 'Paid' THEN
            INSERT INTO customer_transactions (id, tenant_id, customer_id, occurred_at_utc, type, amount, currency, invoice_id, order_id, reference, notes, created_at_utc, updated_at_utc)
            VALUES (gen_random_uuid(), tnt, inv.customer_id, inv.paid_at_utc, 'Payment', -inv.total, inv.currency, inv.id, inv.order_id, inv.invoice_number, 'Invoice paid', inv.paid_at_utc, inv.paid_at_utc);
        END IF;
        IF inv.status = 'Cancelled' AND inv.cancelled_at_utc IS NOT NULL THEN
            INSERT INTO customer_transactions (id, tenant_id, customer_id, occurred_at_utc, type, amount, currency, invoice_id, order_id, reference, notes, created_at_utc, updated_at_utc)
            VALUES (gen_random_uuid(), tnt, inv.customer_id, inv.cancelled_at_utc, 'Adjustment', -inv.total, inv.currency, inv.id, inv.order_id, inv.invoice_number, 'Invoice cancelled (voided)', inv.cancelled_at_utc, inv.cancelled_at_utc);
        END IF;
    END LOOP;

    RAISE NOTICE 'Transaction backfill complete.';
END $$;

SELECT 'customer_transactions' AS table_name, COUNT(*) FROM customer_transactions
UNION ALL SELECT 'stock_transactions', COUNT(*) FROM stock_transactions;
