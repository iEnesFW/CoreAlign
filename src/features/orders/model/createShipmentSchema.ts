import { z } from 'zod';

export const createShipmentSchema = z.object({
  warehouseId: z.string().min(1, { message: 'Validation.Required' }),
  notes: z.string().max(500, { message: 'Validation.TooLong' }).optional().or(z.literal('')),
  lines: z
    .array(
      z.object({
        orderLineId: z.string().min(1),
        selected: z.boolean(),
        quantity: z.number({ message: 'Validation.Required' }),
        available: z.number(),
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
        if (line.quantity > line.available) {
          ctx.addIssue({
            code: 'custom',
            path: [index, 'quantity'],
            message: 'orders.shipments.exceedsAvailable',
          });
        }
      });
      if (!anySelected) {
        ctx.addIssue({ code: 'custom', path: [], message: 'orders.shipments.selectAtLeastOne' });
      }
    }),
});

export type CreateShipmentFormValues = z.infer<typeof createShipmentSchema>;
export type ShipmentLineFormValues = CreateShipmentFormValues['lines'][number];
