import { type ReactNode, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import {
  FileText,
  Mail,
  Phone,
  Receipt,
  ShoppingCart,
  Tag,
  User,
  UserCircle2,
  Users,
  Wallet,
} from 'lucide-react';
import type { Customer, CustomerOverview } from '@/features/customers/model/customer.types';

export const QuickActionsBar = ({
  customerId,
  blocked,
  onCreateOrder,
  onCreateInvoice,
  onRecordPayment,
}: {
  customerId: string;
  blocked: boolean;
  onCreateOrder?: (customerId: string) => void;
  onCreateInvoice?: (customerId: string) => void;
  onRecordPayment?: (customerId: string) => void;
}) => {
  const { t } = useTranslation();
  const actions: { label: string; icon: ReactNode; onClick?: () => void }[] = [
    {
      label: t('customers.detail.actions.newOrder'),
      icon: <ShoppingCart size={13} />,
      onClick: onCreateOrder ? () => onCreateOrder(customerId) : undefined,
    },
    {
      label: t('customers.detail.actions.newInvoice'),
      icon: <FileText size={13} />,
      onClick: onCreateInvoice ? () => onCreateInvoice(customerId) : undefined,
    },
    {
      label: t('customers.detail.actions.recordPayment'),
      icon: <Receipt size={13} />,
      onClick: onRecordPayment ? () => onRecordPayment(customerId) : undefined,
    },
  ];
  return (
    <div className="grid grid-cols-3 gap-1.5">
      {actions.map((action) => (
        <button
          key={action.label}
          type="button"
          onClick={action.onClick}
          disabled={blocked || !action.onClick}
          className="inline-flex items-center justify-center gap-1 rounded-md border border-slate-200 bg-white px-2 py-1.5 text-[11px] font-medium text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-50 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
        >
          {action.icon}
          <span className="truncate">{action.label}</span>
        </button>
      ))}
    </div>
  );
};

export const SnapshotCard = ({ customer }: { customer: Customer }) => {
  const { t } = useTranslation();
  const rows = useMemo(
    () =>
      [
        { label: t('customers.fields.type'), value: t(`customers.type.${customer.type}`) },
        { label: t('customers.fields.code'), value: customer.code ?? '—', mono: true },
        { label: t('customers.fields.legalName'), value: customer.legalName },
        { label: t('customers.fields.tradeName'), value: customer.tradeName },
        { label: t('customers.fields.taxNumber'), value: customer.taxNumber, mono: true },
        { label: t('customers.fields.taxOffice'), value: customer.taxOffice },
        { label: t('customers.fields.nationalId'), value: customer.nationalId, mono: true },
        { label: t('customers.fields.currency'), value: customer.defaultCurrency },
      ].filter((row) => row.value),
    [customer, t],
  );

  return (
    <section className="rounded-lg border border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-900">
      <header className="flex items-center gap-1.5 text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
        <UserCircle2 size={12} />
        {t('customers.detail.snapshot')}
      </header>
      <dl className="mt-2 grid grid-cols-2 gap-x-3 gap-y-1.5 text-[11px]">
        {rows.map((row) => (
          <div key={row.label} className="flex items-center justify-between gap-2">
            <dt className="text-slate-500 dark:text-slate-400">{row.label}</dt>
            <dd
              className={`min-w-0 truncate text-right text-slate-900 dark:text-slate-100 ${row.mono ? 'font-mono' : 'font-medium'}`}
            >
              {row.value}
            </dd>
          </div>
        ))}
        <div className="col-span-2 flex items-center justify-between gap-2 border-t border-slate-100 pt-1.5 text-[11px] dark:border-slate-800">
          <dt className="flex items-center gap-1 text-slate-500 dark:text-slate-400">
            <Mail size={11} />
            {customer.email ?? '—'}
          </dt>
          <dd className="flex items-center gap-1 text-slate-500 dark:text-slate-400">
            <Phone size={11} />
            {customer.phone ?? '—'}
          </dd>
        </div>
      </dl>
    </section>
  );
};

export const MetaChips = ({
  overview,
  loading,
}: {
  overview: CustomerOverview | null;
  loading: boolean;
}) => {
  const { t } = useTranslation();
  const chips = [
    {
      icon: <Users size={11} />,
      label: t('customers.detail.meta.group'),
      value: overview?.groupName,
    },
    {
      icon: <User size={11} />,
      label: t('customers.detail.meta.salesRep'),
      value: overview?.salesRepName,
    },
    {
      icon: <Tag size={11} />,
      label: t('customers.detail.meta.priceList'),
      value: overview?.priceListName,
    },
    {
      icon: <Wallet size={11} />,
      label: t('customers.detail.meta.paymentTerms'),
      value: overview?.paymentTermsName
        ? overview.paymentTermsNetDays !== null && overview.paymentTermsNetDays !== undefined
          ? `${overview.paymentTermsName} · ${t('customers.detail.meta.netDays', { count: overview.paymentTermsNetDays })}`
          : overview.paymentTermsName
        : null,
    },
  ];
  const visible = chips.filter((c) => c.value);
  if (visible.length === 0 && !loading) return null;
  return (
    <section className="flex flex-wrap items-center gap-1.5">
      {visible.map((chip) => (
        <span
          key={chip.label}
          className="inline-flex items-center gap-1 rounded-full border border-slate-200 bg-slate-50 px-2 py-0.5 text-[10px] text-slate-700 dark:border-slate-800 dark:bg-slate-800/60 dark:text-slate-200"
        >
          {chip.icon}
          <span className="font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
            {chip.label}
          </span>
          <span className="font-medium">{chip.value}</span>
        </span>
      ))}
      {loading && visible.length === 0 && (
        <span className="text-[10px] italic text-slate-400 dark:text-slate-500">
          {t('common.loading')}
        </span>
      )}
    </section>
  );
};
