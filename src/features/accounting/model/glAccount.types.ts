export type AccountType =
  | 'Asset'
  | 'Liability'
  | 'Equity'
  | 'Revenue'
  | 'Expense'
  | 'CostOfGoodsSold'
  | 'Memorandum';

export type NormalSide = 'Debit' | 'Credit';

export interface GLAccount {
  id: string;
  code: string;
  name: string;
  description?: string | null;
  type: AccountType;
  normalSide: NormalSide;
  parentId?: string | null;
  parentCode?: string | null;
  level: number;
  isPostable: boolean;
  isActive: boolean;
  currency: string;
}

export interface CreateGLAccountRequest {
  code: string;
  name: string;
  type: AccountType;
  isPostable: boolean;
  parentId?: string | null;
  currency: string;
  description?: string | null;
}

export interface UpdateGLAccountRequest {
  id: string;
  name: string;
  description?: string | null;
  isPostable: boolean;
  currency: string;
}

export interface GLAccountListParams {
  type?: AccountType;
  isActive?: boolean;
  isPostable?: boolean;
  parentId?: string;
}
