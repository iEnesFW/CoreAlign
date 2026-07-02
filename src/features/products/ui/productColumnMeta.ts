import type { TFunction } from 'i18next';
import type { ColumnMeta } from '@/shared/ui/DataTable/columnState';

export const PRODUCT_COLUMN_KEYS = ['product', 'price', 'stock', 'status'] as const;

export const getProductColumnMeta = (t: TFunction): ColumnMeta[] => [
  { key: 'product', label: t('products.columns.name') },
  { key: 'price', label: t('products.columns.price') },
  { key: 'stock', label: t('products.columns.stock') },
  { key: 'status', label: t('products.columns.status') },
];
