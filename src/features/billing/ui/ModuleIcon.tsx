import {
  Activity,
  BarChart3,
  Boxes,
  Box,
  ClipboardList,
  CreditCard,
  FileText,
  Factory,
  LayoutDashboard,
  Package,
  ReceiptText,
  ShoppingCart,
  Settings,
  Users,
  Warehouse,
} from 'lucide-react';

interface Props {
  iconKey?: string | null;
  size?: number;
  className?: string;
}

export const ModuleIcon = ({ iconKey, size, className }: Props) => {
  const key = iconKey?.toLowerCase().trim();
  switch (key) {
    case 'dashboard':
      return <LayoutDashboard size={size} className={className} />;
    case 'sales':
    case 'orders':
      return <ShoppingCart size={size} className={className} />;
    case 'customers':
      return <Users size={size} className={className} />;
    case 'invoices':
      return <FileText size={size} className={className} />;
    case 'products':
      return <Package size={size} className={className} />;
    case 'inventory':
      return <Boxes size={size} className={className} />;
    case 'warehouse':
      return <Warehouse size={size} className={className} />;
    case 'purchasing':
    case 'vendors':
      return <Box size={size} className={className} />;
    case 'vendorbills':
      return <ReceiptText size={size} className={className} />;
    case 'accounting':
    case 'billing':
      return <CreditCard size={size} className={className} />;
    case 'reports':
    case 'analytics':
      return <BarChart3 size={size} className={className} />;
    case 'activity':
      return <Activity size={size} className={className} />;
    case 'production':
      return <Factory size={size} className={className} />;
    case 'settings':
      return <Settings size={size} className={className} />;
    case 'clipboard':
      return <ClipboardList size={size} className={className} />;
    default:
      return <Package size={size} className={className} />;
  }
};
