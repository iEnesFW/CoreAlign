export type SalesBucket = 'Day' | 'Week' | 'Month';

export interface SalesPeriodPoint {
  periodKey: string;
  label: string;
  bucketStart: string;
  revenue: number;
  paid: number;
  invoiceCount: number;
}

export interface SalesByPeriodReport {
  fromUtc: string;
  toUtc: string;
  currency: string;
  points: SalesPeriodPoint[];
  totalRevenue: number;
  totalPaid: number;
  invoiceCount: number;
  customerCount: number;
}

export interface TopCustomerReportRow {
  customerId: string;
  name: string;
  code: string | null;
  currency: string;
  totalRevenue: number;
  totalPaid: number;
  outstanding: number;
  invoiceCount: number;
  orderCount: number;
  lastOrderAtUtc: string | null;
}

export interface TopProductReportRow {
  productId: string | null;
  productSku: string;
  productName: string;
  quantity: number;
  revenue: number;
  invoiceCount: number;
}

export interface CustomerAgingRow {
  customerId: string;
  customerName: string;
  currency: string;
  current: number;
  days1To30: number;
  days31To60: number;
  days61To90: number;
  daysOver90: number;
  totalOutstanding: number;
}

export interface AgingSummaryReport {
  currency: string;
  current: number;
  days1To30: number;
  days31To60: number;
  days61To90: number;
  daysOver90: number;
  totalOutstanding: number;
  customersWithBalance: number;
  byCustomer: CustomerAgingRow[];
}
