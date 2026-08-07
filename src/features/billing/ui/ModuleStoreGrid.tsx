import { Check, Lock } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import { Badge } from '@/shared/ui/Badge/Badge';
import { cn } from '@/shared/lib/cn';
import { formatCurrency, formatDate } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';

import type { StoreGroup, StoreModule } from '../model/moduleStore';
import { ModuleIcon } from './ModuleIcon';

interface Props {
  groups: StoreGroup[];
  canPurchase: boolean;
  onToggle: (moduleId: string, planId: string) => void;
}

const StoreCard = ({
  entry,
  canPurchase,
  onToggle,
}: {
  entry: StoreModule;
  canPurchase: boolean;
  onToggle: (moduleId: string, planId: string) => void;
}) => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const { module, plan, owned, selected } = entry;

  const core = module.isCore;
  const interactive = !core && Boolean(plan) && canPurchase;

  const handleToggle = () => {
    if (!interactive || !plan) return;
    onToggle(module.id, plan.id);
  };

  return (
    <div
      data-testid="module-card"
      data-module-code={module.code}
      data-core={core ? 'true' : 'false'}
      data-owned={owned ? 'true' : 'false'}
      role={interactive ? 'button' : undefined}
      tabIndex={interactive ? 0 : undefined}
      aria-pressed={interactive ? selected : undefined}
      aria-disabled={interactive ? undefined : true}
      onClick={handleToggle}
      onKeyDown={(e) => {
        if (!interactive) return;
        if (e.key === 'Enter' || e.key === ' ') {
          e.preventDefault();
          handleToggle();
        }
      }}
      className={cn(
        'group relative flex h-full flex-col gap-3 rounded-xl border p-4 text-left transition',
        interactive &&
          'cursor-pointer hover:border-primary-300 hover:shadow-sm dark:hover:border-primary-500/50',
        selected
          ? 'border-primary-500 bg-primary-50/60 ring-2 ring-primary-500 dark:bg-primary-500/10'
          : 'border-slate-200/70 bg-white dark:border-white/10 dark:bg-slate-900',
        !interactive && 'opacity-90',
      )}
    >
      <div className="flex items-start gap-3">
        <span
          className={cn(
            'flex h-10 w-10 shrink-0 items-center justify-center rounded-lg',
            selected
              ? 'bg-primary-600 text-white'
              : 'bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-300',
          )}
        >
          <ModuleIcon iconKey={module.iconKey} size={18} />
        </span>

        <div className="min-w-0 flex-1">
          <div className="flex items-start justify-between gap-2">
            <h3 className="truncate text-sm font-semibold text-slate-900 dark:text-slate-100">
              {module.name}
            </h3>
            {selected && (
              <Check
                size={18}
                className="shrink-0 text-primary-600 dark:text-primary-400"
                aria-hidden="true"
              />
            )}
          </div>
          {module.description && (
            <p className="mt-0.5 line-clamp-2 text-xs text-slate-500 dark:text-slate-400">
              {module.description}
            </p>
          )}
        </div>
      </div>

      <div className="mt-auto flex items-end justify-between gap-2">
        <div className="min-w-0">
          {core ? (
            <Badge variant="neutral">
              <Lock size={11} className="mr-1" aria-hidden="true" />
              {t('billing.store.alwaysIncluded')}
            </Badge>
          ) : owned ? (
            <Badge variant="success">
              {t('billing.store.activeUntil', { date: formatDate(owned.endUtc, locale) })}
            </Badge>
          ) : null}
        </div>

        {!core && plan && (
          <div className="shrink-0 text-right">
            <div className="text-base font-semibold text-slate-900 dark:text-slate-100">
              {formatCurrency(plan.price, locale, plan.currency)}
            </div>
            <div className="text-[11px] text-slate-500 dark:text-slate-400">
              {plan.displayLabel}
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export const ModuleStoreGrid = ({ groups, canPurchase, onToggle }: Props) => (
  <div className="space-y-6">
    {groups.map((group) => (
      <section key={group.category}>
        <h2 className="mb-2 text-xs font-semibold tracking-wide text-slate-500 uppercase dark:text-slate-400">
          {group.category}
        </h2>
        <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
          {group.modules.map((entry) => (
            <StoreCard
              key={entry.module.id}
              entry={entry}
              canPurchase={canPurchase}
              onToggle={onToggle}
            />
          ))}
        </div>
      </section>
    ))}
  </div>
);
