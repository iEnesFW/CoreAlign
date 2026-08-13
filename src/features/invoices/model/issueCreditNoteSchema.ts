import { z } from 'zod';

export const issueCreditNoteSchema = z.object({
  reason: z.string().max(500, { message: 'Validation.TooLong' }).optional().or(z.literal('')),
  lines: z
    .array(
      z.object({
        invoiceLineId: z.string().min(1),
        selected: z.boolean(),
        quantity: z.number({ message: 'Validation.Required' }),
        remaining: z.number(),
      }),
    )
    .superRefine((lines, ctx) => {
      let anySelected = false;
      lines.forEach((line, index) => {
        if (!line.selected) return;
        anySelected = true;
        if (!(line.quantity > 0)) {
          ctx.addIssue({
            code: 'custom',
            path: [index, 'quantity'],
            message: 'Validation.Positive',
          });
          return;
        }
        if (line.quantity > line.remaining) {
          ctx.addIssue({
            code: 'custom',
            path: [index, 'quantity'],
            message: 'invoices.creditNote.exceedsRemaining',
          });
        }
      });
      if (!anySelected) {
        ctx.addIssue({ code: 'custom', path: [], message: 'invoices.creditNote.selectAtLeastOne' });
      }
    }),
});

export type IssueCreditNoteFormValues = z.infer<typeof issueCreditNoteSchema>;
export type CreditNoteLineFormValues = IssueCreditNoteFormValues['lines'][number];
