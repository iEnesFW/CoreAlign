import { useUomsQuery } from '@/shared/master-data/hooks/useMasterData';

interface Props {
  value: string;
  onChange: (code: string) => void;
  id?: string;
  disabled?: boolean;
  className?: string;
  placeholder?: string;
}

const fieldCls =
  'w-full rounded border border-slate-200 bg-white px-3 py-2 text-sm text-slate-900 focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500 disabled:opacity-60 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100';

export const UnitOfMeasureSelect = ({
  value,
  onChange,
  id,
  disabled,
  className,
  placeholder,
}: Props) => {
  const { data } = useUomsQuery(true);
  const uoms = data?.data ?? [];
  const hasValue = value !== '' && uoms.some((u) => u.code === value);

  return (
    <select
      id={id}
      disabled={disabled}
      className={className ?? fieldCls}
      value={value}
      onChange={(e) => onChange(e.target.value)}
    >
      {placeholder !== undefined && <option value="">{placeholder}</option>}
      {!hasValue && value !== '' && <option value={value}>{value}</option>}
      {uoms.map((u) => (
        <option key={u.id} value={u.code}>
          {u.code} — {u.name}
        </option>
      ))}
    </select>
  );
};
