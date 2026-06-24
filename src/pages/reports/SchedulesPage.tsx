import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { CalendarClock, PauseCircle, PlayCircle, Plus, Trash2 } from 'lucide-react';
import { apiClient } from '@/shared/api/apiClient';
import { safeRequest } from '@/shared/lib/safeRequest';
import { logger } from '@/shared/lib/logger';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { ListPageTemplate } from '@/shared/ui/PageTemplate/PageTemplate';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { Select } from '@/shared/ui/Select/Select';
import { Badge } from '@/shared/ui/Badge/Badge';

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
    <ListPageTemplate
      header={
        <PageHeader
          icon={<CalendarClock size={20} />}
          title={t('reports.schedules.title')}
          subtitle={t('reports.schedules.subtitle')}
          crumbs={[{ label: t('common.back'), to: '/dashboard/reports' }]}
          actions={
            <Button size="sm" onClick={() => setCreating(true)}>
              <Plus size={14} />
              {t('reports.schedules.new')}
            </Button>
          }
        />
      }
    >
      {errorMessage && (
        <div className="rounded-md border border-danger-200 bg-danger-50 px-3 py-2 text-sm text-danger-700 dark:border-danger-900/50 dark:bg-danger-950/40 dark:text-danger-300">
          {errorMessage}
        </div>
      )}

      {creating && (
        <div className="rounded-md border border-slate-200 bg-white p-4 dark:border-slate-700 dark:bg-slate-900">
          <h2 className="mb-3 text-sm font-semibold text-slate-700 dark:text-slate-200">
            {t('reports.schedules.newTitle')}
          </h2>
          <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
            <Input
              label={t('reports.schedules.name')}
              value={form.name}
              onChange={(e) => setForm({ ...form, name: e.target.value })}
            />
            <Input
              label={t('reports.schedules.reportKey')}
              value={form.reportKey}
              onChange={(e) => setForm({ ...form, reportKey: e.target.value })}
            />
            <Select
              label={t('reports.schedules.frequency')}
              value={form.frequency}
              onChange={(e) => setForm({ ...form, frequency: e.target.value as Frequency })}
            >
              <option value="Hourly">Hourly</option>
              <option value="Daily">Daily</option>
              <option value="Weekly">Weekly</option>
              <option value="Monthly">Monthly</option>
            </Select>
            <Select
              label={t('reports.schedules.format')}
              value={form.format}
              onChange={(e) => setForm({ ...form, format: e.target.value as Format })}
            >
              <option value="Pdf">PDF</option>
              <option value="Xlsx">Excel</option>
            </Select>
            <Input
              label={t('reports.schedules.recipients')}
              className="md:col-span-2"
              value={form.recipientsText}
              onChange={(e) => setForm({ ...form, recipientsText: e.target.value })}
              placeholder="ops@example.com, cfo@example.com"
            />
            <Input
              label={t('reports.schedules.startAt')}
              className="md:col-span-2"
              type="datetime-local"
              value={form.startAtUtc}
              onChange={(e) => setForm({ ...form, startAtUtc: e.target.value })}
            />
          </div>
          <div className="mt-4 flex justify-end gap-2">
            <Button type="button" variant="secondary" size="sm" onClick={() => setCreating(false)}>
              {t('common.cancel')}
            </Button>
            <Button type="button" size="sm" onClick={submit}>
              {t('common.save')}
            </Button>
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
                  <Badge variant={s.isActive ? 'success' : 'neutral'} pill>
                    {s.isActive ? t('reports.schedules.active') : t('reports.schedules.inactive')}
                  </Badge>
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
                      className="text-slate-400 hover:text-danger-500"
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
    </ListPageTemplate>
  );
};

export default SchedulesPage;
