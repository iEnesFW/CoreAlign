import { Eye } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useSearchParams } from 'react-router-dom';
import { Button } from '@/shared/ui/Button';
import { Card } from '@/shared/ui/Card';
import { PageHeader } from '@/shared/ui/PageHeader';
import { Spinner } from '@/shared/ui/Spinner';
import { formatCurrency, formatDateTime } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { ApprovalDecisionModal } from '@/features/approvals/ApprovalDecisionModal';
import { useApprovalsList } from '@/features/approvals/hooks';

export const ApprovalsPage = () => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const [searchParams, setSearchParams] = useSearchParams();

  const focusId = searchParams.get('focus');
  const [activeId, setActiveId] = useState<string | null>(focusId);
  const [trackedFocusId, setTrackedFocusId] = useState<string | null>(focusId);

  if (focusId !== trackedFocusId) {
    setTrackedFocusId(focusId);
    if (focusId) setActiveId(focusId);
  }

  const { data, isLoading } = useApprovalsList({ pageSize: 50 });

  const closeModal = () => {
    setActiveId(null);
    if (searchParams.has('focus')) {
      const next = new URLSearchParams(searchParams);
      next.delete('focus');
      setSearchParams(next, { replace: true });
    }
  };

  return (
    <div className="space-y-6">
      <PageHeader title={t('approvals.title')} subtitle={t('approvals.subtitle')} />

      <Card className="overflow-hidden">
        {isLoading ? (
          <div className="flex items-center gap-2 px-6 py-10 text-sm text-slate-500">
            <Spinner /> {t('common.loading')}
          </div>
        ) : (data?.items.length ?? 0) === 0 ? (
          <p className="px-6 py-10 text-center text-sm text-slate-400">{t('approvals.empty')}</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-slate-100 text-sm dark:divide-slate-800">
              <thead className="bg-slate-50 text-left text-xs uppercase tracking-wide text-slate-500 dark:bg-slate-900 dark:text-slate-400">
                <tr>
                  <th className="px-6 py-3 font-medium">{t('approvals.orderNumber')}</th>
                  <th className="px-6 py-3 font-medium">{t('approvals.dealer')}</th>
                  <th className="px-6 py-3 font-medium">{t('approvals.submittedAt')}</th>
                  <th className="px-6 py-3 text-right font-medium">{t('approvals.total')}</th>
                  <th className="px-6 py-3 text-right font-medium">{t('common.actions')}</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 bg-white dark:divide-slate-800 dark:bg-slate-950">
                {data!.items.map((o) => (
                  <tr key={o.id}>
                    <td className="px-6 py-3 font-medium text-slate-900 dark:text-slate-100">
                      {o.orderNumber}
                    </td>
                    <td className="px-6 py-3 text-slate-600 dark:text-slate-300">
                      {o.originDealerName ?? '—'}
                    </td>
                    <td className="px-6 py-3 text-slate-600 dark:text-slate-300">
                      {formatDateTime(o.orderDate, locale)}
                    </td>
                    <td className="px-6 py-3 text-right font-semibold text-slate-900 dark:text-slate-100">
                      {formatCurrency(o.total, locale, o.currency)}
                    </td>
                    <td className="px-6 py-3 text-right">
                      <div className="inline-flex gap-2">
                        <Button
                          size="sm"
                          variant="ghost"
                          onClick={() => setActiveId(o.id)}
                          aria-label={t('approvals.view')}
                        >
                          <Eye size={14} />
                          {t('approvals.view')}
                        </Button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>

      <ApprovalDecisionModal orderId={activeId} onClose={closeModal} />
    </div>
  );
};
