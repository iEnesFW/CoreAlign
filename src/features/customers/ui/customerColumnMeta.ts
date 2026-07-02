import type { TFunction } from 'i18next';
import type { ColumnMeta } from '@/shared/ui/DataTable/columnState';

export const CUSTOMER_COLUMN_KEYS = [
  'name',
  'contact',
  'balance',
  'creditUsage',
  'status',
] as const;

export const getCustomerColumnMeta = (t: TFunction): ColumnMeta[] => [
  { key: 'name', label: t('customers.columns.name') },
  { key: 'contact', label: t('customers.columns.email') },
  { key: 'balance', label: t('customers.columns.balance', { defaultValue: 'Bakiye' }) },
  { key: 'creditUsage', label: t('customers.columns.creditUsage', { defaultValue: 'Kredi' }) },
  { key: 'status', label: t('customers.columns.status') },
];
