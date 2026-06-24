Route: /dashboard/invoices

# Issuing a credit note

A credit note reverses an issued invoice, in full or in part. There are two ways to issue one.

Directly from the invoice: open the Invoices page, open the invoice you want to credit, and click "Issue credit note". Tick the lines to credit, adjust the quantities, optionally add a reason, and click "Issue Credit Note" (POST /invoices/{id}/credit-notes). The action is available once an invoice has been issued and is not already cancelled or voided.

From a return: when you receive an approved return on the Returns page with "Automatically issue credit note" ticked, CoreAlign reverses the source invoice for the returned lines and issues the credit note for you. The credit note number and source invoice then appear in the return detail.
