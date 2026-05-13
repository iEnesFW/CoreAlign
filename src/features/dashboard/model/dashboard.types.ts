import type { OrderSummary } from '@/features/orders/model/order.types';

export interface LowStockProduct {
  id: string;
  sku: string;
  name: string;
  stockQuantity: number;
  unit: string;
}

export interface SalesTrendPoint {
  date: string;
  total: number;
}

export interface DashboardStats {
  customerCount: number;
  activeProductCount: number;
  orderCountByStatus: Record<string, number>;
  totalOrderCount: number;
  totalSales: number;
  lowStockProducts: LowStockProduct[];
  recentOrders: OrderSummary[];
  salesTrend: SalesTrendPoint[];
  outstandingReceivables: number;
  collectedThisMonth: number;
  openInvoiceCount: number;
}
