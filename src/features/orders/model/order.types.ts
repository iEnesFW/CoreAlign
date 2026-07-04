export type OrderStatus =
  | 'Draft'
  | 'Submitted'
  | 'Approved'
  | 'Allocated'
  | 'Picking'
  | 'Packed'
  | 'PartiallyShipped'
  | 'Shipped'
  | 'Delivered'
  | 'Closed'
  | 'Returned'
  | 'Cancelled'
  | 'Confirmed';

export const ORDER_STATUSES: OrderStatus[] = [
  'Draft',
  'Submitted',
  'Approved',
  'Allocated',
  'Picking',
  'Packed',
  'PartiallyShipped',
  'Shipped',
  'Delivered',
  'Closed',
  'Cancelled',
  'Returned',
];

export type OrderType = 'Standard' | 'Blanket' | 'Return' | 'Sample' | 'Internal';
export type OrderSource = 'Manual' | 'Web' | 'Api' | 'Edi' | 'Marketplace' | 'Phone' | 'InStore';

export type OrderLineStatus =
  | 'Pending'
  | 'Allocated'
  | 'PartiallyShipped'
  | 'Shipped'
  | 'Invoiced'
  | 'PartiallyReturned'
  | 'Returned'
  | 'Cancelled';

import type { AddressSnapshot, CustomerSnapshot } from '@/shared/model/documentSnapshot.types';

export type { AddressSnapshot, CustomerSnapshot };

export interface OrderLine {
  id: string;
  lineNumber: number;
  productId: string;
  productSku: string;
  productName: string;
  productDescription: string | null;
  uomId: string | null;
  uomCode: string | null;
  uomConversionFactor: number;
  quantity: number;
  quantityAllocated: number;
  quantityShipped: number;
  quantityInvoiced: number;
  quantityReturned: number;
  quantityCancelled: number;
  quantityRemainingToShip: number;
  quantityRemainingToInvoice: number;
  listPriceSnapshot: number;
  unitPrice: number;
  lineDiscountPercent: number;
  lineDiscountAmount: number;
  isManualPriceOverride: boolean;
  taxRateId: string | null;
  taxRatePercent: number;
  taxAmount: number;
  isTaxInclusive: boolean;
  withholdingRatePercent: number;
  withholdingAmount: number;
  lineSubtotal: number;
  lineNetAmount: number;
  lineTotal: number;
  unitCostSnapshot: number;
  warehouseId: string | null;
  status: OrderLineStatus;
  lineNotes: string | null;
}

export interface Order {
  id: string;
  orderNumber: string;
  type: OrderType;
  status: OrderStatus;
  source: OrderSource;
  customerId: string;
  customerName: string;
  billingAddressId: string | null;
  shippingAddressId: string | null;
  customerSnapshot: CustomerSnapshot | null;
  billingAddressSnapshot: AddressSnapshot | null;
  shippingAddressSnapshot: AddressSnapshot | null;
  orderDate: string;
  requestedDeliveryDate: string | null;
  promisedDeliveryDate: string | null;
  actualDeliveryDate: string | null;
  submittedAtUtc: string | null;
  approvedAtUtc: string | null;
  cancelledAtUtc: string | null;
  currency: string;
  exchangeRate: number;
  paymentTermsId: string | null;
  paymentTermsNetDaysSnapshot: number | null;
  dueDate: string | null;
  priceListId: string | null;
  subtotal: number;
  lineDiscountTotal: number;
  headerDiscountAmount: number;
  headerDiscountPercent: number;
  taxableTotal: number;
  taxTotal: number;
  withholdingTotal: number;
  shippingCost: number;
  roundingAdjustment: number;
  total: number;
  salesRepUserId: string | null;
  channel: string | null;
  approvedByUserId: string | null;
  originOrderId: string | null;
  cancelReason: string | null;
  internalNotes: string | null;
  customerNotes: string | null;
  notes: string | null;
  lines: OrderLine[];
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface OrderSummary {
  id: string;
  orderNumber: string;
  customerId: string;
  customerName: string;
  orderDate: string;
  status: OrderStatus;
  currency: string;
  total: number;
  invoiceId: string | null;
  invoiceNumber: string | null;
  shipmentId: string | null;
  shipmentNumber: string | null;
}

export interface OrderLineInput {
  productId: string;
  quantity: number;
  unitPrice: number;
  lineDiscountPercent?: number;
  lineDiscountAmount?: number;
  taxRatePercent?: number;
  taxRateId?: string | null;
  isTaxInclusive?: boolean;
  withholdingRatePercent?: number;
  uomId?: string | null;
  uomCode?: string | null;
  uomConversionFactor?: number;
  warehouseId?: string | null;
  lineNotes?: string | null;
  isManualPriceOverride?: boolean;
  unitCostSnapshot?: number;
}

export interface CreateOrderInput {
  orderNumber: string;
  customerId: string;
  orderDate: string;
  currency: string;
  notes?: string | null;
  lines: OrderLineInput[];
  type?: OrderType;
  source?: OrderSource;
  requestedDeliveryDate?: string | null;
  promisedDeliveryDate?: string | null;
  billingAddressId?: string | null;
  shippingAddressId?: string | null;
  paymentTermsId?: string | null;
  priceListId?: string | null;
  exchangeRate?: number;
  shippingCost?: number;
  headerDiscountPercent?: number;
  headerDiscountAmount?: number;
  salesRepUserId?: string | null;
  channel?: string | null;
  internalNotes?: string | null;
  customerNotes?: string | null;
  originOrderId?: string | null;
}

export interface UpdateOrderInput extends CreateOrderInput {
  id: string;
  status: OrderStatus;
}

export interface OrderListParams {
  page: number;
  pageSize: number;
  search?: string;
  customerId?: string;
}

export type ShipmentStatus =
  | 'Draft'
  | 'Picked'
  | 'Packed'
  | 'Dispatched'
  | 'Delivered'
  | 'Cancelled'
  | 'Returned';

export interface ShipmentLine {
  id: string;
  orderLineId: string;
  productId: string;
  productSku: string;
  productName: string;
  lotId: string | null;
  lotNumber: string | null;
  serialNumber: string | null;
  quantity: number;
  unitCostSnapshot: number;
  notes: string | null;
}

export interface Shipment {
  id: string;
  shipmentNumber: string;
  orderId: string;
  orderNumber: string | null;
  customerId: string;
  warehouseId: string;
  warehouseName: string | null;
  status: ShipmentStatus;
  createdDate: string;
  pickedAtUtc: string | null;
  packedAtUtc: string | null;
  dispatchedAtUtc: string | null;
  deliveredAtUtc: string | null;
  cancelledAtUtc: string | null;
  carrierName: string | null;
  trackingNumber: string | null;
  trackingUrl: string | null;
  shippingCost: number | null;
  receivedBy: string | null;
  shippingAddressSnapshot: AddressSnapshot | null;
  notes: string | null;
  cancelReason: string | null;
  lines: ShipmentLine[];
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface CreateShipmentInput {
  orderId: string;
  warehouseId: string;
  lines: {
    orderLineId: string;
    quantity: number;
    lotId?: string | null;
    serialNumber?: string | null;
    notes?: string | null;
  }[];
  notes?: string | null;
}

export interface DispatchShipmentInput {
  id: string;
  carrierName?: string | null;
  trackingNumber?: string | null;
  trackingUrl?: string | null;
  shippingCost?: number | null;
}

export interface DeliverShipmentInput {
  id: string;
  receivedBy?: string | null;
  deliveredAtUtc?: string | null;
}

export interface RecordOrderScrapInput {
  id: string;
  orderLineId: string;
  quantity: number;
  warehouseId: string;
  notes?: string | null;
}
