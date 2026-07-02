export type DocumentSequenceType =
  | 'CustomerCode'
  | 'ProductSku'
  | 'OrderNumber'
  | 'InvoiceNumber'
  | 'CreditNoteNumber'
  | 'DebitNoteNumber'
  | 'PaymentNumber'
  | 'ShipmentNumber'
  | 'JournalNumber'
  | 'SubscriptionOrderNumber'
  | 'QuoteNumber'
  | 'ReturnRequestNumber'
  | 'PurchaseOrderNumber'
  | 'VendorPaymentNumber'
  | 'GlassProjectCode'
  | 'StockCountNumber'
  | 'PurchaseRequisitionNumber'
  | 'MrpPlanRunNumber'
  | 'GoodsReceiptNumber'
  | 'EmployeeNumber'
  | 'PayrollRunNumber'
  | 'PayslipNumber';

export interface DocumentSequenceConfig {
  type: DocumentSequenceType;
  prefix: string;
  padLength: number;
  format: string | null;
  currentYear: number;
  nextNumber: number;
  preview: string;
  isConfigured: boolean;
}

export interface ConfigureDocumentSequenceRequest {
  type: DocumentSequenceType;
  prefix: string;
  padLength: number;
  format: string | null;
  nextNumber: number;
}
