import { useMemo } from 'react';
import type { NavigateFunction } from 'react-router-dom';
import {
  BarChart3,
  Book,
  Box,
  CreditCard,
  FileText,
  LayoutDashboard,
  Package,
  Receipt,
  Scale,
  Settings,
  ShoppingCart,
  Sliders,
  TrendingUp,
  User,
  Users,
} from 'lucide-react';
import type { CommandItem } from '@/shared/ui/CommandPalette/CommandPalette';

export const useCommandItems = (navigate: NavigateFunction): CommandItem[] =>
  useMemo(() => {
    const go = (path: string) => () => navigate(path);
    return [
      {
        id: 'dashboard',
        label: 'Dashboard',
        keywords: 'home ana sayfa',
        icon: LayoutDashboard,
        onSelect: go('/dashboard'),
      },
      {
        id: 'customers',
        label: 'Customers',
        keywords: 'müşteri cari',
        icon: Users,
        onSelect: go('/dashboard/customers'),
      },
      {
        id: 'orders',
        label: 'Orders',
        keywords: 'sipariş',
        icon: ShoppingCart,
        onSelect: go('/dashboard/orders'),
      },
      {
        id: 'invoices',
        label: 'Invoices',
        keywords: 'fatura',
        icon: FileText,
        onSelect: go('/dashboard/invoices'),
      },
      {
        id: 'vendors',
        label: 'Vendors',
        keywords: 'tedarikçi satıcı',
        icon: Box,
        onSelect: go('/dashboard/vendors'),
      },
      {
        id: 'products',
        label: 'Products',
        keywords: 'ürün stok inventory',
        icon: Package,
        onSelect: go('/dashboard/products'),
      },
      {
        id: 'coa',
        label: 'Chart of Accounts',
        keywords: 'hesap planı muhasebe',
        icon: Book,
        onSelect: go('/dashboard/accounting/chart-of-accounts'),
      },
      {
        id: 'journal',
        label: 'Journal Entries',
        keywords: 'yevmiye fiş',
        icon: Receipt,
        onSelect: go('/dashboard/accounting/journal-entries'),
      },
      {
        id: 'trial-balance',
        label: 'Trial Balance',
        keywords: 'mizan',
        icon: CreditCard,
        onSelect: go('/dashboard/accounting/trial-balance'),
      },
      {
        id: 'balance-sheet',
        label: 'Balance Sheet',
        keywords: 'bilanço',
        icon: Scale,
        onSelect: go('/dashboard/accounting/balance-sheet'),
      },
      {
        id: 'income-statement',
        label: 'Income Statement',
        keywords: 'gelir tablosu kâr zarar',
        icon: TrendingUp,
        onSelect: go('/dashboard/accounting/income-statement'),
      },
      {
        id: 'periods',
        label: 'Accounting Periods',
        keywords: 'dönem',
        icon: CreditCard,
        onSelect: go('/dashboard/accounting/periods'),
      },
      {
        id: 'reports',
        label: 'Reports',
        keywords: 'rapor analiz',
        icon: BarChart3,
        onSelect: go('/dashboard/reports'),
      },
      {
        id: 'settings',
        label: 'Settings',
        keywords: 'ayar yönetim',
        icon: Settings,
        onSelect: go('/dashboard/settings'),
      },
      {
        id: 'profile',
        label: 'Profile',
        keywords: 'profil hesabım',
        icon: User,
        onSelect: go('/dashboard/profile'),
      },
      {
        id: 'activity',
        label: 'Activity',
        keywords: 'aktivite log',
        icon: Sliders,
        onSelect: go('/dashboard/activity'),
      },
    ];
  }, [navigate]);
