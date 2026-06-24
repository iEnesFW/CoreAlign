export interface StatementLineDto {
  accountId: string;
  accountCode: string;
  accountName: string;
  amount: number;
}

export interface StatementSectionDto {
  lines: StatementLineDto[];
  total: number;
}

export interface BalanceSheetReportDto {
  asOf: string;
  assets: StatementSectionDto;
  liabilities: StatementSectionDto;
  equity: StatementSectionDto;
  currentYearEarnings: number;
  retainedPriorEarnings: number;
  totalLiabilitiesAndEquity: number;
  isBalanced: boolean;
  variance: number;
}

export interface IncomeStatementReportDto {
  fromDate: string;
  toDate: string;
  revenue: StatementSectionDto;
  cogs: StatementSectionDto;
  opex: StatementSectionDto;
  grossProfit: number;
  netIncome: number;
}

export interface ReconciliationLineDto {
  controlCode: string;
  controlName: string;
  subledger: string;
  glBalance: number;
  subledgerBalance: number;
  variance: number;
  isReconciled: boolean;
}

export interface ReconciliationReportDto {
  asOf: string;
  lines: ReconciliationLineDto[];
  allReconciled: boolean;
}
