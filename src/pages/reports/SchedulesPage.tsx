import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { ArrowLeft, PauseCircle, PlayCircle, Plus, Trash2 } from 'lucide-react';
import { apiClient } from '@/shared/api/apiClient';
import { safeRequest } from '@/shared/lib/safeRequest';
import { logger } from '@/shared/lib/logger';

type Frequency = 'Hourly' | 'Daily' | 'Weekly' | 'Monthly';
type Format = 'Pdf' | 'Xlsx';

interface ScheduleDto {
  id: string;
  name: string;
  reportKey: string;
  customReportDefinitionId: string | null;
  frequency: string;
  cronExpression: string | null;
  recipients: string[];
  format: string;
  filtersJson: string;
  isActive: boolean;
  nextRunAtUtc: string;
  lastRunAtUtc: string | null;
  lastRunStatus: string | null;
  lastRunError: string | null;
}

interface FormState {
  name: string;
  reportKey: string;
  frequency: Frequency;
  format: Format;
  recipientsText: string;
  startAtUtc: string;
}

const emptyForm: FormState = {
  name: '',
  reportKey: 'inventory-stock-on-hand',
  frequency: 'Daily',
  format: 'Pdf',
  recipientsText: '',
  startAtUtc: '',
};

export const SchedulesPage = () => {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [items, setItems] = useState<ScheduleDto[]>([]);
  const [creating, setCreating] = useState(false);
  const [form, setForm] = useState<FormState>(emptyForm);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const refresh = async () => {
    const [response, err] = await safeRequest(
      apiClient.get<{ data: ScheduleDto[] }>('/reports/schedules'),
    );
    if (err) {
      logger.error('Failed to load schedules', err);
      return;
    }
    if (response) setItems(response.data.data);
  };

  useEffect(() => {
    let cancelled = false;
    const initial = async () => {
      const [response, err] = await safeRequest(
        apiClient.get<{ data: ScheduleDto[] }>('/reports/schedules'),
      );
      if (cancelled) return;
      if (err) {
        logger.error('Failed to load schedules', err);
        return;
      }
      if (response) setItems(response.data.data);
    };
    void initial();
    return () => {
      cancelled = true;
    };
  }, []);

  const submit = async () => {
    setErrorMessage(null);
    const recipients = form.recipientsText
      .split(',')
      .map((s) => s.trim())
      .filter((s) => s.length > 0);
    if (recipients.length === 0) {
      setErrorMessage(t('reports.schedules.error.recipients'));
      return;
    }
    if (!form.name.trim()) {
      setErrorMessage(t('reports.schedules.error.name'));
      return;
    }
    const [, err] = await safeRequest(
      apiClient.post('/reports/schedules', {
        name: form.name.trim(),
        reportKey: form.reportKey,
        customReportDefinitionId: null,
        frequency: form.frequency,
        cronExpression: null,
        recipients,
        format: form.format,
        filtersJson: '{}',
        startAtUtc: form.startAtUtc ? new Date(form.startAtUtc).toISOString() : null,
      }),
    );
    if (err) {
      logger.error('Failed to create schedule', err);
      setErrorMessage(t('reports.schedules.error.save'));
      return;
    }
    setCreating(false);
    setForm(emptyForm);
    await refresh();
  };

  const toggleActive = async (s: ScheduleDto) => {
    await safeRequest(
      apiClient.put(`/reports/schedules/${s.id}`, {
        name: s.name,
        reportKey: s.reportKey,
        customReportDefinitionId: s.customReportDefinitionId,
        frequency: s.frequency,
        cronExpression: s.cronExpression,
        recipients: s.recipients,
        format: s.format,
        filtersJson: s.filtersJson,
        isActive: !s.isActive,
      }),
    );
    await refresh();
  };

  const remove = async (id: string) => {
    await safeRequest(apiClient.delete(`/reports/schedules/${id}`));
    await refresh();
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <button
            type="button"
            onClick={() => navigate('/dashboard/reports')}
            className="mb-2 inline-flex items-center gap-2 text-sm text-slate-500 hover:text-slate-700 dark:text-slate-400 dark:hover:text-slate-200"
          >
            <ArrowLeft className="h-4 w-4" /> {t('common.back')}
          </button>
          <h1 className="text-2xl font-semibold text-slate-900 dark:text-slate-50">
            {t('reports.schedules.title')}
          </h1>
          <p className="text-sm text-slate-500 dark:text-slate-400">
            {t('reports.schedules.subtitle')}
          </p>
        </div>
        <button
          type="button"
          onClick={() => setCreating(true)}
          className="inline-flex items-center gap-2 rounded-md bg-emerald-600 px-3 py-2 text-sm font-medium text-white hover:bg-emerald-500"
        >
          <Plus className="h-4 w-4" /> {t('reports.schedules.new')}
        </button>
      </div>

      {errorMessage && (
        <div className="rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700 dark:border-red-900/50 dark:bg-red-950/40 dark:text-red-300">
          {errorMessage}
        </div>
      )}

      {creating && (
        <div className="rounded-md border border-slate-200 bg-white p-4 dark:border-slate-700 dark:bg-slate-900">
          <h2 className="mb-3 text-sm font-semibold text-slate-700 dark:text-slate-200">
            {t('reports.schedules.newTitle')}
          </h2>
          <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
            <label className="block text-sm">
              <span className="text-xs font-medium uppercase tracking-wide text-slate-500 dark:text-slate-400">
                {t('reports.schedules.name')}
              </span>
              <input
                value={form.name}
                onChange={(e) => setForm({ ...form, name: e.target.value })}
                className="mt-1 w-full rounded-md border border-slate-300 bg-white px-3 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
              />
            </label>
            <label className="block text-sm">
              <span className="text-xs font-medium uppercase tracking-wide text-slate-500 dark:text-slate-400">
                {t('reports.schedules.reportKey')}
              </span>
              <input
                value={form.reportKey}
                onChange={(e) => setForm({ ...form, reportKey: e.target.value })}
                className="mt-1 w-full rounded-md border border-slate-300 bg-white px-3 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
              />
            </label>
            <label className="block text-sm">
              <span className="text-xs font-medium uppercase tracking-wide text-slate-500 dark:text-slate-400">
                {t('reports.schedules.frequency')}
              </span>
              <select
                value={form.frequency}
                onChange={(e) => setForm({ ...form, frequency: e.target.value as Frequency })}
                className="mt-1 w-full rounded-md border border-slate-300 bg-white px-3 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
              >
                <option value="Hourly">Hourly</option>
                <option value="Daily">Daily</option>
                <option value="Weekly">Weekly</option>
                <option value="Monthly">Monthly</option>
              </select>
            </label>
            <label className="block text-sm">
              <span className="text-xs font-medium uppercase tracking-wide text-slate-500 dark:text-slate-400">
                {t('reports.schedules.format')}
              </span>
              <select
                value={form.format}
                onChange={(e) => setForm({ ...form, format: e.target.value as Format })}
                className="mt-1 w-full rounded-md border border-slate-300 bg-white px-3 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
              >
                <option value="Pdf">PDF</option>
                <option value="Xlsx">Excel</option>
              </select>
            </label>
            <label className="block text-sm md:col-span-2">
              <span className="text-xs font-medium uppercase tracking-wide text-slate-500 dark:text-slate-400">
                {t('reports.schedules.recipients')}
              </span>
              <input
                value={form.recipientsText}
                onChange={(e) => setForm({ ...form, recipientsText: e.target.value })}
                placeholder="ops@example.com, cfo@example.com"
                className="mt-1 w-full rounded-md border border-slate-300 bg-white px-3 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
              />
            </label>
            <label className="block text-sm md:col-span-2">
              <span className="text-xs font-medium uppercase tracking-wide text-slate-500 dark:text-slate-400">
                {t('reports.schedules.startAt')}
              </span>
              <input
                type="datetime-local"
                value={form.startAtUtc}
                onChange={(e) => setForm({ ...form, startAtUtc: e.target.value })}
                className="mt-1 w-full rounded-md border border-slate-300 bg-white px-3 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
              />
            </label>
          </div>
          <div className="mt-4 flex justify-end gap-2">
            <button
              type="button"
              onClick={() => setCreating(false)}
              className="rounded-md border border-slate-300 px-3 py-1.5 text-sm dark:border-slate-700"
            >
              {t('common.cancel')}
            </button>
            <button
              type="button"
              onClick={submit}
              className="rounded-md bg-emerald-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-emerald-500"
            >
              {t('common.save')}
            </button>
          </div>
        </div>
      )}

      <div className="overflow-x-auto rounded-md border border-slate-200 bg-white dark:border-slate-700 dark:bg-slate-900">
        <table className="min-w-full divide-y divide-slate-200 text-sm dark:divide-slate-700">
          <thead className="bg-slate-50 dark:bg-slate-800/40">
            <tr>
              <th className="px-3 py-2 text-left text-xs font-medium uppercase tracking-wide text-slate-500 dark:text-slate-400">
                {t('reports.schedules.cols.name')}
              </th>
              <th className="px-3 py-2 text-left text-xs font-medium uppercase tracking-wide text-slate-500 dark:text-slate-400">
                {t('reports.schedules.cols.reportKey')}
              </th>
              <th className="px-3 py-2 text-left text-xs font-medium uppercase tracking-wide text-slate-500 dark:text-slate-400">
                {t('reports.schedules.cols.frequency')}
              </th>
              <th className="px-3 py-2 text-left text-xs font-medium uppercase tracking-wide text-slate-500 dark:text-slate-400">
                {t('reports.schedules.cols.recipients')}
              </th>
              <th className="px-3 py-2 text-left text-xs font-medium uppercase tracking-wide text-slate-500 dark:text-slate-400">
                {t('reports.schedules.cols.nextRun')}
              </th>
              <th className="px-3 py-2 text-left text-xs font-medium uppercase tracking-wide text-slate-500 dark:text-slate-400">
                {t('reports.schedules.cols.status')}
              </th>
              <th className="px-3 py-2 text-right text-xs font-medium uppercase tracking-wide text-slate-500 dark:text-slate-400">
                {t('reports.schedules.cols.actions')}
              </th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
            {items.length === 0 && (
              <tr>
                <td
                  colSpan={7}
                  className="px-3 py-6 text-center text-sm text-slate-500 dark:text-slate-400"
                >
                  {t('reports.schedules.empty')}
                </td>
              </tr>
            )}
            {items.map((s) => (
              <tr key={s.id}>
                <td className="px-3 py-2 font-medium text-slate-800 dark:text-slate-100">
                  {s.name}
                </td>
                <td className="px-3 py-2 text-slate-700 dark:text-slate-300">{s.reportKey}</td>
                <td className="px-3 py-2 text-slate-700 dark:text-slate-300">{s.frequency}</td>
                <td className="px-3 py-2 text-slate-700 dark:text-slate-300">
                  {s.recipients.join(', ')}
                </td>
                <td className="px-3 py-2 text-slate-700 dark:text-slate-300">
                  {new Date(s.nextRunAtUtc).toLocaleString()}
                </td>
                <td className="px-3 py-2">
                  <span
                    className={`inline-flex rounded-full px-2 py-0.5 text-xs ${
                      s.isActive
                        ? 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-300'
                        : 'bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-300'
                    }`}
                  >
                    {s.isActive ? t('reports.schedules.active') : t('reports.schedules.inactive')}
                  </span>
                </td>
                <td className="px-3 py-2 text-right">
                  <div className="inline-flex items-center gap-1">
                    <button
                      type="button"
                      onClick={() => void toggleActive(s)}
                      className="text-slate-500 hover:text-slate-700 dark:text-slate-400 dark:hover:text-slate-200"
                    >
                      {s.isActive ? (
                        <PauseCircle className="h-4 w-4" />
                      ) : (
                        <PlayCircle className="h-4 w-4" />
                      )}
                    </button>
                    <button
                      type="button"
                      onClick={() => void remove(s.id)}
                      className="text-slate-400 hover:text-red-500"
                    >
                      <Trash2 className="h-4 w-4" />
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};

export default SchedulesPage;
