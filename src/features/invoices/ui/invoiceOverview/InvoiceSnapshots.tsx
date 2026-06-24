import { type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { User as UserIcon } from 'lucide-react';
import type { AddressSnapshot, CustomerSnapshot } from '@/shared/model/documentSnapshot.types';

export const CustomerSnapshotCard = ({ snapshot }: { snapshot: CustomerSnapshot }) => {
  const { t } = useTranslation();
  const rows = [
    { label: t('customers.fields.code'), value: snapshot.code, mono: true },
    { label: t('customers.fields.legalName'), value: snapshot.legalName },
    { label: t('customers.fields.tradeName'), value: snapshot.tradeName },
    { label: t('customers.fields.taxNumber'), value: snapshot.taxNumber, mono: true },
    { label: t('customers.fields.taxOffice'), value: snapshot.taxOffice },
    { label: t('customers.fields.nationalId'), value: snapshot.nationalId, mono: true },
    { label: t('customers.fields.email'), value: snapshot.email },
    { label: t('customers.fields.phone'), value: snapshot.phone },
  ].filter((r) => r.value);
  if (rows.length === 0) return null;
  return (
    <section className="rounded-lg border border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-900">
      <header className="flex items-center gap-1.5 text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
        <UserIcon size={12} />
        {t('orders.detail.customerSnapshot')}
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
      </dl>
    </section>
  );
};

export const AddressSnapshotCard = ({
  icon,
  title,
  snapshot,
  empty,
}: {
  icon: ReactNode;
  title: string;
  snapshot: AddressSnapshot | null;
  empty: string;
}) => (
  <article className="rounded-lg border border-slate-200 bg-white p-2.5 dark:border-slate-800 dark:bg-slate-900">
    <header className="flex items-center gap-1.5 text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:text-slate-400">
      {icon}
      {title}
    </header>
    <div className="mt-1.5 min-h-[44px] text-[11px] leading-tight text-slate-700 dark:text-slate-200">
      {snapshot ? (
        <>
          {snapshot.recipientName && <div className="font-semibold">{snapshot.recipientName}</div>}
          {snapshot.label && (
            <div className="text-[10px] uppercase tracking-wider text-slate-400">
              {snapshot.label}
            </div>
          )}
          <div className="mt-0.5">{snapshot.line1}</div>
          {snapshot.line2 && <div>{snapshot.line2}</div>}
          <div className="mt-0.5 text-slate-500 dark:text-slate-400">
            {[snapshot.postalCode, snapshot.city, snapshot.state, snapshot.country]
              .filter(Boolean)
              .join(', ')}
          </div>
        </>
      ) : (
        <span className="italic text-slate-400 dark:text-slate-500">{empty}</span>
      )}
    </div>
  </article>
);
