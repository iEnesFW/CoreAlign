import React, { useCallback, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { NavLink } from 'react-router-dom';
import { routePreloaders } from '@/app/router/routePrefetch';
import { useIsTenantAdmin } from '@/features/billing/hooks/useIsTenantAdmin';
import {
  LayoutDashboard,
  Users,
  X,
  Package,
  Boxes,
  ShoppingCart,
  ChevronDown,
  PanelLeftClose,
  PanelLeftOpen,
  Box,
  CreditCard,
  FileText,
  Activity,
  BarChart3,
  Settings,
  Plug,
  Factory,
  ShieldCheck,
  Wrench,
  FolderKanban,
  KeyRound,
  LayoutGrid,
  ClipboardList,
} from 'lucide-react';

interface SidebarProps {
  isOpen: boolean;
  setIsOpen: (isOpen: boolean) => void;
  isCollapsed: boolean;
  setIsCollapsed: (isCollapsed: boolean) => void;
}

type NavItem = {
  name?: string;
  labelKey?: string;
  href?: string;
  icon?: React.ComponentType<{ size?: number; className?: string }>;
  children?: { name: string; href: string }[];
  section?: string;
  tourAnchor?: string;
};

// Only routes that are actually wired in App.tsx appear here. New modules are
// added when their routes ship; never advertise a link before the page exists
// — clicking a dead link erodes user trust and bypasses the SPA's protected
// boundary.
const baseNavigation: NavItem[] = [
  { section: 'OVERVIEW' },
  { name: 'Dashboard', href: '/dashboard', icon: LayoutDashboard },

  { section: 'SALES & CRM' },
  { name: 'Customers', href: '/dashboard/customers', icon: Users },
  { name: 'Orders', href: '/dashboard/orders', icon: ShoppingCart },
  { name: 'Invoices', href: '/dashboard/invoices', icon: FileText },

  { section: 'PROJECTS' },
  {
    name: 'Projects',
    href: '/dashboard/glass-enclosure/projects',
    icon: FolderKanban,
    tourAnchor: 'sidebar-projects',
  },

  { section: 'PURCHASING' },
  { name: 'Vendors', href: '/dashboard/vendors', icon: Box },

  { section: 'INVENTORY' },
  { name: 'Products', href: '/dashboard/products', icon: Package },
  { name: 'Stock', href: '/dashboard/inventory', icon: Boxes },
  {
    name: 'Stock Counts',
    labelKey: 'Inventory.StockCounts.navLabel',
    href: '/dashboard/inventory/stock-counts',
    icon: ClipboardList,
    tourAnchor: 'sidebar-stock-counts',
  },

  { section: 'PRODUCTION' },
  { name: 'MRP', href: '/dashboard/mrp', icon: Factory, tourAnchor: 'sidebar-mrp' },
  {
    name: 'MRP Workbench',
    href: '/dashboard/mrp/workbench',
    icon: LayoutGrid,
    tourAnchor: 'sidebar-mrp-workbench',
  },

  { section: 'AFTER SALES' },
  {
    name: 'Warranty',
    href: '/dashboard/warranty/contracts',
    icon: ShieldCheck,
    tourAnchor: 'sidebar-warranty',
  },
  {
    name: 'Installation',
    href: '/dashboard/installation/acceptances',
    icon: Wrench,
    tourAnchor: 'sidebar-installation',
  },

  { section: 'ACCOUNTING' },
  {
    name: 'Accounting',
    icon: CreditCard,
    children: [
      { name: 'Chart of Accounts', href: '/dashboard/accounting/chart-of-accounts' },
      { name: 'Journal Entries', href: '/dashboard/accounting/journal-entries' },
      { name: 'Trial Balance (Mizan)', href: '/dashboard/accounting/trial-balance' },
      { name: 'Balance Sheet (Bilanço)', href: '/dashboard/accounting/balance-sheet' },
      { name: 'Income Statement (Gelir Tablosu)', href: '/dashboard/accounting/income-statement' },
      { name: 'Accounting Periods', href: '/dashboard/accounting/periods' },
    ],
  },

  { section: 'ANALYTICS' },
  { name: 'Reports', href: '/dashboard/reports', icon: BarChart3 },

  { section: 'SYSTEM' },
  { name: 'Activity Log', href: '/dashboard/activity', icon: Activity },
  { name: 'Settings', href: '/dashboard/settings', icon: Settings },
];

const adminNavigation: NavItem[] = [
  { section: 'ADMINISTRATION' },
  { name: 'Providers', href: '/dashboard/admin/providers', icon: Plug },
  { name: 'SSO Settings', href: '/dashboard/admin/providers/sso', icon: KeyRound },
];

const SidebarComponent: React.FC<SidebarProps> = ({
  isOpen,
  setIsOpen,
  isCollapsed,
  setIsCollapsed,
}) => {
  const { t } = useTranslation();
  const [expandedMenus, setExpandedMenus] = useState<string[]>(['Customers', 'Orders']);
  const isAdmin = useIsTenantAdmin();
  const navigation = useMemo<NavItem[]>(
    () => (isAdmin ? [...baseNavigation, ...adminNavigation] : baseNavigation),
    [isAdmin],
  );

  const prefetch = useCallback((href: string) => {
    const loader = routePreloaders[href];
    if (loader) {
      try {
        loader();
      } catch {
        /* prefetch failures are non-critical */
      }
    }
  }, []);

  const toggleMenu = (name: string) => {
    setExpandedMenus((prev) =>
      prev.includes(name) ? prev.filter((item) => item !== name) : [...prev, name],
    );
    if (isCollapsed) {
      setIsCollapsed(false);
    }
  };

  return (
    <>
      {/* Mobile backdrop */}
      {isOpen && (
        <div
          className="fixed inset-0 z-40 bg-slate-900/50 backdrop-blur-sm lg:hidden"
          onClick={() => setIsOpen(false)}
        />
      )}

      {/* Sidebar */}
      <aside
        className={`fixed inset-y-0 left-0 z-50 bg-white dark:bg-[#0B0F19] border-r border-slate-200/60 dark:border-slate-800/60 transform transition-all duration-300 ease-[cubic-bezier(0.4,0,0.2,1)] lg:translate-x-0 lg:static lg:inset-0 flex flex-col shadow-[4px_0_24px_rgba(0,0,0,0.02)] dark:shadow-[4px_0_24px_rgba(0,0,0,0.2)] ${isOpen ? 'translate-x-0' : '-translate-x-full'} ${isCollapsed ? 'lg:w-[60px]' : 'w-[240px]'}`}
      >
        {/* Logo Area */}
        <div className="flex items-center justify-between h-12 px-3 border-b border-slate-200/60 dark:border-slate-800/60 shrink-0">
          <div
            className={`flex items-center gap-2 font-bold text-sm text-slate-900 dark:text-white transition-opacity duration-300 ${isCollapsed ? 'lg:opacity-0 lg:w-0 lg:overflow-hidden' : 'opacity-100'}`}
          >
            <div className="w-6 h-6 bg-gradient-to-br from-indigo-500 to-purple-600 rounded-[5px] flex items-center justify-center text-white shadow-lg shadow-indigo-500/20">
              <Box size={14} className="text-white" />
            </div>
            <span className="tracking-tight">
              Nexus<span className="text-indigo-500">ERP</span>
            </span>
          </div>

          {/* Logo for collapsed state */}
          <div
            className={`hidden lg:flex items-center justify-center absolute left-0 w-full transition-opacity duration-300 ${isCollapsed ? 'opacity-100' : 'opacity-0 pointer-events-none'}`}
          >
            <div className="w-7 h-7 bg-gradient-to-br from-indigo-500 to-purple-600 rounded-[5px] flex items-center justify-center text-white shadow-lg shadow-indigo-500/20">
              <Box size={16} className="text-white" />
            </div>
          </div>

          <button
            onClick={() => setIsOpen(false)}
            className="lg:hidden text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 transition-colors"
          >
            <X size={16} />
          </button>
        </div>

        {/* Navigation */}
        <nav className="flex-1 px-[5px] py-[5px] space-y-[2px] overflow-y-auto overflow-x-hidden scrollbar-thin scrollbar-thumb-slate-200 dark:scrollbar-thumb-slate-800">
          {navigation.map((item, index) => {
            if (item.section) {
              return (
                <div
                  key={`section-${index}`}
                  className={`px-2 pt-3 pb-1 text-[9px] font-bold text-slate-400 dark:text-slate-500 tracking-wider transition-opacity duration-300 ${isCollapsed ? 'opacity-0 h-0 overflow-hidden pt-0 pb-0' : 'opacity-100'}`}
                >
                  {item.section}
                </div>
              );
            }

            const Icon = item.icon!;
            const hasChildren = item.children && item.children.length > 0;
            const isExpanded = expandedMenus.includes(item.name!);
            const label = item.labelKey ? t(item.labelKey, { defaultValue: item.name }) : item.name;

            if (hasChildren) {
              return (
                <div key={item.name} className="space-y-[2px]">
                  <button
                    onClick={() => toggleMenu(item.name!)}
                    className={`w-full flex items-center justify-between px-2 py-1.5 rounded-[5px] transition-all duration-200 group ${
                      isExpanded
                        ? 'bg-slate-50 dark:bg-slate-800/50 text-slate-900 dark:text-white'
                        : 'text-slate-500 dark:text-slate-400 hover:bg-slate-50 dark:hover:bg-slate-800/50 hover:text-slate-900 dark:hover:text-white'
                    }`}
                  >
                    <div className="flex items-center gap-2">
                      <Icon
                        size={16}
                        className={`transition-colors ${isExpanded ? 'text-indigo-500' : 'group-hover:text-indigo-500'}`}
                      />
                      <span
                        className={`font-medium text-xs transition-opacity duration-300 whitespace-nowrap ${isCollapsed ? 'lg:opacity-0 lg:w-0 lg:overflow-hidden' : 'opacity-100'}`}
                      >
                        {item.name}
                      </span>
                    </div>
                    {!isCollapsed && (
                      <ChevronDown
                        size={14}
                        className={`transition-transform duration-200 shrink-0 ${isExpanded ? 'rotate-180 text-indigo-500' : ''}`}
                      />
                    )}
                  </button>

                  {/* Accordion Content */}
                  <div
                    className={`overflow-hidden transition-all duration-300 ease-in-out ${isExpanded && !isCollapsed ? 'max-h-[400px] opacity-100 mt-[2px]' : 'max-h-0 opacity-0'}`}
                  >
                    <div className="pl-8 pr-2 py-0.5 space-y-[2px] relative before:absolute before:left-4 before:top-0 before:bottom-0 before:w-px before:bg-slate-200 dark:before:bg-slate-800">
                      {item.children?.map((child) => (
                        <NavLink
                          key={child.name}
                          to={child.href}
                          onMouseEnter={() => prefetch(child.href)}
                          onFocus={() => prefetch(child.href)}
                          className={({ isActive }) =>
                            `block px-2 py-1.5 rounded-[5px] text-[11px] font-medium transition-all duration-200 relative truncate ${
                              isActive
                                ? 'text-indigo-600 dark:text-indigo-400 bg-indigo-50/50 dark:bg-indigo-500/10 before:absolute before:-left-4 before:top-1/2 before:-translate-y-1/2 before:w-1.5 before:h-1.5 before:rounded-full before:bg-indigo-500'
                                : 'text-slate-500 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white hover:bg-slate-50 dark:hover:bg-slate-800/50'
                            }`
                          }
                          title={child.name}
                        >
                          {child.name}
                        </NavLink>
                      ))}
                    </div>
                  </div>
                </div>
              );
            }

            return (
              <NavLink
                key={item.name}
                to={item.href!}
                onMouseEnter={() => prefetch(item.href!)}
                onFocus={() => prefetch(item.href!)}
                data-tour={item.tourAnchor}
                className={({ isActive }) =>
                  `flex items-center gap-2 px-2 py-1.5 rounded-[5px] transition-all duration-200 group ${
                    isActive
                      ? 'bg-indigo-50 dark:bg-indigo-500/10 text-indigo-600 dark:text-indigo-400 font-medium'
                      : 'text-slate-500 dark:text-slate-400 hover:bg-slate-50 dark:hover:bg-slate-800/50 hover:text-slate-900 dark:hover:text-white'
                  }`
                }
              >
                {({ isActive }) => (
                  <>
                    <Icon
                      size={16}
                      className={isActive ? '' : 'group-hover:text-indigo-500 transition-colors'}
                    />
                    <span
                      className={`text-xs transition-opacity duration-300 whitespace-nowrap ${isCollapsed ? 'lg:opacity-0 lg:w-0 lg:overflow-hidden' : 'opacity-100'}`}
                    >
                      {label}
                    </span>
                  </>
                )}
              </NavLink>
            );
          })}
        </nav>

        {/* Sidebar Footer / Collapse Toggle */}
        <div className="p-2 border-t border-slate-200/60 dark:border-slate-800/60 flex justify-center lg:justify-end shrink-0">
          <button
            onClick={() => setIsCollapsed(!isCollapsed)}
            className="hidden lg:flex items-center justify-center w-8 h-8 rounded-[5px] text-slate-400 hover:text-slate-600 hover:bg-slate-100 dark:hover:text-slate-200 dark:hover:bg-slate-800 transition-all"
            aria-label="Toggle Sidebar"
          >
            {isCollapsed ? <PanelLeftOpen size={16} /> : <PanelLeftClose size={16} />}
          </button>
        </div>
      </aside>
    </>
  );
};

export const Sidebar = React.memo(SidebarComponent);
