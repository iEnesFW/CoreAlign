import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Settings2, Activity, FlaskConical, Webhook, Star, StarOff } from 'lucide-react';
import { Badge } from '@/shared/ui/Badge/Badge';
import { toastApiError } from '@/shared/lib/mutationToast';
import { formatDateTime } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { ProviderHealthBadge } from './ProviderHealthBadge';
import { ProviderConfigModal } from './ProviderConfigModal';
import { ProviderTestSuiteResultDialog } from './ProviderTestSuiteResultDialog';
import { WebhookHistoryModal } from './WebhookHistoryModal';
import {
  useCheckProviderHealthMutation,
  useRunProviderTestSuiteMutation,
  useSetDefaultProviderMutation,
  useSetProviderEnabledMutation,
} from '../hooks/useProvidersAdmin';
import type { ProviderInfo, TestSuiteResult } from '../api/providersAdminApi';

interface Props {
  provider: ProviderInfo;
}

export const ProviderCard = ({ provider }: Props) => {
  const { t } = useTranslation();
  const locale = useFormatLocale();

  const [configOpen, setConfigOpen] = useState(false);
  const [webhookOpen, setWebhookOpen] = useState(false);
  const [testDialogOpen, setTestDialogOpen] = useState(false);
  const [testResult, setTestResult] = useState<TestSuiteResult | null>(null);

  const setEnabled = useSetProviderEnabledMutation();
  const setDefault = useSetDefaultProviderMutation();
  const checkHealth = useCheckProviderHealthMutation();
  const runTestSuite = useRunProviderTestSuiteMutation();

  const handleToggleEnabled = async () => {
    try {
      await setEnabled.mutateAsync({ provider, enabled: !provider.isEnabled });
      toast.success(
        provider.isEnabled
          ? t('Admin.Providers.Toast.Disabled')
          : t('Admin.Providers.Toast.Enabled'),
      );
    } catch (err) {
      toastApiError(err);
    }
  };

  const handleSetDefault = async () => {
    if (provider.isDefault) return;
    try {
      await setDefault.mutateAsync(provider);
      toast.success(t('Admin.Providers.Toast.DefaultSet'));
    } catch (err) {
      toastApiError(err);
    }
  };

  const handleCheckHealth = async () => {
    try {
      const snapshot = await checkHealth.mutateAsync({
        category: provider.category,
        name: provider.name,
      });
      if (snapshot.isHealthy) {
        toast.success(t('Admin.Providers.Toast.HealthOk'));
      } else {
        toast.error(snapshot.message ?? t('Admin.Providers.Toast.HealthFailed'));
      }
    } catch (err) {
      toastApiError(err);
    }
  };

  const handleRunTestSuite = async () => {
    setTestResult(null);
    setTestDialogOpen(true);
    try {
      const result = await runTestSuite.mutateAsync({
        category: provider.category,
        name: provider.name,
      });
      setTestResult(result);
    } catch (err) {
      toastApiError(err);
      setTestDialogOpen(false);
    }
  };

  const canRunTestSuite = provider.isConfigured && provider.isEnabled && provider.isSandbox;

  return (
    <>
      <article className="flex flex-col gap-3 rounded-xl border border-slate-200 bg-white p-4 shadow-sm transition-shadow hover:shadow-md dark:border-slate-800 dark:bg-slate-900">
        <header className="flex items-start justify-between gap-3">
          <div className="min-w-0">
            <div className="flex items-center gap-2">
              <h3 className="truncate text-sm font-semibold text-slate-900 dark:text-slate-100">
                {provider.displayName}
              </h3>
              {provider.isDefault && (
                <Badge variant="default" pill>
                  {t('Admin.Providers.Default')}
                </Badge>
              )}
              {provider.isSandbox ? (
                <Badge variant="warning" pill>
                  {t('Admin.Providers.Sandbox')}
                </Badge>
              ) : (
                <Badge variant="success" pill>
                  {t('Admin.Providers.Prod')}
                </Badge>
              )}
            </div>
            <p className="mt-0.5 text-[11px] text-slate-500 dark:text-slate-400">{provider.name}</p>
          </div>
          <ProviderHealthBadge
            status={provider.lastHealthStatus}
            isConfigured={provider.isConfigured}
          />
        </header>

        {provider.capabilities.length > 0 && (
          <div className="flex flex-wrap gap-1">
            {provider.capabilities.map((cap) => (
              <span
                key={cap}
                className="rounded-md bg-slate-100 px-1.5 py-0.5 text-[10px] font-medium text-slate-600 dark:bg-slate-800 dark:text-slate-300"
              >
                {cap}
              </span>
            ))}
          </div>
        )}

        {provider.lastHealthCheckedUtc && (
          <div className="text-[11px] text-slate-500 dark:text-slate-400">
            <span className="font-medium">{t('Admin.Providers.LastChecked')}: </span>
            {formatDateTime(provider.lastHealthCheckedUtc, locale)}
            {provider.lastHealthMessage && (
              <p className="mt-0.5 italic">{provider.lastHealthMessage}</p>
            )}
          </div>
        )}

        <div className="flex items-center justify-between gap-2">
          <label className="inline-flex cursor-pointer items-center gap-2">
            <span className="relative inline-block">
              <input
                type="checkbox"
                checked={provider.isEnabled}
                onChange={handleToggleEnabled}
                disabled={setEnabled.isPending}
                className="peer sr-only"
              />
              <span className="block h-5 w-9 rounded-full bg-slate-200 transition-colors peer-checked:bg-indigo-500 dark:bg-slate-700" />
              <span className="pointer-events-none absolute left-0.5 top-0.5 h-4 w-4 rounded-full bg-white transition-transform peer-checked:translate-x-4" />
            </span>
            <span className="text-xs font-medium text-slate-700 dark:text-slate-200">
              {provider.isEnabled
                ? t('Admin.Providers.Action.Enable')
                : t('Admin.Providers.Action.Disable')}
            </span>
          </label>

          <button
            type="button"
            onClick={handleSetDefault}
            disabled={provider.isDefault || setDefault.isPending}
            className="inline-flex items-center gap-1 rounded px-2 py-1 text-[11px] font-medium text-amber-600 hover:bg-amber-50 disabled:cursor-not-allowed disabled:opacity-50 dark:text-amber-300 dark:hover:bg-amber-500/10"
          >
            {provider.isDefault ? <Star size={12} fill="currentColor" /> : <StarOff size={12} />}
            {provider.isDefault
              ? t('Admin.Providers.Default')
              : t('Admin.Providers.Action.SetDefault')}
          </button>
        </div>

        <div className="grid grid-cols-2 gap-2 sm:grid-cols-4">
          <button
            type="button"
            onClick={() => setConfigOpen(true)}
            className="inline-flex items-center justify-center gap-1 rounded-md border border-slate-200 bg-white px-2 py-1.5 text-[11px] font-medium text-slate-700 transition-colors hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
          >
            <Settings2 size={12} />
            {t('Admin.Providers.Action.Configure')}
          </button>
          <button
            type="button"
            onClick={handleCheckHealth}
            disabled={checkHealth.isPending}
            className="inline-flex items-center justify-center gap-1 rounded-md border border-slate-200 bg-white px-2 py-1.5 text-[11px] font-medium text-slate-700 transition-colors hover:bg-slate-50 disabled:opacity-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
          >
            <Activity size={12} />
            {t('Admin.Providers.Action.CheckHealth')}
          </button>
          <button
            type="button"
            onClick={handleRunTestSuite}
            disabled={runTestSuite.isPending || !canRunTestSuite}
            title={!canRunTestSuite ? t('Admin.Providers.TestSuite.Unavailable') : undefined}
            className="inline-flex items-center justify-center gap-1 rounded-md border border-slate-200 bg-white px-2 py-1.5 text-[11px] font-medium text-slate-700 transition-colors hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
          >
            <FlaskConical size={12} />
            {t('Admin.Providers.Action.RunTestSuite')}
          </button>
          <button
            type="button"
            onClick={() => setWebhookOpen(true)}
            className="inline-flex items-center justify-center gap-1 rounded-md border border-slate-200 bg-white px-2 py-1.5 text-[11px] font-medium text-slate-700 transition-colors hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
          >
            <Webhook size={12} />
            {t('Admin.Providers.Action.WebhookHistory')}
          </button>
        </div>
      </article>

      <ProviderConfigModal
        open={configOpen}
        provider={provider}
        onClose={() => setConfigOpen(false)}
      />
      <WebhookHistoryModal
        open={webhookOpen}
        provider={provider}
        onClose={() => setWebhookOpen(false)}
      />
      <ProviderTestSuiteResultDialog
        open={testDialogOpen}
        isRunning={runTestSuite.isPending}
        result={testResult}
        providerDisplayName={provider.displayName}
        onClose={() => setTestDialogOpen(false)}
      />
    </>
  );
};
