import {
  Activity,
  BadgeDollarSign,
  BarChart3,
  Box,
  Boxes,
  Calculator,
  ClipboardList,
  Cog,
  CreditCard,
  Factory,
  FileText,
  FolderKanban,
  Landmark,
  LayoutDashboard,
  Package,
  ReceiptText,
  Settings,
  ShieldCheck,
  ShoppingCart,
  Sparkles,
  SquareStack,
  Store,
  Truck,
  UserRound,
  Users,
  Warehouse,
  Wrench,
} from 'lucide-react';
import type { LucideIcon } from 'lucide-react';

interface Props {
  iconKey?: string | null;
  size?: number;
  className?: string;
}

// The catalog stores a lucide-style key, so the map is keyed the same way. Legacy aliases
// (the pre-2026-08 catalog used domain words like "sales") are kept so an old row still renders.
const ICONS: Record<string, LucideIcon> = {
  'layout-dashboard': LayoutDashboard,
  dashboard: LayoutDashboard,
  'credit-card': CreditCard,
  billing: CreditCard,
  settings: Settings,
  users: Users,
  customers: Users,
  'shopping-cart': ShoppingCart,
  sales: ShoppingCart,
  orders: ShoppingCart,
  'folder-kanban': FolderKanban,
  'square-stack': SquareStack,
  truck: Truck,
  vendors: Truck,
  package: Package,
  purchasing: Package,
  box: Box,
  products: Box,
  warehouse: Warehouse,
  inventory: Boxes,
  factory: Factory,
  production: Factory,
  cog: Cog,
  'badge-dollar-sign': BadgeDollarSign,
  'shield-check': ShieldCheck,
  wrench: Wrench,
  calculator: Calculator,
  accounting: Calculator,
  landmark: Landmark,
  'bar-chart': BarChart3,
  reports: BarChart3,
  analytics: BarChart3,
  'file-text': FileText,
  invoices: FileText,
  sparkles: Sparkles,
  store: Store,
  'user-round': UserRound,
  activity: Activity,
  clipboard: ClipboardList,
  vendorbills: ReceiptText,
};

export const ModuleIcon = ({ iconKey, size, className }: Props) => {
  const Icon = ICONS[iconKey?.toLowerCase().trim() ?? ''] ?? Package;
  return <Icon size={size} className={className} />;
};
