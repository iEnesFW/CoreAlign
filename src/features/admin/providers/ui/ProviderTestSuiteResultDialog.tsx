import { useTranslation } from 'react-i18next';
import { CheckCircle2, XCircle, Loader2 } from 'lucide-react';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { Badge } from '@/shared/ui/Badge/Badge';
import type { TestSuiteResult } from '../api/providersAdminApi';

interface Props {
  open: boolean;
  isRunning: boolean;
  result: TestSuiteResult | null;
  providerDisplayName: string | null;
  onClose: () => void;
}

export const ProviderTestSuiteResultDialog = ({
  open,
  isRunning,
  result,
  providerDisplayName,
  onClose,
}: Props) => {
  const { t } = useTranslation();

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={t('Admin.Providers.TestSuite.Title', { name: providerDisplayName ?? '' })}
      size="lg"
      footer={
        <Button variant="outline" onClick={onClose} type="button">
          {t('common.close')}
        </Button>
      }
    >
      {isRunning && (
        <div className="flex flex-col items-center justify-center py-10 text-slate-500 dark:text-slate-400">
          <Loader2 className="mb-3 h-8 w-8 animate-spin text-indigo-500" />
          <p className="text-sm">{t('Admin.Providers.TestSuite.Running')}</p>
        </div>
      )}

      {!isRunning && result && (
        <div className="space-y-3">
          <div className="flex items-center justify-between rounded-md border border-slate-200 bg-slate-50 px-3 py-2 dark:border-slate-700 dark:bg-slate-900/40">
            <span className="text-sm font-medium text-slate-700 dark:text-slate-200">
              {t('Admin.Providers.TestSuite.Outcome')}
            </span>
            {result.allPassed ? (
              <Badge variant="success" pill>
                {t('Admin.Providers.TestSuite.Success')}
              </Badge>
            ) : (
              <Badge variant="error" pill>
                {t('Admin.Providers.TestSuite.Failed')}
              </Badge>
            )}
          </div>

          <ol className="space-y-2">
            {result.steps.map((step, idx) => (
              <li
                key={`${step.stepName}-${idx}`}
                className="flex items-start gap-3 rounded-md border border-slate-200 px-3 py-2 dark:border-slate-700"
              >
                <div className="mt-0.5 shrink-0">
                  {step.passed ? (
                    <CheckCircle2 className="h-4 w-4 text-emerald-500" />
                  ) : (
                    <XCircle className="h-4 w-4 text-red-500" />
                  )}
                </div>
                <div className="min-w-0 flex-1">
                  <div className="flex items-center justify-between gap-2">
                    <p className="truncate text-sm font-medium text-slate-900 dark:text-slate-100">
                      {step.stepName}
                    </p>
                    <span className="shrink-0 text-[10px] tabular-nums text-slate-400 dark:text-slate-500">
                      {step.durationMs} ms
                    </span>
                  </div>
                  {step.detail && (
                    <p className="mt-0.5 break-words text-[11px] text-slate-500 dark:text-slate-400">
                      {step.detail}
                    </p>
                  )}
                </div>
              </li>
            ))}
          </ol>
        </div>
      )}

      {!isRunning && !result && (
        <p className="py-6 text-center text-sm text-slate-500 dark:text-slate-400">
          {t('Admin.Providers.TestSuite.NoResult')}
        </p>
      )}
    </Modal>
  );
};
