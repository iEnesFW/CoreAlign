import { useCurrenciesQuery } from '@/shared/lookups/hooks/useLookups';

interface Props {
  value: string;
  onChange: (code: string) => void;
  id?: string;
  disabled?: boolean;
  className?: string;
}

const fieldCls =
  'w-full rounded border border-slate-200 bg-white px-3 py-2 text-sm text-slate-900 focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500 disabled:opacity-60 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100';

export const CurrencySelect = ({ value, onChange, id, disabled, className }: Props) => {
  const { data } = useCurrenciesQuery(true);
  const currencies = data?.data ?? [];
  const hasValue = value && currencies.some((c) => c.code === value);

  return (
    <select
      id={id}
      disabled={disabled}
      className={className ?? fieldCls}
      value={value}
      onChange={(e) => onChange(e.target.value)}
    >
      {!hasValue && value && <option value={value}>{value}</option>}
      {currencies.map((c) => (
        <option key={c.code} value={c.code}>
          {c.code} — {c.name}
        </option>
      ))}
    </select>
  );
};
