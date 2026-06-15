export type GLPostingKey =
  | 'AccountsReceivable'
  | 'SalesRevenue'
  | 'OutputVat'
  | 'Cash'
  | 'Bank'
  | 'AccountsPayable'
  | 'InputVat'
  | 'Inventory'
  | 'CostOfGoodsSold'
  | 'GoodsReceiptClearing'
  | 'PurchaseExpense'
  | 'InventoryWriteOff';

export interface GLPostingMapping {
  postingKey: GLPostingKey;
  key: string;
  effectiveCode: string;
  overrideCode: string | null;
  defaultCode: string | null;
  accountName: string | null;
  resolves: boolean;
}

export interface ConfigureGLPostingMapRequest {
  key: GLPostingKey;
  accountCode: string;
}
