export type DashboardWidgetType =
  | 'LineChart'
  | 'BarChart'
  | 'StatCard'
  | 'Table'
  | 'Calendar'
  | 'PieChart'
  | 'AreaChart';

export type BIDataSource = 'Sales' | 'Inventory' | 'Warranty' | 'Service' | 'Cash' | 'AR' | 'AP';

export type BIExportFormat = 'Pdf' | 'Xlsx' | 'Csv';

export interface BIQueryFilter {
  field: string;
  operator: string;
  value?: string | null;
  value2?: string | null;
}

export interface BIQueryConfig {
  groupBy?: string | null;
  aggregation?: string | null;
  measureField?: string | null;
  fromUtc?: string | null;
  toUtc?: string | null;
  filters?: BIQueryFilter[];
  limit?: number | null;
}

export interface BIResultColumn {
  key: string;
  label: string;
  dataType: string;
}

export interface BIResult {
  columns: BIResultColumn[];
  rows: Array<Record<string, unknown>>;
  totalRowCount: number;
}

export interface DashboardWidget {
  id: string;
  userId: string | null;
  title: string;
  type: DashboardWidgetType;
  dataSource: BIDataSource;
  queryConfigJson: string;
  gridX: number;
  gridY: number;
  width: number;
  height: number;
  displayOrder: number;
  isActive: boolean;
}

export interface DashboardWidgetUpsert {
  id?: string | null;
  title: string;
  type: DashboardWidgetType;
  dataSource: BIDataSource;
  queryConfigJson: string;
  gridX: number;
  gridY: number;
  width: number;
  height: number;
  displayOrder: number;
}

export interface SavedReport {
  id: string;
  ownerUserId: string;
  name: string;
  description?: string | null;
  dataSource: BIDataSource;
  queryConfigJson: string;
  isPublic: boolean;
  lastRunAtUtc?: string | null;
  lastRunRowCount?: number | null;
}

export interface SavedReportUpsert {
  id?: string | null;
  name: string;
  description?: string | null;
  dataSource: BIDataSource;
  queryConfigJson: string;
  isPublic: boolean;
}
