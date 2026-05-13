import { z } from 'zod';

export const productComponentSchema = z.object({
  componentProductId: z.string().min(1, { message: 'Validation.Required' }),
  quantity: z.number().positive({ message: 'Validation.Positive' }),
  notes: z.string().max(500, { message: 'Validation.TooLong' }).optional().or(z.literal('')),
});

export type ProductComponentFormValues = z.infer<typeof productComponentSchema>;

export const emptyProductComponentForm: ProductComponentFormValues = {
  componentProductId: '',
  quantity: 1,
  notes: '',
};
