import { useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Save } from 'lucide-react';
import { safeRequestWithNotify } from '@/shared/lib/safeRequest';
import { useAuditScheduleQuery, useUpsertAuditSchedule } from '../useAuditAdmin';
import type {
  AuditExportFrequency,
  AuditLogExportFormat,
  ScheduledAuditExportConfig,
  UpsertScheduledAuditExportBody,
} from '../auditAdminApi';

const FREQUENCIES: AuditExportFrequency[] = ['Daily', 'Weekly', 'Monthly'];
const FORMATS: AuditLogExportFormat[] = ['Csv', 'Json', 'Excel'];

const defaultDraft: UpsertScheduledAuditExportBody = {
  enabled: false,
  frequency: 'Weekly',
  format: 'Csv',
  lookbackDays: 7,
  recipients: [],
  entityTypes: null,
};

const fromConfig = (
  config: ScheduledAuditExportConfig | null | undefined,
): UpsertScheduledAuditExportBody => {
  if (!config) return defaultDraft;
  return {
    enabled: config.enabled,
    frequency: config.frequency,
    format: config.format,
    lookbackDays: config.lookbackDays,
    recipients: config.recipients ?? [],
    entityTypes: config.entityTypes ?? null,
  };
};

export const AuditExportScheduleEditor = () => {
  const { t } = useTranslation();
  const query = useAuditScheduleQuery();
  const upsert = useUpsertAuditSchedule();
  const [draft, setDraft] = useState<UpsertScheduledAuditExportBody>(defaultDraft);
  const [recipientsText, setRecipientsText] = useState('');
  const [entityTypesText, setEntityTypesText] = useState('');
  const lastSyncedRef = useRef<ScheduledAuditExportConfig | null | undefined>(undefined);

  if (query.data?.data !== lastSyncedRef.current) {
    lastSyncedRef.current = query.data?.data;
    const next = fromConfig(query.data?.data);
    setDraft(next);
    setRecipientsText((next.recipients ?? []).join(', '));
    setEntityTypesText((next.entityTypes ?? []).join(', '));
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const recipients = parseCsv(recipientsText);
    const entityTypes = parseCsv(entityTypesText);
    const payload: UpsertScheduledAuditExportBody = {
      ...draft,
      recipients,
      entityTypes: entityTypes.length > 0 ? entityTypes : null,
    };
    await safeRequestWithNotify(upsert.mutateAsync(payload), {
      successMessage: t('Audit.Admin.ScheduleSaved'),
    });
  };

  return (
    <form
      onSubmit={handleSubmit}
      className="space-y-4 rounded-lg border border-slate-200 bg-white p-4 shadow-sm dark:border-slate-700 dark:bg-slate-800"
    >
      <div className="flex items-center gap-2">
        <input
          id="schedule-enabled"
          type="checkbox"
          checked={draft.enabled}
          onChange={(e) => setDraft({ ...draft, enabled: e.target.checked })}
          className="h-4 w-4 rounded border-slate-300"
        />
        <label
          htmlFor="schedule-enabled"
          className="text-sm font-medium text-slate-700 dark:text-slate-200"
        >
          {t('Audit.Admin.ScheduleEnabled')}
        </label>
      </div>

      <div className="grid gap-3 sm:grid-cols-2">
        <div>
          <label
            htmlFor="schedule-frequency"
            className="block text-xs font-medium text-slate-700 dark:text-slate-200"
          >
            {t('Audit.Admin.Frequency')}
          </label>
          <select
            id="schedule-frequency"
            value={draft.frequency}
            onChange={(e) =>
              setDraft({ ...draft, frequency: e.target.value as AuditExportFrequency })
            }
            className="mt-1 w-full rounded-md border border-slate-300 bg-white px-2 py-1.5 text-sm text-slate-900 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
          >
            {FREQUENCIES.map((f) => (
              <option key={f} value={f}>
                {t(`Audit.Admin.Frequency.${f}`)}
              </option>
            ))}
          </select>
        </div>
        <div>
          <label
            htmlFor="schedule-format"
            className="block text-xs font-medium text-slate-700 dark:text-slate-200"
          >
            {t('Audit.Admin.Format')}
          </label>
          <select
            id="schedule-format"
            value={draft.format}
            onChange={(e) => setDraft({ ...draft, format: e.target.value as AuditLogExportFormat })}
            className="mt-1 w-full rounded-md border border-slate-300 bg-white px-2 py-1.5 text-sm text-slate-900 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
          >
            {FORMATS.map((f) => (
              <option key={f} value={f}>
                {f}
              </option>
            ))}
          </select>
        </div>
        <div>
          <label
            htmlFor="schedule-lookback"
            className="block text-xs font-medium text-slate-700 dark:text-slate-200"
          >
            {t('Audit.Admin.LookbackDays')}
          </label>
          <input
            id="schedule-lookback"
            type="number"
            min={1}
            max={365}
            value={draft.lookbackDays}
            onChange={(e) =>
              setDraft({ ...draft, lookbackDays: Number.parseInt(e.target.value, 10) || 1 })
            }
            className="mt-1 w-full rounded-md border border-slate-300 bg-white px-2 py-1.5 text-sm text-slate-900 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
          />
        </div>
      </div>

      <div>
        <label
          htmlFor="schedule-recipients"
          className="block text-xs font-medium text-slate-700 dark:text-slate-200"
        >
          {t('Audit.Admin.Recipients')}
        </label>
        <input
          id="schedule-recipients"
          type="text"
          value={recipientsText}
          onChange={(e) => setRecipientsText(e.target.value)}
          placeholder="compliance@example.com, dpo@example.com"
          className="mt-1 w-full rounded-md border border-slate-300 bg-white px-2 py-1.5 text-sm text-slate-900 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
        />
      </div>

      <div>
        <label
          htmlFor="schedule-entityTypes"
          className="block text-xs font-medium text-slate-700 dark:text-slate-200"
        >
          {t('Audit.Admin.EntityTypesFilter')}
        </label>
        <input
          id="schedule-entityTypes"
          type="text"
          value={entityTypesText}
          onChange={(e) => setEntityTypesText(e.target.value)}
          placeholder="Order, Invoice, Customer"
          className="mt-1 w-full rounded-md border border-slate-300 bg-white px-2 py-1.5 text-sm text-slate-900 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
        />
      </div>

      {query.data?.data?.lastRunAtUtc && (
        <div className="rounded-md bg-slate-50 p-3 text-xs text-slate-600 dark:bg-slate-900 dark:text-slate-300">
          <p>
            {t('Audit.Admin.LastRun')}: {query.data.data.lastRunAtUtc}{' '}
            <span className="ml-2 font-medium">[{query.data.data.lastRunStatus}]</span>
          </p>
          {query.data.data.lastRunError && (
            <p className="mt-1 text-danger-600 dark:text-danger-400">
              {query.data.data.lastRunError}
            </p>
          )}
        </div>
      )}

      <div className="flex justify-end">
        <button
          type="submit"
          disabled={upsert.isPending}
          className="inline-flex items-center gap-2 rounded-md bg-success-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-success-700 disabled:opacity-50"
        >
          <Save size={14} />
          {t('Audit.Admin.SaveSchedule')}
        </button>
      </div>
    </form>
  );
};

const parseCsv = (raw: string): string[] =>
  raw
    .split(',')
    .map((token) => token.trim())
    .filter((token) => token.length > 0);
