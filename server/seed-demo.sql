DO $$
DECLARE
    tnt uuid;
    usr uuid;
    role_id int;

    c_acme uuid := gen_random_uuid();
    c_globex uuid := gen_random_uuid();
    c_initech uuid := gen_random_uuid();
    c_soylent uuid := gen_random_uuid();
    c_stark uuid := gen_random_uuid();
    c_wayne uuid := gen_random_uuid();
    c_wonka uuid := gen_random_uuid();
    c_cyberdyne uuid := gen_random_uuid();

    p_w001 uuid := gen_random_uuid();
    p_w002 uuid := gen_random_uuid();
    p_h001 uuid := gen_random_uuid();
    p_h002 uuid := gen_random_uuid();
    p_g001 uuid := gen_random_uuid();
    p_g002 uuid := gen_random_uuid();
    p_g003 uuid := gen_random_uuid();
    p_c001 uuid := gen_random_uuid();
    p_c002 uuid := gen_random_uuid();
    p_s001 uuid := gen_random_uuid();
    p_s002 uuid := gen_random_uuid();
    p_x001 uuid := gen_random_uuid();

    o1 uuid := gen_random_uuid();
    o2 uuid := gen_random_uuid();
    o3 uuid := gen_random_uuid();
    o4 uuid := gen_random_uuid();
    o5 uuid := gen_random_uuid();
    o6 uuid := gen_random_uuid();
    o7 uuid := gen_random_uuid();
    o8 uuid := gen_random_uuid();
    o9 uuid := gen_random_uuid();
    o10 uuid := gen_random_uuid();
    o11 uuid := gen_random_uuid();
    o12 uuid := gen_random_uuid();

    i1 uuid := gen_random_uuid();
    i2 uuid := gen_random_uuid();
    i3 uuid := gen_random_uuid();
    i4 uuid := gen_random_uuid();
    i5 uuid := gen_random_uuid();
    i6 uuid := gen_random_uuid();
    i7 uuid := gen_random_uuid();
    i8 uuid := gen_random_uuid();

    now_utc timestamptz := now() AT TIME ZONE 'UTC';
BEGIN
    IF current_setting('corealign.allow_destructive_seed', true) IS DISTINCT FROM 'yes' THEN
        RAISE EXCEPTION 'seed-demo.sql is destructive (wipes customers/products/orders/invoices for the tenant). Run with: SET corealign.allow_destructive_seed = ''yes''; before executing.';
    END IF;

    SELECT id INTO tnt FROM tenants LIMIT 1;
    SELECT id INTO usr FROM users LIMIT 1;

    IF tnt IS NULL OR usr IS NULL THEN
        RAISE EXCEPTION 'No tenant or user found; aborting seed-demo.';
    END IF;

    DELETE FROM invoice_lines WHERE tenant_id = tnt;
    DELETE FROM invoices WHERE tenant_id = tnt;
    DELETE FROM order_lines WHERE tenant_id = tnt;
    DELETE FROM orders WHERE tenant_id = tnt;
    DELETE FROM products WHERE tenant_id = tnt;
    DELETE FROM customers WHERE tenant_id = tnt;

    ------------------------------------------------------------
    -- CUSTOMERS (8)
    ------------------------------------------------------------
    INSERT INTO customers (id, tenant_id, name, email, phone, tax_number, address, notes, is_active, created_at_utc, updated_at_utc) VALUES
      (c_acme,      tnt, 'Acme Industries',     'billing@acme.com',     '+1 415 555 0101', 'TX-100001', '1 Acme Plaza, San Francisco, CA',     'Long-term customer since 2024.', true, now_utc - interval '60 days', now_utc - interval '60 days'),
      (c_globex,    tnt, 'Globex Corporation',  'ar@globex.com',        '+1 212 555 0102', 'TX-100002', '500 Globex Ave, New York, NY',         'Net-30 terms.',                  true, now_utc - interval '55 days', now_utc - interval '55 days'),
      (c_initech,   tnt, 'Initech LLC',         'finance@initech.com',  '+1 512 555 0103', 'TX-100003', '742 Software Way, Austin, TX',         null,                              true, now_utc - interval '50 days', now_utc - interval '50 days'),
      (c_soylent,   tnt, 'Soylent Foods',       'orders@soylent.com',   '+1 213 555 0104', 'TX-100004', '888 Greenway, Los Angeles, CA',        null,                              true, now_utc - interval '45 days', now_utc - interval '45 days'),
      (c_stark,     tnt, 'Stark Industries',    'pepper@stark.com',     '+1 646 555 0105', 'TX-100005', 'Stark Tower, Manhattan, NY',           'VIP - priority shipping.',        true, now_utc - interval '40 days', now_utc - interval '40 days'),
      (c_wayne,     tnt, 'Wayne Enterprises',   'lucius@wayne.com',     '+1 312 555 0106', 'TX-100006', '1007 Mountain Dr, Gotham',            null,                              true, now_utc - interval '35 days', now_utc - interval '35 days'),
      (c_wonka,     tnt, 'Wonka Industries',    'orders@wonka.co.uk',   '+44 20 7946 0107','TX-100007', '50 Confection Ln, London',            'Bulk discount agreed.',           true, now_utc - interval '30 days', now_utc - interval '30 days'),
      (c_cyberdyne, tnt, 'Cyberdyne Systems',   'ap@cyberdyne.io',      '+1 408 555 0108', 'TX-100008', '18144 El Camino Real, Sunnyvale, CA', null,                              false, now_utc - interval '20 days', now_utc - interval '20 days');

    ------------------------------------------------------------
    -- PRODUCTS (12)
    ------------------------------------------------------------
    INSERT INTO products (id, tenant_id, sku, name, description, unit, price, currency, stock_quantity, is_active, created_at_utc, updated_at_utc) VALUES
      (p_w001, tnt, 'WGT-001', 'Widget Standard',         'Standard utility widget.',          'pcs', 12.50,   'USD', 240, true, now_utc - interval '50 days', now_utc - interval '50 days'),
      (p_w002, tnt, 'WGT-002', 'Widget Pro',              'Professional grade widget.',        'pcs', 29.90,   'USD', 78,  true, now_utc - interval '48 days', now_utc - interval '48 days'),
      (p_h001, tnt, 'HW-RTR-01','Router 1G',              'Gigabit edge router.',              'pcs', 149.00,  'USD', 42,  true, now_utc - interval '45 days', now_utc - interval '45 days'),
      (p_h002, tnt, 'HW-SWT-01','Switch 24p',             '24-port managed switch.',           'pcs', 399.00,  'USD', 12,  true, now_utc - interval '45 days', now_utc - interval '45 days'),
      (p_g001, tnt, 'GDT-101', 'Wireless Mouse',          'Bluetooth ergonomic mouse.',        'pcs', 18.75,   'USD', 320, true, now_utc - interval '42 days', now_utc - interval '42 days'),
      (p_g002, tnt, 'GDT-102', 'Mechanical Keyboard',     'TKL hot-swappable keyboard.',       'pcs', 89.00,   'USD', 56,  true, now_utc - interval '40 days', now_utc - interval '40 days'),
      (p_g003, tnt, 'GDT-103', 'USB-C Hub 7-in-1',        'Aluminium hub, 7 ports.',           'pcs', 34.50,   'USD', 4,   true, now_utc - interval '38 days', now_utc - interval '38 days'),
      (p_c001, tnt, 'CBL-USBC','USB-C Cable 2m',          'Braided 100W USB-C cable.',         'pcs', 9.90,    'USD', 880, true, now_utc - interval '35 days', now_utc - interval '35 days'),
      (p_c002, tnt, 'CBL-HDMI','HDMI 2.1 Cable 1.5m',     '8K compatible HDMI cable.',         'pcs', 14.25,   'USD', 0,   true, now_utc - interval '34 days', now_utc - interval '34 days'),
      (p_s001, tnt, 'SVC-DEPL','Deployment Service',      'Standard deployment package.',      'hour',180.00,  'USD', 0,   true, now_utc - interval '30 days', now_utc - interval '30 days'),
      (p_s002, tnt, 'SVC-SUPP','Premium Support / mo',    'Monthly premium support plan.',     'mo',  499.00,  'USD', 0,   true, now_utc - interval '30 days', now_utc - interval '30 days'),
      (p_x001, tnt, 'OBS-OLD', 'Legacy Adapter',          'Discontinued.',                     'pcs', 5.00,    'USD', 5,   false, now_utc - interval '25 days', now_utc - interval '25 days');

    ------------------------------------------------------------
    -- ORDERS (12) + LINES
    ------------------------------------------------------------
    -- Helper: insert order header then its lines, recalc total later

    -- O1: Acme - Closed (paid invoice)
    INSERT INTO orders (id, tenant_id, order_number, customer_id, order_date, status, currency, total, notes, created_at_utc, updated_at_utc)
      VALUES (o1, tnt, 'ORD-2026-0001', c_acme, now_utc - interval '27 days', 'Closed', 'USD', 0, 'Q2 hardware refresh.', now_utc - interval '27 days', now_utc - interval '27 days');
    INSERT INTO order_lines (id, tenant_id, order_id, product_id, product_sku, product_name, quantity, unit_price, created_at_utc, updated_at_utc) VALUES
      (gen_random_uuid(), tnt, o1, p_h001, 'HW-RTR-01', 'Router 1G',       4, 149.00, now_utc - interval '27 days', now_utc - interval '27 days'),
      (gen_random_uuid(), tnt, o1, p_h002, 'HW-SWT-01', 'Switch 24p',      2, 399.00, now_utc - interval '27 days', now_utc - interval '27 days'),
      (gen_random_uuid(), tnt, o1, p_c001, 'CBL-USBC',  'USB-C Cable 2m', 20, 9.90,   now_utc - interval '27 days', now_utc - interval '27 days');
    UPDATE orders SET total = (SELECT COALESCE(SUM(quantity * unit_price),0) FROM order_lines WHERE order_id = o1) WHERE id = o1;

    -- O2: Globex - Closed (paid invoice)
    INSERT INTO orders (id, tenant_id, order_number, customer_id, order_date, status, currency, total, notes, created_at_utc, updated_at_utc)
      VALUES (o2, tnt, 'ORD-2026-0002', c_globex, now_utc - interval '22 days', 'Closed', 'USD', 0, null, now_utc - interval '22 days', now_utc - interval '22 days');
    INSERT INTO order_lines (id, tenant_id, order_id, product_id, product_sku, product_name, quantity, unit_price, created_at_utc, updated_at_utc) VALUES
      (gen_random_uuid(), tnt, o2, p_g001, 'GDT-101', 'Wireless Mouse',      30, 18.75, now_utc - interval '22 days', now_utc - interval '22 days'),
      (gen_random_uuid(), tnt, o2, p_g002, 'GDT-102', 'Mechanical Keyboard', 15, 89.00, now_utc - interval '22 days', now_utc - interval '22 days');
    UPDATE orders SET total = (SELECT COALESCE(SUM(quantity * unit_price),0) FROM order_lines WHERE order_id = o2) WHERE id = o2;

    -- O3: Initech - Shipped (issued invoice)
    INSERT INTO orders (id, tenant_id, order_number, customer_id, order_date, status, currency, total, notes, created_at_utc, updated_at_utc)
      VALUES (o3, tnt, 'ORD-2026-0003', c_initech, now_utc - interval '17 days', 'Shipped', 'USD', 0, 'Net-30 terms.', now_utc - interval '17 days', now_utc - interval '17 days');
    INSERT INTO order_lines (id, tenant_id, order_id, product_id, product_sku, product_name, quantity, unit_price, created_at_utc, updated_at_utc) VALUES
      (gen_random_uuid(), tnt, o3, p_w001, 'WGT-001', 'Widget Standard', 100, 12.50, now_utc - interval '17 days', now_utc - interval '17 days'),
      (gen_random_uuid(), tnt, o3, p_w002, 'WGT-002', 'Widget Pro',      40, 29.90, now_utc - interval '17 days', now_utc - interval '17 days');
    UPDATE orders SET total = (SELECT COALESCE(SUM(quantity * unit_price),0) FROM order_lines WHERE order_id = o3) WHERE id = o3;

    -- O4: Stark - Closed (paid invoice)
    INSERT INTO orders (id, tenant_id, order_number, customer_id, order_date, status, currency, total, notes, created_at_utc, updated_at_utc)
      VALUES (o4, tnt, 'ORD-2026-0004', c_stark, now_utc - interval '14 days', 'Closed', 'USD', 0, 'VIP - rush.', now_utc - interval '14 days', now_utc - interval '14 days');
    INSERT INTO order_lines (id, tenant_id, order_id, product_id, product_sku, product_name, quantity, unit_price, created_at_utc, updated_at_utc) VALUES
      (gen_random_uuid(), tnt, o4, p_s001, 'SVC-DEPL', 'Deployment Service',     20, 180.00, now_utc - interval '14 days', now_utc - interval '14 days'),
      (gen_random_uuid(), tnt, o4, p_s002, 'SVC-SUPP', 'Premium Support / mo',   3,  499.00, now_utc - interval '14 days', now_utc - interval '14 days');
    UPDATE orders SET total = (SELECT COALESCE(SUM(quantity * unit_price),0) FROM order_lines WHERE order_id = o4) WHERE id = o4;

    -- O5: Wayne - Shipped (issued invoice)
    INSERT INTO orders (id, tenant_id, order_number, customer_id, order_date, status, currency, total, notes, created_at_utc, updated_at_utc)
      VALUES (o5, tnt, 'ORD-2026-0005', c_wayne, now_utc - interval '11 days', 'Shipped', 'USD', 0, null, now_utc - interval '11 days', now_utc - interval '11 days');
    INSERT INTO order_lines (id, tenant_id, order_id, product_id, product_sku, product_name, quantity, unit_price, created_at_utc, updated_at_utc) VALUES
      (gen_random_uuid(), tnt, o5, p_g002, 'GDT-102', 'Mechanical Keyboard', 25, 89.00, now_utc - interval '11 days', now_utc - interval '11 days'),
      (gen_random_uuid(), tnt, o5, p_g003, 'GDT-103', 'USB-C Hub 7-in-1',    25, 34.50, now_utc - interval '11 days', now_utc - interval '11 days'),
      (gen_random_uuid(), tnt, o5, p_c001, 'CBL-USBC','USB-C Cable 2m',      50, 9.90,  now_utc - interval '11 days', now_utc - interval '11 days');
    UPDATE orders SET total = (SELECT COALESCE(SUM(quantity * unit_price),0) FROM order_lines WHERE order_id = o5) WHERE id = o5;

    -- O6: Wonka - Closed (paid invoice)
    INSERT INTO orders (id, tenant_id, order_number, customer_id, order_date, status, currency, total, notes, created_at_utc, updated_at_utc)
      VALUES (o6, tnt, 'ORD-2026-0006', c_wonka, now_utc - interval '9 days', 'Closed', 'USD', 0, 'Bulk order.', now_utc - interval '9 days', now_utc - interval '9 days');
    INSERT INTO order_lines (id, tenant_id, order_id, product_id, product_sku, product_name, quantity, unit_price, created_at_utc, updated_at_utc) VALUES
      (gen_random_uuid(), tnt, o6, p_w001, 'WGT-001', 'Widget Standard', 250, 12.50, now_utc - interval '9 days', now_utc - interval '9 days');
    UPDATE orders SET total = (SELECT COALESCE(SUM(quantity * unit_price),0) FROM order_lines WHERE order_id = o6) WHERE id = o6;

    -- O7: Soylent - Confirmed
    INSERT INTO orders (id, tenant_id, order_number, customer_id, order_date, status, currency, total, notes, created_at_utc, updated_at_utc)
      VALUES (o7, tnt, 'ORD-2026-0007', c_soylent, now_utc - interval '7 days', 'Confirmed', 'USD', 0, null, now_utc - interval '7 days', now_utc - interval '7 days');
    INSERT INTO order_lines (id, tenant_id, order_id, product_id, product_sku, product_name, quantity, unit_price, created_at_utc, updated_at_utc) VALUES
      (gen_random_uuid(), tnt, o7, p_g001, 'GDT-101', 'Wireless Mouse',  60, 18.75, now_utc - interval '7 days', now_utc - interval '7 days'),
      (gen_random_uuid(), tnt, o7, p_c001, 'CBL-USBC','USB-C Cable 2m', 100, 9.90,  now_utc - interval '7 days', now_utc - interval '7 days');
    UPDATE orders SET total = (SELECT COALESCE(SUM(quantity * unit_price),0) FROM order_lines WHERE order_id = o7) WHERE id = o7;

    -- O8: Acme - Confirmed
    INSERT INTO orders (id, tenant_id, order_number, customer_id, order_date, status, currency, total, notes, created_at_utc, updated_at_utc)
      VALUES (o8, tnt, 'ORD-2026-0008', c_acme, now_utc - interval '5 days', 'Confirmed', 'USD', 0, null, now_utc - interval '5 days', now_utc - interval '5 days');
    INSERT INTO order_lines (id, tenant_id, order_id, product_id, product_sku, product_name, quantity, unit_price, created_at_utc, updated_at_utc) VALUES
      (gen_random_uuid(), tnt, o8, p_h001, 'HW-RTR-01', 'Router 1G',      2, 149.00, now_utc - interval '5 days', now_utc - interval '5 days'),
      (gen_random_uuid(), tnt, o8, p_w002, 'WGT-002',   'Widget Pro',    10, 29.90,  now_utc - interval '5 days', now_utc - interval '5 days');
    UPDATE orders SET total = (SELECT COALESCE(SUM(quantity * unit_price),0) FROM order_lines WHERE order_id = o8) WHERE id = o8;

    -- O9: Globex - Shipped (issued invoice)
    INSERT INTO orders (id, tenant_id, order_number, customer_id, order_date, status, currency, total, notes, created_at_utc, updated_at_utc)
      VALUES (o9, tnt, 'ORD-2026-0009', c_globex, now_utc - interval '3 days', 'Shipped', 'USD', 0, null, now_utc - interval '3 days', now_utc - interval '3 days');
    INSERT INTO order_lines (id, tenant_id, order_id, product_id, product_sku, product_name, quantity, unit_price, created_at_utc, updated_at_utc) VALUES
      (gen_random_uuid(), tnt, o9, p_s002, 'SVC-SUPP', 'Premium Support / mo', 12, 499.00, now_utc - interval '3 days', now_utc - interval '3 days');
    UPDATE orders SET total = (SELECT COALESCE(SUM(quantity * unit_price),0) FROM order_lines WHERE order_id = o9) WHERE id = o9;

    -- O10: Initech - Draft
    INSERT INTO orders (id, tenant_id, order_number, customer_id, order_date, status, currency, total, notes, created_at_utc, updated_at_utc)
      VALUES (o10, tnt, 'ORD-2026-0010', c_initech, now_utc - interval '2 days', 'Draft', 'USD', 0, 'Awaiting confirmation.', now_utc - interval '2 days', now_utc - interval '2 days');
    INSERT INTO order_lines (id, tenant_id, order_id, product_id, product_sku, product_name, quantity, unit_price, created_at_utc, updated_at_utc) VALUES
      (gen_random_uuid(), tnt, o10, p_w001, 'WGT-001', 'Widget Standard', 50, 12.50, now_utc - interval '2 days', now_utc - interval '2 days');
    UPDATE orders SET total = (SELECT COALESCE(SUM(quantity * unit_price),0) FROM order_lines WHERE order_id = o10) WHERE id = o10;

    -- O11: Stark - Draft
    INSERT INTO orders (id, tenant_id, order_number, customer_id, order_date, status, currency, total, notes, created_at_utc, updated_at_utc)
      VALUES (o11, tnt, 'ORD-2026-0011', c_stark, now_utc - interval '1 days', 'Draft', 'USD', 0, null, now_utc - interval '1 days', now_utc - interval '1 days');
    INSERT INTO order_lines (id, tenant_id, order_id, product_id, product_sku, product_name, quantity, unit_price, created_at_utc, updated_at_utc) VALUES
      (gen_random_uuid(), tnt, o11, p_g002, 'GDT-102', 'Mechanical Keyboard', 8, 89.00, now_utc - interval '1 days', now_utc - interval '1 days'),
      (gen_random_uuid(), tnt, o11, p_g003, 'GDT-103', 'USB-C Hub 7-in-1',    8, 34.50, now_utc - interval '1 days', now_utc - interval '1 days');
    UPDATE orders SET total = (SELECT COALESCE(SUM(quantity * unit_price),0) FROM order_lines WHERE order_id = o11) WHERE id = o11;

    -- O12: Cyberdyne - Cancelled
    INSERT INTO orders (id, tenant_id, order_number, customer_id, order_date, status, currency, total, notes, created_at_utc, updated_at_utc)
      VALUES (o12, tnt, 'ORD-2026-0012', c_cyberdyne, now_utc - interval '12 days', 'Cancelled', 'USD', 0, 'Customer cancelled.', now_utc - interval '12 days', now_utc - interval '12 days');
    INSERT INTO order_lines (id, tenant_id, order_id, product_id, product_sku, product_name, quantity, unit_price, created_at_utc, updated_at_utc) VALUES
      (gen_random_uuid(), tnt, o12, p_h002, 'HW-SWT-01', 'Switch 24p', 3, 399.00, now_utc - interval '12 days', now_utc - interval '12 days');
    UPDATE orders SET total = (SELECT COALESCE(SUM(quantity * unit_price),0) FROM order_lines WHERE order_id = o12) WHERE id = o12;

    ------------------------------------------------------------
    -- INVOICES (8) — mix of Paid / Issued / Cancelled
    ------------------------------------------------------------
    -- INV from O1 (Closed) — Paid
    INSERT INTO invoices (id, tenant_id, invoice_number, order_id, customer_id, customer_name_snapshot, issue_date, due_date, status, currency, subtotal, total, paid_at_utc, cancelled_at_utc, notes, created_at_utc, updated_at_utc)
      VALUES (i1, tnt, 'INV-20260415-A001', o1, c_acme, 'Acme Industries', now_utc - interval '25 days', now_utc + interval '5 days', 'Paid', 'USD', 0, 0, now_utc - interval '8 days', null, null, now_utc - interval '25 days', now_utc - interval '8 days');
    INSERT INTO invoice_lines (id, tenant_id, invoice_id, product_id, product_sku, product_name, quantity, unit_price, created_at_utc, updated_at_utc)
      SELECT gen_random_uuid(), tnt, i1, product_id, product_sku, product_name, quantity, unit_price, now_utc - interval '25 days', now_utc - interval '25 days' FROM order_lines WHERE order_id = o1;
    UPDATE invoices SET subtotal = (SELECT COALESCE(SUM(quantity * unit_price),0) FROM invoice_lines WHERE invoice_id = i1),
                        total    = (SELECT COALESCE(SUM(quantity * unit_price),0) FROM invoice_lines WHERE invoice_id = i1) WHERE id = i1;

    -- INV from O2 (Closed) — Paid (this month)
    INSERT INTO invoices (id, tenant_id, invoice_number, order_id, customer_id, customer_name_snapshot, issue_date, due_date, status, currency, subtotal, total, paid_at_utc, cancelled_at_utc, notes, created_at_utc, updated_at_utc)
      VALUES (i2, tnt, 'INV-20260420-B001', o2, c_globex, 'Globex Corporation', now_utc - interval '20 days', now_utc + interval '10 days', 'Paid', 'USD', 0, 0, now_utc - interval '6 days', null, null, now_utc - interval '20 days', now_utc - interval '6 days');
    INSERT INTO invoice_lines (id, tenant_id, invoice_id, product_id, product_sku, product_name, quantity, unit_price, created_at_utc, updated_at_utc)
      SELECT gen_random_uuid(), tnt, i2, product_id, product_sku, product_name, quantity, unit_price, now_utc - interval '20 days', now_utc - interval '20 days' FROM order_lines WHERE order_id = o2;
    UPDATE invoices SET subtotal = (SELECT COALESCE(SUM(quantity * unit_price),0) FROM invoice_lines WHERE invoice_id = i2),
                        total    = (SELECT COALESCE(SUM(quantity * unit_price),0) FROM invoice_lines WHERE invoice_id = i2) WHERE id = i2;

    -- INV from O3 (Shipped) — Issued (outstanding)
    INSERT INTO invoices (id, tenant_id, invoice_number, order_id, customer_id, customer_name_snapshot, issue_date, due_date, status, currency, subtotal, total, paid_at_utc, cancelled_at_utc, notes, created_at_utc, updated_at_utc)
      VALUES (i3, tnt, 'INV-20260425-C001', o3, c_initech, 'Initech LLC', now_utc - interval '15 days', now_utc + interval '15 days', 'Issued', 'USD', 0, 0, null, null, 'Net-30.', now_utc - interval '15 days', now_utc - interval '15 days');
    INSERT INTO invoice_lines (id, tenant_id, invoice_id, product_id, product_sku, product_name, quantity, unit_price, created_at_utc, updated_at_utc)
      SELECT gen_random_uuid(), tnt, i3, product_id, product_sku, product_name, quantity, unit_price, now_utc - interval '15 days', now_utc - interval '15 days' FROM order_lines WHERE order_id = o3;
    UPDATE invoices SET subtotal = (SELECT COALESCE(SUM(quantity * unit_price),0) FROM invoice_lines WHERE invoice_id = i3),
                        total    = (SELECT COALESCE(SUM(quantity * unit_price),0) FROM invoice_lines WHERE invoice_id = i3) WHERE id = i3;

    -- INV from O4 (Closed) — Paid (this month)
    INSERT INTO invoices (id, tenant_id, invoice_number, order_id, customer_id, customer_name_snapshot, issue_date, due_date, status, currency, subtotal, total, paid_at_utc, cancelled_at_utc, notes, created_at_utc, updated_at_utc)
      VALUES (i4, tnt, 'INV-20260501-D001', o4, c_stark, 'Stark Industries', now_utc - interval '12 days', now_utc + interval '18 days', 'Paid', 'USD', 0, 0, now_utc - interval '4 days', null, null, now_utc - interval '12 days', now_utc - interval '4 days');
    INSERT INTO invoice_lines (id, tenant_id, invoice_id, product_id, product_sku, product_name, quantity, unit_price, created_at_utc, updated_at_utc)
      SELECT gen_random_uuid(), tnt, i4, product_id, product_sku, product_name, quantity, unit_price, now_utc - interval '12 days', now_utc - interval '12 days' FROM order_lines WHERE order_id = o4;
    UPDATE invoices SET subtotal = (SELECT COALESCE(SUM(quantity * unit_price),0) FROM invoice_lines WHERE invoice_id = i4),
                        total    = (SELECT COALESCE(SUM(quantity * unit_price),0) FROM invoice_lines WHERE invoice_id = i4) WHERE id = i4;

    -- INV from O5 (Shipped) — Issued (outstanding)
    INSERT INTO invoices (id, tenant_id, invoice_number, order_id, customer_id, customer_name_snapshot, issue_date, due_date, status, currency, subtotal, total, paid_at_utc, cancelled_at_utc, notes, created_at_utc, updated_at_utc)
      VALUES (i5, tnt, 'INV-20260503-E001', o5, c_wayne, 'Wayne Enterprises', now_utc - interval '10 days', now_utc + interval '20 days', 'Issued', 'USD', 0, 0, null, null, null, now_utc - interval '10 days', now_utc - interval '10 days');
    INSERT INTO invoice_lines (id, tenant_id, invoice_id, product_id, product_sku, product_name, quantity, unit_price, created_at_utc, updated_at_utc)
      SELECT gen_random_uuid(), tnt, i5, product_id, product_sku, product_name, quantity, unit_price, now_utc - interval '10 days', now_utc - interval '10 days' FROM order_lines WHERE order_id = o5;
    UPDATE invoices SET subtotal = (SELECT COALESCE(SUM(quantity * unit_price),0) FROM invoice_lines WHERE invoice_id = i5),
                        total    = (SELECT COALESCE(SUM(quantity * unit_price),0) FROM invoice_lines WHERE invoice_id = i5) WHERE id = i5;

    -- INV from O6 (Closed) — Paid (this month)
    INSERT INTO invoices (id, tenant_id, invoice_number, order_id, customer_id, customer_name_snapshot, issue_date, due_date, status, currency, subtotal, total, paid_at_utc, cancelled_at_utc, notes, created_at_utc, updated_at_utc)
      VALUES (i6, tnt, 'INV-20260504-F001', o6, c_wonka, 'Wonka Industries', now_utc - interval '8 days', now_utc + interval '22 days', 'Paid', 'USD', 0, 0, now_utc - interval '2 days', null, null, now_utc - interval '8 days', now_utc - interval '2 days');
    INSERT INTO invoice_lines (id, tenant_id, invoice_id, product_id, product_sku, product_name, quantity, unit_price, created_at_utc, updated_at_utc)
      SELECT gen_random_uuid(), tnt, i6, product_id, product_sku, product_name, quantity, unit_price, now_utc - interval '8 days', now_utc - interval '8 days' FROM order_lines WHERE order_id = o6;
    UPDATE invoices SET subtotal = (SELECT COALESCE(SUM(quantity * unit_price),0) FROM invoice_lines WHERE invoice_id = i6),
                        total    = (SELECT COALESCE(SUM(quantity * unit_price),0) FROM invoice_lines WHERE invoice_id = i6) WHERE id = i6;

    -- INV from O9 (Shipped) — Issued (outstanding, recent)
    INSERT INTO invoices (id, tenant_id, invoice_number, order_id, customer_id, customer_name_snapshot, issue_date, due_date, status, currency, subtotal, total, paid_at_utc, cancelled_at_utc, notes, created_at_utc, updated_at_utc)
      VALUES (i7, tnt, 'INV-20260509-G001', o9, c_globex, 'Globex Corporation', now_utc - interval '2 days', now_utc + interval '28 days', 'Issued', 'USD', 0, 0, null, null, null, now_utc - interval '2 days', now_utc - interval '2 days');
    INSERT INTO invoice_lines (id, tenant_id, invoice_id, product_id, product_sku, product_name, quantity, unit_price, created_at_utc, updated_at_utc)
      SELECT gen_random_uuid(), tnt, i7, product_id, product_sku, product_name, quantity, unit_price, now_utc - interval '2 days', now_utc - interval '2 days' FROM order_lines WHERE order_id = o9;
    UPDATE invoices SET subtotal = (SELECT COALESCE(SUM(quantity * unit_price),0) FROM invoice_lines WHERE invoice_id = i7),
                        total    = (SELECT COALESCE(SUM(quantity * unit_price),0) FROM invoice_lines WHERE invoice_id = i7) WHERE id = i7;

    -- INV stand-alone — Cancelled (was for Acme, then voided)
    INSERT INTO invoices (id, tenant_id, invoice_number, order_id, customer_id, customer_name_snapshot, issue_date, due_date, status, currency, subtotal, total, paid_at_utc, cancelled_at_utc, notes, created_at_utc, updated_at_utc)
      VALUES (i8, tnt, 'INV-20260418-H001', null, c_acme, 'Acme Industries', now_utc - interval '24 days', now_utc + interval '6 days', 'Cancelled', 'USD', 250.00, 250.00, null, now_utc - interval '20 days', 'Duplicate, voided.', now_utc - interval '24 days', now_utc - interval '20 days');
    INSERT INTO invoice_lines (id, tenant_id, invoice_id, product_id, product_sku, product_name, quantity, unit_price, created_at_utc, updated_at_utc) VALUES
      (gen_random_uuid(), tnt, i8, p_w001, 'WGT-001', 'Widget Standard', 20, 12.50, now_utc - interval '24 days', now_utc - interval '24 days');

    RAISE NOTICE 'Seed completed for tenant %', tnt;
END $$;

SELECT 'customers' AS table_name, COUNT(*) FROM customers
UNION ALL SELECT 'products', COUNT(*) FROM products
UNION ALL SELECT 'orders', COUNT(*) FROM orders
UNION ALL SELECT 'order_lines', COUNT(*) FROM order_lines
UNION ALL SELECT 'invoices', COUNT(*) FROM invoices
UNION ALL SELECT 'invoice_lines', COUNT(*) FROM invoice_lines;
