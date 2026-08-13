import { z } from 'zod';

const positiveMm = z
  .number({ message: 'Validation.Required' })
  .gt(0, { message: 'Validation.Positive' });

export const receiveGlassPlatesSchema = z.object({
  productId: z.string().min(1, { message: 'Validation.Required' }),
  warehouseId: z.string().min(1, { message: 'Validation.Required' }),
  storageLocationId: z.string().optional().or(z.literal('')),
  unitCostPerM2: z.string().optional().or(z.literal('')),
  notes: z.string().max(200, { message: 'Validation.TooLong' }).optional().or(z.literal('')),
  plates: z
    .array(
      z.object({
        plateNumber: z
          .string()
          .min(1, { message: 'Validation.Required' })
          .max(60, { message: 'Validation.TooLong' }),
        widthMm: positiveMm,
        heightMm: positiveMm,
        thicknessMm: positiveMm,
      }),
    )
    .min(1, { message: 'Validation.AtLeastOneLine' })
    .superRefine((plates, ctx) => {
      // WHY duplicates are refused here and not left to the server: a plate number is the physical
      // label on the glass, so two identical ones in one receipt make the stock untraceable.
      const seen = new Map<string, number>();
      plates.forEach((plate, index) => {
        const key = plate.plateNumber.trim().toLocaleUpperCase('tr-TR');
        if (!key) return;
        const first = seen.get(key);
        if (first === undefined) {
          seen.set(key, index);
          return;
        }
        ctx.addIssue({
          code: 'custom',
          path: [index, 'plateNumber'],
          message: 'GlassPlates.receiveForm.duplicatePlateNumber',
        });
      });
    }),
});

export type ReceiveGlassPlatesFormValues = z.infer<typeof receiveGlassPlatesSchema>;
export type GlassPlateLineFormValues = ReceiveGlassPlatesFormValues['plates'][number];

export const plateAreaM2 = (widthMm?: number, heightMm?: number): number =>
  widthMm && heightMm && widthMm > 0 && heightMm > 0
    ? Math.round(((widthMm * heightMm) / 1_000_000) * 10000) / 10000
    : 0;
