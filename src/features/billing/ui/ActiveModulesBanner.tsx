import { useTranslation } from 'react-i18next';
import { CheckCircle2 } from 'lucide-react';
import { formatDate } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import type { TenantModuleDto } from '../model/billing.types';

interface Props {
  modules: TenantModuleDto[];
}

export const ActiveModulesBanner = ({ modules }: Props) => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const active = modules.filter((m) => m.isCurrentlyActive);

  return (
    <div className="rounded-xl border border-success-200/70 bg-success-50/60 p-3 dark:border-success-500/30 dark:bg-success-500/5">
      <div className="flex flex-wrap items-center gap-2">
        <CheckCircle2 size={16} className="text-success-600 dark:text-success-300" />
        <h3 className="text-xs font-semibold text-success-900 dark:text-success-200">
          {t('billing.activeBanner.title', { count: active.length })}
        </h3>
      </div>
      {active.length > 0 ? (
        <div className="mt-2 flex flex-wrap gap-1.5">
          {active.map((m) => (
            <span
              key={m.id}
              className="inline-flex items-center gap-1 rounded-full bg-white px-2 py-0.5 text-[11px] font-medium text-success-700 ring-1 ring-success-200 dark:bg-success-500/10 dark:text-success-200 dark:ring-success-500/30"
            >
              <span>{m.name}</span>
              {m.endUtc && (
                <span className="text-[10px] text-success-600/70 dark:text-success-300/70">
                  · {formatDate(m.endUtc, locale)}
                </span>
              )}
            </span>
          ))}
        </div>
      ) : (
        <p className="mt-1 text-[11px] text-success-700/80 dark:text-success-200/80">
          {t('billing.activeBanner.empty')}
        </p>
      )}
    </div>
  );
};
