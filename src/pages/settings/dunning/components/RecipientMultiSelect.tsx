import { useId, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Checkbox } from '@/shared/ui/Checkbox/Checkbox';
import { cn } from '@/shared/lib/cn';
import type { AppUser } from '@/features/users/model/user.types';

interface RecipientMultiSelectProps {
  users: AppUser[];
  selectedIds: string[];
  onChange: (ids: string[]) => void;
  disabled?: boolean;
}

export const RecipientMultiSelect = ({
  users,
  selectedIds,
  onChange,
  disabled = false,
}: RecipientMultiSelectProps) => {
  const { t } = useTranslation();
  const [query, setQuery] = useState('');
  const groupId = useId();

  const selected = useMemo(() => new Set(selectedIds), [selectedIds]);
  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase();
    if (!q) return users;
    return users.filter((u) =>
      [u.username, u.email, u.firstName, u.lastName]
        .filter((v): v is string => Boolean(v))
        .some((v) => v.toLowerCase().includes(q)),
    );
  }, [users, query]);

  const toggle = (id: string) => {
    const next = new Set(selected);
    if (next.has(id)) next.delete(id);
    else next.add(id);
    onChange([...next]);
  };

  const labelFor = (u: AppUser) => {
    const name = [u.firstName, u.lastName].filter(Boolean).join(' ').trim();
    return name ? `${name} · ${u.email}` : u.email || u.username;
  };

  return (
    <div
      role="group"
      aria-labelledby={groupId}
      className={cn('flex flex-col gap-2', disabled && 'pointer-events-none opacity-60')}
    >
      <div className="flex items-center justify-between">
        <span id={groupId} className="text-xs font-medium text-slate-600 dark:text-slate-400">
          {t('Dunning.recipients.label')}
        </span>
        <span className="text-[11px] text-slate-400 dark:text-slate-500">
          {t('Dunning.recipients.selectedCount', { count: selectedIds.length })}
        </span>
      </div>

      <input
        type="search"
        value={query}
        disabled={disabled}
        onChange={(e) => setQuery(e.target.value)}
        placeholder={t('Dunning.recipients.searchPlaceholder')}
        aria-label={t('Dunning.recipients.searchPlaceholder')}
        className="w-full rounded-lg border border-slate-300 bg-white px-3 py-1.5 text-sm text-slate-700 placeholder:text-slate-400 focus-visible:ring-2 focus-visible:ring-primary-500/40 dark:border-slate-600 dark:bg-slate-900 dark:text-slate-200"
      />

      <div className="max-h-44 overflow-y-auto rounded-lg border border-slate-200 p-2 dark:border-slate-700">
        {filtered.length === 0 ? (
          <p className="px-1 py-2 text-xs text-slate-400 dark:text-slate-500">
            {t('Dunning.recipients.empty')}
          </p>
        ) : (
          <ul className="flex flex-col gap-1">
            {filtered.map((u) => (
              <li key={u.id}>
                <Checkbox
                  checked={selected.has(u.id)}
                  disabled={disabled}
                  onChange={() => toggle(u.id)}
                  label={labelFor(u)}
                />
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
};
