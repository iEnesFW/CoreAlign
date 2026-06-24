Route: /dashboard/invoices

# Creating an invoice

There are two ways to create an invoice. To bill an existing order, open the Orders page, find the order and click "Generate invoice", then confirm the prompt — CoreAlign builds the invoice from the order's lines (POST /invoices/from-order/{orderId}).

To raise an invoice that is not tied to an order, open the Invoices page and click "New invoice" at the top right. Choose the customer, set the currency, issue date and due days, add line items (SKU, item name, quantity, unit price, tax rate), and click "Create" (POST /invoices/standalone).

A new invoice is issued immediately and posts to the customer ledger and accounting. From the invoice detail you can record a payment, print it, or issue a credit note.
