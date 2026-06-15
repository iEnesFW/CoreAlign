import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Plus, Save } from 'lucide-react';
import { safeRequestWithNotify } from '@/shared/lib/safeRequest';
import {
  useCreateRetentionPolicy,
  useRetentionPoliciesQuery,
  useUpdateRetentionPolicy,
} from '../hooks/usePrivacyRequests';
import type { RetentionActionOnExpiry, UpsertRetentionPolicyBody } from '../model/privacy.types';

const ENTITY_TYPES = [
  'User',
  'Order',
  'Invoice',
  'AuditLog',
  'Notification',
  'ActivityLog',
  'UserSession',
];
const ACTIONS: RetentionActionOnExpiry[] = ['Anonymize', 'Archive', 'Delete'];

const defaultDraft: UpsertRetentionPolicyBody = {
  entityType: 'Notification',
  retentionDays: 365,
  actionOnExpiry: 'Anonymize',
  keepFinancialTrail: true,
  isEnabled: true,
};

export const RetentionPolicyEditor = () => {
  const { t } = useTranslation();
  const query = useRetentionPoliciesQuery();
  const create = useCreateRetentionPolicy();
  const update = useUpdateRetentionPolicy();
  const [draft, setDraft] = useState<UpsertRetentionPolicyBody>(defaultDraft);

  const policies = query.data?.data ?? [];

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    const [result] = await safeRequestWithNotify(create.mutateAsync(draft), {
      successMessage: t('Privacy.Retention.CreateSuccess'),
    });
    if (result) setDraft(defaultDraft);
  };

  const handleToggle = async (id: string, current: UpsertRetentionPolicyBody) => {
    await safeRequestWithNotify(
      update.mutateAsync({ id, body: { ...current, isEnabled: !current.isEnabled } }),
      { successMessage: t('Privacy.Retention.UpdateSuccess') },
    );
  };

  return (
    <div className="space-y-6">
      <section className="rounded-lg border border-slate-200 bg-white p-5 shadow-sm dark:border-slate-700 dark:bg-slate-800">
        <h3 className="mb-3 text-base font-semibold text-slate-900 dark:text-slate-100">
          {t('Privacy.Retention.ExistingPolicies')}
        </h3>
        <table className="w-full text-sm">
          <thead className="bg-slate-50 dark:bg-slate-900">
            <tr>
              <th className="px-3 py-2 text-left font-semibold text-slate-700 dark:text-slate-200">
                {t('Privacy.Retention.EntityType')}
              </th>
              <th className="px-3 py-2 text-left font-semibold text-slate-700 dark:text-slate-200">
                {t('Privacy.Retention.Days')}
              </th>
              <th className="px-3 py-2 text-left font-semibold text-slate-700 dark:text-slate-200">
                {t('Privacy.Retention.Action')}
              </th>
              <th className="px-3 py-2 text-left font-semibold text-slate-700 dark:text-slate-200">
                {t('Privacy.Retention.Enabled')}
              </th>
              <th className="px-3 py-2 text-left font-semibold text-slate-700 dark:text-slate-200">
                {t('Privacy.Retention.LastRun')}
              </th>
            </tr>
          </thead>
          <tbody>
            {policies.length === 0 && (
              <tr>
                <td
                  colSpan={5}
                  className="px-3 py-6 text-center text-slate-500 dark:text-slate-400"
                >
                  {t('Privacy.Retention.Empty')}
                </td>
              </tr>
            )}
            {policies.map((p) => (
              <tr key={p.id} className="border-t border-slate-100 dark:border-slate-700">
                <td className="px-3 py-2">{p.entityType}</td>
                <td className="px-3 py-2 tabular-nums">{p.retentionDays}</td>
                <td className="px-3 py-2">
                  {t(`Privacy.Retention.ActionOption.${p.actionOnExpiry}`)}
                </td>
                <td className="px-3 py-2">
                  <button
                    type="button"
                    onClick={() =>
                      handleToggle(p.id, {
                        entityType: p.entityType,
                        retentionDays: p.retentionDays,
                        actionOnExpiry: p.actionOnExpiry,
                        keepFinancialTrail: p.keepFinancialTrail,
                        isEnabled: p.isEnabled,
                      })
                    }
                    className={
                      p.isEnabled
                        ? 'rounded-full bg-emerald-100 px-2 py-0.5 text-xs font-semibold text-emerald-700 dark:bg-emerald-500/20 dark:text-emerald-300'
                        : 'rounded-full bg-slate-200 px-2 py-0.5 text-xs font-semibold text-slate-600 dark:bg-slate-700 dark:text-slate-300'
                    }
                  >
                    {p.isEnabled ? t('Common.Enabled') : t('Common.Disabled')}
                  </button>
                </td>
                <td className="px-3 py-2 text-slate-500 dark:text-slate-400">
                  {p.lastRunAtUtc ?? '—'}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>

      <section className="rounded-lg border border-slate-200 bg-white p-5 shadow-sm dark:border-slate-700 dark:bg-slate-800">
        <h3 className="mb-3 text-base font-semibold text-slate-900 dark:text-slate-100">
          {t('Privacy.Retention.AddPolicy')}
        </h3>
        <form onSubmit={handleCreate} className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
          <div>
            <label
              htmlFor="ret-entity"
              className="block text-xs font-medium text-slate-600 dark:text-slate-300"
            >
              {t('Privacy.Retention.EntityType')}
            </label>
            <select
              id="ret-entity"
              value={draft.entityType}
              onChange={(e) => setDraft({ ...draft, entityType: e.target.value })}
              className="mt-1 block w-full rounded-md border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-900"
            >
              {ENTITY_TYPES.map((et) => (
                <option key={et} value={et}>
                  {et}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label
              htmlFor="ret-days"
              className="block text-xs font-medium text-slate-600 dark:text-slate-300"
            >
              {t('Privacy.Retention.Days')}
            </label>
            <input
              id="ret-days"
              type="number"
              min={1}
              value={draft.retentionDays}
              onChange={(e) => setDraft({ ...draft, retentionDays: Number(e.target.value) })}
              className="mt-1 block w-full rounded-md border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-900"
            />
          </div>
          <div>
            <label
              htmlFor="ret-action"
              className="block text-xs font-medium text-slate-600 dark:text-slate-300"
            >
              {t('Privacy.Retention.Action')}
            </label>
            <select
              id="ret-action"
              value={draft.actionOnExpiry}
              onChange={(e) =>
                setDraft({ ...draft, actionOnExpiry: e.target.value as RetentionActionOnExpiry })
              }
              className="mt-1 block w-full rounded-md border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-900"
            >
              {ACTIONS.map((a) => (
                <option key={a} value={a}>
                  {t(`Privacy.Retention.ActionOption.${a}`)}
                </option>
              ))}
            </select>
          </div>
          <div className="flex items-end">
            <button
              type="submit"
              disabled={create.isPending}
              className="inline-flex w-full items-center justify-center gap-2 rounded-md bg-indigo-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-indigo-700 disabled:opacity-50"
            >
              {create.isPending ? <Save size={14} /> : <Plus size={14} />}
              {t('Privacy.Retention.AddButton')}
            </button>
          </div>
        </form>
      </section>
    </div>
  );
};
