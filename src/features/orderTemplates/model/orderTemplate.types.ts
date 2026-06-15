export type OrderFrequency = 'None' | 'Daily' | 'Weekly' | 'BiWeekly' | 'Monthly' | 'Quarterly';

export interface OrderTemplateLine {
  id: string;
  lineNumber: number;
  productId: string;
  productSku: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  notes?: string | null;
}

export interface OrderTemplate {
  id: string;
  name: string;
  customerId: string;
  currency: string;
  priceListId?: string | null;
  frequency: OrderFrequency;
  nextRunAtUtc?: string | null;
  lastRunAtUtc?: string | null;
  isActive: boolean;
  createdByUserId: string;
  notes?: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
  lines: OrderTemplateLine[];
}

export interface OrderTemplateLineInput {
  productId: string;
  quantity: number;
  unitPrice: number;
  notes?: string | null;
}

export interface CreateOrderTemplateInput {
  name: string;
  customerId: string;
  currency: string;
  frequency: OrderFrequency;
  firstRunAtUtc?: string | null;
  priceListId?: string | null;
  notes?: string | null;
  lines: OrderTemplateLineInput[];
}

export interface UpdateOrderTemplateInput {
  id: string;
  name: string;
  customerId: string;
  currency: string;
  frequency: OrderFrequency;
  nextRunAtUtc?: string | null;
  priceListId?: string | null;
  notes?: string | null;
  isActive: boolean;
  lines: OrderTemplateLineInput[];
}

export interface OrderTemplateListParams {
  page?: number;
  pageSize?: number;
  customerId?: string;
}
