Route: /dashboard/inventory

# Transferring stock between warehouses

Go to the Stock page from the sidebar. Click the Transfer voucher button in the page header to open the Stock transfer voucher window.

Choose the From warehouse and the To warehouse (they must differ). On each line, enter the product and the transfer quantity; fill the Document # / reference field if needed. Use Add line for multiple products.

Click Post voucher. The system records an issue from the source warehouse and a receipt into the destination warehouse (POST /api/v1/stock/transfer); total stock quantity and value stay the same, only the location changes.
