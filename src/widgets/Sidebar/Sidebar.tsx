import React, { useCallback, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { NavLink, useLocation } from 'react-router-dom';
import { routePreloaders } from '@/app/router/routePrefetch';
import { useIsTenantAdmin } from '@/features/billing/hooks/useIsTenantAdmin';
import { Logo } from '@/shared/ui/Logo/Logo';
import { cn } from '@/shared/lib/cn';
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
  FileSignature,
  FileText,
  Repeat,
  RotateCcw,
  Activity,
  BarChart3,
  CopyCheck,
  Hash,
  Settings,
  Plug,
  Factory,
  ShieldCheck,
  Wrench,
  FolderKanban,
  KeyRound,
  LayoutGrid,
  ClipboardList,
  ScanBarcode,
  ShoppingBag,
  ReceiptText,
  Inbox,
  ListChecks,
  CalendarClock,
  Mail,
  PackageCheck,
  Bug,
  BellRing,
  UserCog,
  Wallet,
  SlidersHorizontal,
} from 'lucide-react';

interface SidebarProps {
  isOpen: boolean;
  setIsOpen: (isOpen: boolean) => void;
  isCollapsed: boolean;
  setIsCollapsed: (isCollapsed: boolean) => void;
}

type NavChild = { name: string; href: string; labelKey: string };

type NavItem = {
  name?: string;
  labelKey?: string;
  href?: string;
  icon?: React.ComponentType<{ size?: number; className?: string }>;
  children?: NavChild[];
  section?: string;
  tourAnchor?: string;
};

type NavGroup = { section: string; items: NavItem[] };

const baseNavigation: NavItem[] = [
  { section: 'OVERVIEW' },
  {
    name: 'Dashboard',
    labelKey: 'Sidebar.nav.dashboard',
    href: '/dashboard',
    icon: LayoutDashboard,
  },

  { section: 'SALES & CRM' },
  {
    name: 'Customers',
    labelKey: 'Sidebar.nav.customers',
    href: '/dashboard/customers',
    icon: Users,
  },
  {
    name: 'Quotes',
    labelKey: 'Sidebar.nav.quotes',
    href: '/dashboard/quotes',
    icon: FileSignature,
  },
  {
    name: 'Orders',
    labelKey: 'Sidebar.nav.orders',
    href: '/dashboard/orders',
    icon: ShoppingCart,
  },
  {
    name: 'Invoices',
    labelKey: 'Sidebar.nav.invoices',
    href: '/dashboard/invoices',
    icon: FileText,
  },
  {
    name: 'Recurring Invoices',
    labelKey: 'Sidebar.nav.recurringInvoices',
    href: '/dashboard/recurring-invoices',
    icon: Repeat,
  },
  {
    name: 'Returns',
    labelKey: 'Sidebar.nav.returns',
    href: '/dashboard/returns',
    icon: RotateCcw,
  },

  { section: 'PROJECTS' },
  {
    name: 'Projects',
    labelKey: 'Sidebar.nav.projects',
    href: '/dashboard/glass-enclosure/projects',
    icon: FolderKanban,
    tourAnchor: 'sidebar-projects',
  },

  { section: 'PURCHASING' },
  { name: 'Vendors', labelKey: 'Sidebar.nav.vendors', href: '/dashboard/vendors', icon: Box },
  {
    name: 'Purchase Orders',
    labelKey: 'po.navLabel',
    href: '/dashboard/purchasing/purchase-orders',
    icon: ShoppingBag,
  },
  {
    name: 'Vendor Bills',
    labelKey: 'ap.navLabel',
    href: '/dashboard/purchasing/vendor-bills',
    icon: ReceiptText,
  },
  {
    name: 'Incoming Invoices',
    labelKey: 'Sidebar.nav.incomingInvoices',
    href: '/dashboard/incoming-invoices',
    icon: Inbox,
  },
  {
    name: 'Goods Receipts',
    labelKey: 'grn.page.title',
    href: '/dashboard/purchasing/goods-receipts',
    icon: PackageCheck,
  },
  {
    name: '3-Way Match',
    labelKey: 'VendorBills.threeWayMatch.navLabel',
    href: '/dashboard/purchasing/three-way-match',
    icon: ListChecks,
  },
  {
    name: 'Payables Aging',
    labelKey: 'apAging.navLabel',
    href: '/dashboard/purchasing/payables-aging',
    icon: CalendarClock,
  },

  { section: 'HR & PAYROLL' },
  {
    name: 'Employees',
    labelKey: 'Payroll.nav.employees',
    href: '/dashboard/hr/employees',
    icon: UserCog,
  },
  {
    name: 'Payroll Runs',
    labelKey: 'Payroll.nav.payrollRuns',
    href: '/dashboard/hr/payroll-runs',
    icon: Wallet,
  },
  {
    name: 'Payroll Parameters',
    labelKey: 'Payroll.nav.parameters',
    href: '/dashboard/hr/payroll-parameters',
    icon: SlidersHorizontal,
  },

  { section: 'INVENTORY' },
  {
    name: 'Products',
    labelKey: 'Sidebar.nav.products',
    href: '/dashboard/products',
    icon: Package,
  },
  { name: 'Stock', labelKey: 'Sidebar.nav.stock', href: '/dashboard/inventory', icon: Boxes },
  {
    name: 'Stock Counts',
    labelKey: 'Inventory.StockCounts.navLabel',
    href: '/dashboard/inventory/stock-counts',
    icon: ClipboardList,
    tourAnchor: 'sidebar-stock-counts',
  },
  {
    name: 'Serial Lookup',
    labelKey: 'Sidebar.nav.serialLookup',
    href: '/dashboard/inventory/serial-lookup',
    icon: ScanBarcode,
  },

  { section: 'PRODUCTION' },
  {
    name: 'MRP',
    labelKey: 'Sidebar.nav.mrp',
    href: '/dashboard/mrp',
    icon: Factory,
    tourAnchor: 'sidebar-mrp',
  },
  {
    name: 'MRP Workbench',
    labelKey: 'Sidebar.nav.mrpWorkbench',
    href: '/dashboard/mrp/workbench',
    icon: LayoutGrid,
    tourAnchor: 'sidebar-mrp-workbench',
  },

  { section: 'AFTER SALES' },
  {
    name: 'Warranty',
    labelKey: 'Sidebar.nav.warranty',
    href: '/dashboard/warranty/contracts',
    icon: ShieldCheck,
    tourAnchor: 'sidebar-warranty',
  },
  {
    name: 'Installation',
    labelKey: 'Sidebar.nav.installation',
    href: '/dashboard/installation/acceptances',
    icon: Wrench,
    tourAnchor: 'sidebar-installation',
  },

  { section: 'ACCOUNTING' },
  {
    name: 'Accounting',
    labelKey: 'Sidebar.nav.accounting',
    icon: CreditCard,
    children: [
      {
        name: 'Chart of Accounts',
        labelKey: 'Sidebar.nav.chartOfAccounts',
        href: '/dashboard/accounting/chart-of-accounts',
      },
      {
        name: 'Journal Entries',
        labelKey: 'Sidebar.nav.journalEntries',
        href: '/dashboard/accounting/journal-entries',
      },
      {
        name: 'Trial Balance',
        labelKey: 'Sidebar.nav.trialBalance',
        href: '/dashboard/accounting/trial-balance',
      },
      {
        name: 'Balance Sheet',
        labelKey: 'Sidebar.nav.balanceSheet',
        href: '/dashboard/accounting/balance-sheet',
      },
      {
        name: 'Income Statement',
        labelKey: 'Sidebar.nav.incomeStatement',
        href: '/dashboard/accounting/income-statement',
      },
      {
        name: 'Reconciliation',
        labelKey: 'Sidebar.nav.reconciliation',
        href: '/dashboard/accounting/reconciliation',
      },
      {
        name: 'Accounting Periods',
        labelKey: 'Sidebar.nav.periods',
        href: '/dashboard/accounting/periods',
      },
      {
        name: 'Year-End Close',
        labelKey: 'Sidebar.nav.yearEndClose',
        href: '/dashboard/accounting/year-end-close',
      },
      {
        name: 'Bank Accounts',
        labelKey: 'Sidebar.nav.bankAccounts',
        href: '/dashboard/accounting/bank-accounts',
      },
      {
        name: 'Cash Position',
        labelKey: 'Sidebar.nav.cashPosition',
        href: '/dashboard/accounting/cash-position',
      },
    ],
  },

  { section: 'ANALYTICS' },
  { name: 'Reports', labelKey: 'Sidebar.nav.reports', href: '/dashboard/reports', icon: BarChart3 },
  {
    name: 'Duplicate Detection',
    labelKey: 'Sidebar.nav.duplicateDetection',
    href: '/dashboard/reports/duplicates',
    icon: CopyCheck,
  },
  {
    name: 'Number Gaps',
    labelKey: 'Sidebar.nav.documentNumberGap',
    href: '/dashboard/reports/document-number-gaps',
    icon: Hash,
  },

  { section: 'SYSTEM' },
  {
    name: 'Activity Log',
    labelKey: 'Sidebar.nav.activityLog',
    href: '/dashboard/activity',
    icon: Activity,
  },
  {
    name: 'Settings',
    labelKey: 'Sidebar.nav.settings',
    href: '/dashboard/settings',
    icon: Settings,
  },
];

const adminNavigation: NavItem[] = [
  { section: 'ADMINISTRATION' },
  {
    name: 'Providers',
    labelKey: 'Sidebar.nav.providers',
    href: '/dashboard/admin/providers',
    icon: Plug,
  },
  {
    name: 'SSO Settings',
    labelKey: 'Sidebar.nav.ssoSettings',
    href: '/dashboard/admin/providers/sso',
    icon: KeyRound,
  },
  {
    name: 'SMTP Settings',
    labelKey: 'Sidebar.nav.smtpSettings',
    href: '/dashboard/admin/smtp',
    icon: Mail,
  },
  {
    name: 'Error Logs',
    labelKey: 'Sidebar.nav.errorLogs',
    href: '/dashboard/admin/error-logs',
    icon: Bug,
  },
  {
    name: 'Dunning Reminders',
    labelKey: 'Sidebar.nav.dunningSettings',
    href: '/dashboard/admin/dunning-settings',
    icon: BellRing,
  },
];

const SECTION_KEYS: Record<string, string> = {
  OVERVIEW: 'overview',
  'SALES & CRM': 'salesCrm',
  PROJECTS: 'projects',
  PURCHASING: 'purchasing',
  'HR & PAYROLL': 'hrPayroll',
  INVENTORY: 'inventory',
  PRODUCTION: 'production',
  'AFTER SALES': 'afterSales',
  ACCOUNTING: 'accounting',
  ANALYTICS: 'analytics',
  SYSTEM: 'system',
  ADMINISTRATION: 'administration',
};

const toGroups = (items: NavItem[]): NavGroup[] => {
  const groups: NavGroup[] = [];
  let current: NavGroup | null = null;
  for (const item of items) {
    if (item.section) {
      current = { section: item.section, items: [] };
      groups.push(current);
    } else if (current) {
      current.items.push(item);
    }
  }
  return groups;
};

const itemBase =
  'group relative flex items-center gap-2.5 rounded-lg px-2.5 py-2 transition-all duration-200';
const itemActive =
  'bg-gradient-to-r from-primary-100/90 via-primary-50/60 to-transparent font-semibold text-primary-700 shadow-sm shadow-primary-500/5 ring-1 ring-inset ring-primary-500/10 before:absolute before:inset-y-1 before:left-0 before:w-1 before:rounded-r-full before:bg-gradient-to-b before:from-primary-500 before:to-accent-500 dark:from-primary-500/20 dark:via-primary-500/[0.08] dark:text-primary-200 dark:ring-primary-400/15';
const itemIdle =
  'text-slate-500 hover:bg-slate-100/70 hover:text-slate-900 dark:text-slate-400 dark:hover:bg-white/5 dark:hover:text-white';

const SidebarComponent: React.FC<SidebarProps> = ({
  isOpen,
  setIsOpen,
  isCollapsed,
  setIsCollapsed,
}) => {
  const { t } = useTranslation();
  const location = useLocation();
  const [expandedMenus, setExpandedMenus] = useState<string[]>(['Accounting']);
  const [collapsedSections, setCollapsedSections] = useState<string[]>([]);
  const isAdmin = useIsTenantAdmin();

  const groups = useMemo<NavGroup[]>(
    () => toGroups(isAdmin ? [...baseNavigation, ...adminNavigation] : baseNavigation),
    [isAdmin],
  );

  const prefetch = useCallback((href: string) => {
    const loader = routePreloaders[href];
    if (loader) {
      try {
        loader();
      } catch {
        // WHY: prefetch is a best-effort optimization; failures must not break navigation
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

  const toggleSection = (section: string) => {
    setCollapsedSections((prev) =>
      prev.includes(section) ? prev.filter((s) => s !== section) : [...prev, section],
    );
  };

  const isItemActive = (item: NavItem): boolean => {
    if (item.href) {
      return location.pathname === item.href || location.pathname.startsWith(`${item.href}/`);
    }
    if (item.children) {
      return item.children.some((c) => location.pathname.startsWith(c.href));
    }
    return false;
  };

  const isSectionOpen = (group: NavGroup): boolean =>
    !collapsedSections.includes(group.section) || group.items.some(isItemActive);

  const labelHidden = (collapsed: boolean) =>
    collapsed ? 'lg:w-0 lg:overflow-hidden lg:opacity-0' : 'opacity-100';

  const renderItem = (item: NavItem) => {
    const Icon = item.icon!;
    const hasChildren = item.children && item.children.length > 0;
    const isExpanded = expandedMenus.includes(item.name!);
    const active = isItemActive(item);
    const label = item.labelKey ? t(item.labelKey, { defaultValue: item.name }) : item.name;

    if (hasChildren) {
      return (
        <div key={item.name} className="space-y-0.5">
          <button
            type="button"
            onClick={() => toggleMenu(item.name!)}
            aria-expanded={isExpanded}
            title={isCollapsed ? (label ?? undefined) : undefined}
            className={cn(itemBase, 'w-full justify-between', active ? itemActive : itemIdle)}
          >
            <span className="flex items-center gap-2.5">
              <Icon
                size={17}
                className={cn(
                  'shrink-0 transition-colors',
                  active
                    ? 'text-primary-600 dark:text-primary-400'
                    : 'group-hover:text-primary-500',
                )}
              />
              <span
                className={cn(
                  'whitespace-nowrap text-xs font-medium transition-opacity duration-300',
                  labelHidden(isCollapsed),
                )}
              >
                {label}
              </span>
            </span>
            {!isCollapsed && (
              <ChevronDown
                size={14}
                className={cn(
                  'shrink-0 text-slate-400 transition-transform duration-200',
                  isExpanded && 'rotate-180 text-primary-500',
                )}
              />
            )}
          </button>

          <div
            className={cn(
              'grid transition-all duration-300 ease-in-out',
              isExpanded && !isCollapsed
                ? 'grid-rows-[1fr] opacity-100'
                : 'grid-rows-[0fr] opacity-0',
            )}
          >
            <div className="overflow-hidden">
              <div className="relative space-y-0.5 py-0.5 pl-7 pr-1 before:absolute before:bottom-1 before:left-[14px] before:top-1 before:w-px before:bg-slate-200 dark:before:bg-white/10">
                {item.children?.map((child) => {
                  const childLabel = t(child.labelKey, { defaultValue: child.name });
                  return (
                    <NavLink
                      key={child.href}
                      to={child.href}
                      onMouseEnter={() => prefetch(child.href)}
                      onFocus={() => prefetch(child.href)}
                      className={({ isActive }) =>
                        cn(
                          'relative block truncate rounded-lg px-2.5 py-1.5 text-[11px] font-medium transition-all duration-200',
                          isActive
                            ? 'text-primary-600 before:absolute before:-left-[14px] before:top-1/2 before:h-1.5 before:w-1.5 before:-translate-y-1/2 before:rounded-full before:bg-primary-500 before:shadow-[0_0_0_3px_rgba(99,102,241,0.15)] dark:text-primary-300'
                            : 'text-slate-500 hover:translate-x-0.5 hover:text-slate-900 dark:text-slate-400 dark:hover:text-white',
                        )
                      }
                      title={childLabel}
                    >
                      {childLabel}
                    </NavLink>
                  );
                })}
              </div>
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
        title={isCollapsed ? (label ?? undefined) : undefined}
        className={({ isActive }) => cn(itemBase, isActive ? itemActive : itemIdle)}
      >
        {({ isActive }) => (
          <>
            <Icon
              size={17}
              className={cn(
                'shrink-0 transition-colors',
                isActive
                  ? 'text-primary-600 dark:text-primary-400'
                  : 'group-hover:text-primary-500',
              )}
            />
            <span
              className={cn(
                'whitespace-nowrap text-xs font-medium transition-opacity duration-300',
                labelHidden(isCollapsed),
              )}
            >
              {label}
            </span>
          </>
        )}
      </NavLink>
    );
  };

  return (
    <>
      {isOpen && (
        <div
          className="fixed inset-0 z-40 bg-slate-900/40 backdrop-blur-sm lg:hidden"
          onClick={() => setIsOpen(false)}
          aria-hidden="true"
        />
      )}

      <aside
        aria-label={t('Sidebar.primaryNav', { defaultValue: 'Primary navigation' })}
        className={cn(
          'fixed inset-y-0 left-0 z-50 flex transform flex-col border-r border-slate-200/70 bg-white shadow-[8px_0_32px_-12px_rgba(15,23,42,0.08)] transition-all duration-300 ease-[cubic-bezier(0.4,0,0.2,1)] lg:static lg:inset-0 lg:translate-x-0 dark:border-white/5 dark:bg-shell dark:shadow-[8px_0_32px_-12px_rgba(0,0,0,0.5)]',
          isOpen ? 'translate-x-0' : '-translate-x-full',
          isCollapsed ? 'lg:w-[64px]' : 'w-[244px]',
        )}
      >
        <div className="relative flex h-12 shrink-0 items-center justify-between border-b border-slate-200/70 px-3 dark:border-white/5">
          <div
            className={cn(
              'flex items-center transition-opacity duration-300',
              labelHidden(isCollapsed),
            )}
          >
            <Logo size={20} />
          </div>

          <div
            className={cn(
              'absolute left-0 hidden w-full items-center justify-center transition-opacity duration-300 lg:flex',
              isCollapsed ? 'opacity-100' : 'pointer-events-none opacity-0',
            )}
          >
            <Logo size={26} showText={false} />
          </div>

          <button
            type="button"
            onClick={() => setIsOpen(false)}
            aria-label={t('Navbar.closeMenu', { defaultValue: 'Close menu' })}
            className="grid h-7 w-7 place-items-center rounded-lg text-slate-400 transition-colors hover:bg-slate-100 hover:text-slate-600 lg:hidden dark:hover:bg-white/5 dark:hover:text-slate-200"
          >
            <X size={16} />
          </button>
        </div>

        <nav
          aria-label={t('Sidebar.primaryNav', { defaultValue: 'Primary navigation' })}
          className="flex-1 space-y-0.5 overflow-y-auto overflow-x-hidden px-2 py-2"
        >
          {groups.map((group) => {
            const open = isSectionOpen(group);
            const sectionLabel = t(`Sidebar.sections.${SECTION_KEYS[group.section] ?? ''}`, {
              defaultValue: group.section,
            });
            return (
              <div key={group.section} className="space-y-0.5">
                {isCollapsed ? (
                  <div
                    className="mx-auto my-2 h-px w-6 bg-slate-200/70 dark:bg-white/5"
                    aria-hidden="true"
                  />
                ) : (
                  <button
                    type="button"
                    onClick={() => toggleSection(group.section)}
                    aria-expanded={open}
                    className="group/section flex w-full items-center justify-between rounded-lg px-2.5 pb-1 pt-4 text-[10px] font-semibold uppercase tracking-[0.08em] text-slate-400 transition-colors hover:text-slate-600 dark:text-slate-500 dark:hover:text-slate-300"
                  >
                    <span className="truncate">{sectionLabel}</span>
                    <ChevronDown
                      size={11}
                      className={cn(
                        'shrink-0 text-slate-300 transition-all duration-200 group-hover/section:text-slate-500 dark:text-slate-600',
                        !open && '-rotate-90',
                      )}
                    />
                  </button>
                )}
                <div className={cn('space-y-0.5', !isCollapsed && !open && 'hidden')}>
                  {group.items.map(renderItem)}
                </div>
              </div>
            );
          })}
        </nav>

        <div className="flex shrink-0 justify-center border-t border-slate-200/70 p-2 lg:justify-end dark:border-white/5">
          <button
            type="button"
            onClick={() => setIsCollapsed(!isCollapsed)}
            className="hidden h-8 w-8 items-center justify-center rounded-lg text-slate-400 transition-all hover:bg-slate-100 hover:text-primary-600 lg:flex dark:hover:bg-white/5 dark:hover:text-primary-400"
            aria-label={t('common.toggleSidebar', { defaultValue: 'Toggle sidebar' })}
          >
            {isCollapsed ? <PanelLeftOpen size={16} /> : <PanelLeftClose size={16} />}
          </button>
        </div>
      </aside>
    </>
  );
};

export const Sidebar = React.memo(SidebarComponent);
