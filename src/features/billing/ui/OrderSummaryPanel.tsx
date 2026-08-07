import { ArrowLeft, ArrowRight, Lock, ShieldCheck, ShoppingCart, X } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import { formatCurrency, formatDate } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { Badge } from '@/shared/ui/Badge/Badge';
import { Button } from '@/shared/ui/Button/Button';
import { Card, CardBody, CardHeader, CardTitle } from '@/shared/ui/Card/Card';

import type { StoreLine } from '../model/moduleStore';
import { projectedEndUtc } from '../model/moduleStore';
import { ModuleIcon } from './ModuleIcon';

interface Props {
  step: 'select' | 'payment';
  lines: StoreLine[];
  currency: string;
  total: number;
  canPurchase: boolean;
  isSubmitting: boolean;
  mixedCurrency: boolean;
  onRemove: (moduleId: string) => void;
  onClear: () => void;
  onNext: () => void;
  onBack: () => void;
  onSubmit: () => void;
}

export const OrderSummaryPanel = ({
  step,
  lines,
  currency,
  total,
  canPurchase,
  isSubmitting,
  mixedCurrency,
  onRemove,
  onClear,
  onNext,
  onBack,
  onSubmit,
}: Props) => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const now = new Date();
  const empty = lines.length === 0;

  return (
    <Card variant="elevated" padding="none" data-testid="order-summary">
      <CardHeader className="flex items-center justify-between gap-2 px-4 py-3">
        <CardTitle className="text-sm">{t('billing.store.summary')}</CardTitle>
        {!empty && (
          <button
            type="button"
            onClick={onClear}
            className="text-xs text-slate-500 hover:text-danger-600 dark:text-slate-400 dark:hover:text-danger-400"
          >
            {t('billing.store.clear')}
          </button>
        )}
      </CardHeader>

      <CardBody className="space-y-3 px-4 pb-4">
        {empty ? (
          <div className="flex flex-col items-center gap-2 py-8 text-center">
            <ShoppingCart
              size={22}
              className="text-slate-300 dark:text-slate-600"
              aria-hidden="true"
            />
            <p className="text-xs text-slate-500 dark:text-slate-400">
              {t('billing.store.emptyCart')}
            </p>
          </div>
        ) : (
          <ul className="divide-y divide-slate-100 dark:divide-white/5">
            {lines.map((line) => (
              <li key={line.moduleId} className="flex items-start gap-2 py-2.5">
                <ModuleIcon
                  iconKey={line.iconKey}
                  size={15}
                  className="mt-0.5 shrink-0 text-slate-400 dark:text-slate-500"
                />
                <div className="min-w-0 flex-1">
                  <div className="truncate text-sm font-medium text-slate-800 dark:text-slate-200">
                    {line.moduleName}
                  </div>
                  <div className="mt-0.5 flex flex-wrap items-center gap-1.5 text-[11px] text-slate-500 dark:text-slate-400">
                    <span>{line.planLabel}</span>
                    {line.isRenewal && (
                      <Badge variant="info">
                        {t('billing.store.renewalUntil', {
                          date: formatDate(projectedEndUtc(line, now), locale),
                        })}
                      </Badge>
                    )}
                  </div>
                </div>
                <div
                  data-testid="summary-line-price"
                  className="shrink-0 text-right text-sm font-medium text-slate-800 tabular-nums dark:text-slate-200"
                >
                  {formatCurrency(line.unitPrice, locale, line.currency)}
                </div>
                {step === 'select' && (
                  <button
                    type="button"
                    onClick={() => onRemove(line.moduleId)}
                    aria-label={t('billing.store.removeLine', { name: line.moduleName })}
                    className="mt-0.5 shrink-0 rounded p-0.5 text-slate-400 hover:bg-slate-100 hover:text-danger-600 dark:hover:bg-slate-800 dark:hover:text-danger-400"
                  >
                    <X size={13} aria-hidden="true" />
                  </button>
                )}
              </li>
            ))}
          </ul>
        )}

        {!empty && (
          <div className="border-t border-slate-100 pt-3 dark:border-white/5">
            <div className="flex items-baseline justify-between">
              <span className="text-sm text-slate-600 dark:text-slate-300">
                {t('billing.store.total')}
              </span>
              <span
                data-testid="summary-total"
                className="text-lg font-semibold text-slate-900 tabular-nums dark:text-slate-100"
              >
                {formatCurrency(total, locale, currency)}
              </span>
            </div>
            <p className="mt-1 text-[11px] text-slate-400 dark:text-slate-500">
              {t('billing.store.taxNote')}
            </p>
          </div>
        )}

        {mixedCurrency && (
          <p className="rounded-md bg-danger-50 px-2.5 py-2 text-xs text-danger-700 dark:bg-danger-500/10 dark:text-danger-300">
            {t('billing.store.mixedCurrency')}
          </p>
        )}

        {!canPurchase && (
          <p className="flex items-start gap-1.5 rounded-md bg-warning-50 px-2.5 py-2 text-xs text-warning-800 dark:bg-warning-500/10 dark:text-warning-300">
            <Lock size={13} className="mt-0.5 shrink-0" aria-hidden="true" />
            {t('billing.store.adminOnly')}
          </p>
        )}

        {step === 'payment' && (
          <p className="flex items-start gap-1.5 rounded-md bg-slate-50 px-2.5 py-2 text-xs text-slate-600 dark:bg-slate-800/60 dark:text-slate-300">
            <ShieldCheck
              size={13}
              className="mt-0.5 shrink-0 text-success-600 dark:text-success-400"
              aria-hidden="true"
            />
            {t('billing.store.cardNotice')}
          </p>
        )}

        <div className="space-y-2 pt-1">
          {step === 'select' ? (
            <Button
              size="lg"
              className="w-full"
              disabled={empty || !canPurchase || mixedCurrency}
              onClick={onNext}
            >
              {t('billing.store.continue')}
              <ArrowRight size={16} className="ml-1.5" aria-hidden="true" />
            </Button>
          ) : (
            <>
              <Button
                size="lg"
                className="w-full"
                isLoading={isSubmitting}
                disabled={empty || !canPurchase || mixedCurrency}
                onClick={onSubmit}
              >
                {t('billing.store.payNow')}
              </Button>
              <Button
                variant="ghost"
                size="sm"
                className="w-full"
                onClick={onBack}
                disabled={isSubmitting}
              >
                <ArrowLeft size={14} className="mr-1.5" aria-hidden="true" />
                {t('billing.store.backToSelection')}
              </Button>
            </>
          )}
        </div>
      </CardBody>
    </Card>
  );
};
