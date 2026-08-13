import { z } from 'zod';

export type StockVoucherType = 'receive' | 'issue' | 'count' | 'transfer';

export const stockVoucherSchema = (type: StockVoucherType) =>
  z
    .object({
      warehouseId: z.string().min(1, { message: 'Validation.Required' }),
      toWarehouseId: z.string().optional().or(z.literal('')),
      reference: z.string().max(64, { message: 'Validation.TooLong' }).optional().or(z.literal('')),
      notes: z.string().max(200, { message: 'Validation.TooLong' }).optional().or(z.literal('')),
      lines: z
        .array(
          z.object({
            productId: z.string().min(1, { message: 'Validation.Required' }),
            // WHY count accepts zero while the others demand a positive number: counting a product
            // to nothing is a legitimate write-off, so a zero here must reach the adjustment.
            quantity:
              type === 'count'
                ? z
                    .number({ message: 'Validation.Required' })
                    .min(0, { message: 'Validation.NonNegative' })
                : z
                    .number({ message: 'Validation.Required' })
                    .gt(0, { message: 'Validation.Positive' }),
            unitCost: z
              .number({ message: 'Validation.Required' })
              .min(0, { message: 'Validation.NonNegative' })
              .optional(),
          }),
        )
        .min(1, { message: 'Validation.AtLeastOneLine' }),
    })
    .superRefine((value, ctx) => {
      if (type !== 'transfer') return;
      if (!value.toWarehouseId) {
        ctx.addIssue({
          code: 'custom',
          path: ['toWarehouseId'],
          message: 'Validation.Required',
        });
        return;
      }
      if (value.toWarehouseId === value.warehouseId) {
        ctx.addIssue({
          code: 'custom',
          path: ['toWarehouseId'],
          message: 'inventory.voucher.toWarehouseRequired',
        });
      }
    });

export type StockVoucherFormValues = z.infer<ReturnType<typeof stockVoucherSchema>>;
export type StockVoucherLineFormValues = StockVoucherFormValues['lines'][number];
