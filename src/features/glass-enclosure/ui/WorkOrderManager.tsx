import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { AlertOctagon, ArrowRight, Check, Hammer, PackageCheck } from 'lucide-react';
import {
  useRecordDefectMutation,
  useUpdateWorkOrderStatusMutation,
  useWorkOrdersQuery,
} from '../hooks/useGlassProjectQueries';
import { safeRequestWithNotify } from '@/shared/lib/safeRequest';

interface WorkOrderManagerProps {
  projectId: string;
}

const STATUS_FLOW: Record<string, string[]> = {
  Pending: ['Cutting'],
  Cutting: ['Assembling'],
  Assembling: ['Ready'],
  Ready: ['InTransit', 'Installed'],
  InTransit: ['Installed'],
  Defective: ['Cutting'],
};

const STATUS_LABEL_KEYS: Record<string, string> = {
  Pending: 'GlassEnclosure.WorkOrder.Status.Pending',
  Cutting: 'GlassEnclosure.WorkOrder.Status.Cutting',
  Assembling: 'GlassEnclosure.WorkOrder.Status.Assembling',
  Ready: 'GlassEnclosure.WorkOrder.Status.Ready',
  InTransit: 'GlassEnclosure.WorkOrder.Status.InTransit',
  Installed: 'GlassEnclosure.WorkOrder.Status.Installed',
  Defective: 'GlassEnclosure.WorkOrder.Status.Defective',
};

const STATUS_BADGE: Record<string, string> = {
  Pending: 'bg-slate-200 text-slate-700',
  Cutting: 'bg-amber-100 text-amber-700',
  Assembling: 'bg-blue-100 text-blue-700',
  Ready: 'bg-teal-100 text-teal-700',
  InTransit: 'bg-orange-100 text-orange-700',
  Installed: 'bg-emerald-100 text-emerald-700',
  Defective: 'bg-red-100 text-red-700',
};

export function WorkOrderManager({ projectId }: WorkOrderManagerProps) {
  const { t, i18n } = useTranslation();
  const workOrdersQuery = useWorkOrdersQuery(projectId);
  const updateStatus = useUpdateWorkOrderStatusMutation();
  const recordDefect = useRecordDefectMutation();
  const [defectingId, setDefectingId] = useState<string | null>(null);
  const [defectNotes, setDefectNotes] = useState('');
  const dateFormatter = useMemo(
    () => new Intl.DateTimeFormat(i18n.language, { dateStyle: 'short', timeStyle: 'short' }),
    [i18n.language],
  );

  const orders = workOrdersQuery.data?.data ?? [];

  if (workOrdersQuery.isLoading) {
    return <p className="text-xs text-slate-500 dark:text-slate-400">{t('Common.Loading')}</p>;
  }
  if (orders.length === 0) {
    return (
      <p className="text-xs text-slate-500 dark:text-slate-400">
        {t('GlassEnclosure.WorkOrder.None')}
      </p>
    );
  }

  const handleAdvance = async (workOrderId: string, status: string) => {
    await safeRequestWithNotify(updateStatus.mutateAsync({ workOrderId, status, projectId }), {
      successMessage: t('GlassEnclosure.WorkOrder.Updated'),
    });
  };

  const handleSubmitDefect = async (workOrderId: string) => {
    if (!defectNotes.trim()) return;
    await safeRequestWithNotify(
      recordDefect.mutateAsync({ workOrderId, defectNotes: defectNotes.trim(), projectId }),
      { successMessage: t('GlassEnclosure.WorkOrder.DefectRecorded') },
    );
    setDefectingId(null);
    setDefectNotes('');
  };

  return (
    <ul className="space-y-2">
      {orders.map((wo) => {
        const nextStatuses = STATUS_FLOW[wo.status] ?? [];
        return (
          <li
            key={wo.id}
            className="rounded border border-slate-200 bg-white p-2 text-xs dark:border-slate-700 dark:bg-slate-800"
          >
            <div className="flex items-center justify-between">
              <span className="font-mono text-slate-700 dark:text-slate-300">
                {wo.workloadM2.toFixed(2)} m²
              </span>
              <span
                className={`rounded px-1.5 py-0.5 text-[10px] font-medium ${STATUS_BADGE[wo.status] ?? STATUS_BADGE.Pending} dark:bg-opacity-30`}
              >
                {t(STATUS_LABEL_KEYS[wo.status] as never, { defaultValue: wo.status })}
              </span>
            </div>
            <div className="mt-1 text-[10px] text-slate-500 dark:text-slate-400">
              {dateFormatter.format(new Date(wo.scheduledStartDate))} →{' '}
              {dateFormatter.format(new Date(wo.scheduledEndDate))}
            </div>
            {wo.recutCount > 0 && (
              <div className="mt-1 text-[10px] text-amber-600 dark:text-amber-400">
                recut: {wo.recutCount}
              </div>
            )}
            {wo.defectNotes && (
              <p
                className="mt-1 truncate text-[10px] text-red-600 dark:text-red-400"
                title={wo.defectNotes}
              >
                {wo.defectNotes}
              </p>
            )}
            <div className="mt-2 flex flex-wrap gap-1.5">
              {nextStatuses.map((s) => (
                <button
                  key={s}
                  type="button"
                  onClick={() => handleAdvance(wo.id, s)}
                  disabled={updateStatus.isPending}
                  className="inline-flex items-center gap-1 rounded border border-blue-600 px-1.5 py-0.5 text-[10px] font-medium text-blue-700 hover:bg-blue-50 disabled:opacity-50 dark:border-blue-500/40 dark:text-blue-300 dark:hover:bg-blue-950/30"
                >
                  {s === 'Ready' ? (
                    <PackageCheck size={10} />
                  ) : s === 'Installed' ? (
                    <Check size={10} />
                  ) : (
                    <ArrowRight size={10} />
                  )}
                  {t(STATUS_LABEL_KEYS[s] as never, { defaultValue: s })}
                </button>
              ))}
              {wo.status !== 'Defective' && wo.status !== 'Installed' && (
                <button
                  type="button"
                  onClick={() => {
                    setDefectingId(wo.id === defectingId ? null : wo.id);
                    setDefectNotes('');
                  }}
                  className="inline-flex items-center gap-1 rounded border border-red-500/50 px-1.5 py-0.5 text-[10px] font-medium text-red-600 hover:bg-red-50 dark:hover:bg-red-950/30"
                >
                  <AlertOctagon size={10} />
                  {t('GlassEnclosure.WorkOrder.Defect')}
                </button>
              )}
            </div>
            {defectingId === wo.id && (
              <div className="mt-2 space-y-1.5">
                <textarea
                  rows={2}
                  value={defectNotes}
                  onChange={(e) => setDefectNotes(e.target.value)}
                  placeholder={t('GlassEnclosure.WorkOrder.DefectPlaceholder')}
                  className="w-full rounded border border-slate-300 bg-white p-1.5 text-xs dark:border-slate-700 dark:bg-slate-900"
                />
                <button
                  type="button"
                  onClick={() => handleSubmitDefect(wo.id)}
                  disabled={!defectNotes.trim() || recordDefect.isPending}
                  className="inline-flex items-center gap-1 rounded-md bg-red-600 px-2 py-1 text-[10px] font-medium text-white hover:bg-red-700 disabled:opacity-50"
                >
                  <Hammer size={10} />
                  {t('GlassEnclosure.WorkOrder.DefectSubmit')}
                </button>
              </div>
            )}
          </li>
        );
      })}
    </ul>
  );
}
