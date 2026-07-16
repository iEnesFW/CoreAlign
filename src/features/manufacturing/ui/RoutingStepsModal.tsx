import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { ListOrdered, Plus, Trash2 } from 'lucide-react';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { fieldBaseClasses } from '@/shared/lib/fieldClasses';
import { toastApiError } from '@/shared/lib/mutationToast';
import {
  useRoutingQuery,
  useSetRoutingSteps,
  useWorkCentersQuery,
} from '../hooks/useManufacturingQueries';
import type { RoutingOperationType } from '../model/manufacturing.types';

interface Props {
  routingId: string;
  routingCode: string;
  onClose: () => void;
}

interface StepRow {
  key: string;
  workCenterId: string;
  operationName: string;
  operationType: RoutingOperationType;
  setupTimeMinutes: string;
  runTimeMinutesPerUnit: string;
  runTimeMinutesPerSqm: string;
  scrapPercentage: string;
  isOptional: boolean;
}

const OP_TYPES: RoutingOperationType[] = [
  'Cutting',
  'Edging',
  'Tempering',
  'Lamination',
  'Drilling',
  'Sandblasting',
  'Washing',
  'QualityControl',
  'Packaging',
  'Other',
];

const newRow = (): StepRow => ({
  key: crypto.randomUUID(),
  workCenterId: '',
  operationName: '',
  operationType: 'Cutting',
  setupTimeMinutes: '0',
  runTimeMinutesPerUnit: '0',
  runTimeMinutesPerSqm: '',
  scrapPercentage: '0',
  isOptional: false,
});

export const RoutingStepsModal = ({ routingId, routingCode, onClose }: Props) => {
  const { t } = useTranslation();
  const routingQuery = useRoutingQuery(routingId);
  const workCentersQuery = useWorkCentersQuery(false);
  const setStepsMutation = useSetRoutingSteps();

  const workCenters = workCentersQuery.data ?? [];

  const [rows, setRows] = useState<StepRow[]>([]);
  const [syncKey, setSyncKey] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const currentKey = `${routingId}:${routingQuery.dataUpdatedAt}`;
  if (routingQuery.isSuccess && currentKey !== syncKey) {
    setSyncKey(currentKey);
    const loaded = routingQuery.data?.steps ?? [];
    setRows(
      loaded.length === 0
        ? [newRow()]
        : loaded.map((s) => ({
            key: crypto.randomUUID(),
            workCenterId: s.workCenterId,
            operationName: s.operationName,
            operationType: s.operationType,
            setupTimeMinutes: String(s.setupTimeMinutes),
            runTimeMinutesPerUnit: String(s.runTimeMinutesPerUnit),
            runTimeMinutesPerSqm:
              s.runTimeMinutesPerSqm === null ? '' : String(s.runTimeMinutesPerSqm),
            scrapPercentage: String(s.scrapPercentage),
            isOptional: s.isOptional,
          })),
    );
  }

  const updateRow = (key: string, patch: Partial<StepRow>) =>
    setRows((prev) => prev.map((r) => (r.key === key ? { ...r, ...patch } : r)));
  const addRow = () => setRows((prev) => [...prev, newRow()]);
  const removeRow = (key: string) =>
    setRows((prev) => (prev.length === 1 ? prev : prev.filter((r) => r.key !== key)));

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    const invalid = rows.some((r) => !r.workCenterId || !r.operationName.trim());
    if (invalid) {
      toast.error(t('Manufacturing.stepsForm.rowsIncomplete'));
      return;
    }

    setSubmitting(true);
    const result = await setStepsMutation
      .mutateAsync({
        routingId,
        steps: rows.map((r, i) => ({
          stepNumber: i + 1,
          workCenterId: r.workCenterId,
          operationName: r.operationName.trim(),
          operationType: r.operationType,
          setupTimeMinutes: Number(r.setupTimeMinutes) || 0,
          runTimeMinutesPerUnit: Number(r.runTimeMinutesPerUnit) || 0,
          runTimeMinutesPerSqm: r.runTimeMinutesPerSqm.trim()
            ? Number(r.runTimeMinutesPerSqm)
            : null,
          scrapPercentage: Number(r.scrapPercentage) || 0,
          instructions: null,
          isOptional: r.isOptional,
        })),
      })
      .catch((err) => {
        toastApiError(err);
        return null;
      });
    setSubmitting(false);

    if (result?.isSuccess) {
      toast.success(t('Manufacturing.stepsForm.saved'));
      onClose();
    } else if (result && !result.isSuccess) {
      toast.error(result.errors?.[0] ?? t('Manufacturing.stepsForm.failed'));
    }
  };

  return (
    <Modal
      open={true}
      title={t('Manufacturing.stepsForm.title', { code: routingCode })}
      icon={<ListOrdered size={18} />}
      onClose={onClose}
      size="2xl"
      footer={
        <>
          <Button variant="ghost" type="button" onClick={onClose}>
            {t('Manufacturing.actions.cancel')}
          </Button>
          <Button type="submit" form="routing-steps-form" isLoading={submitting}>
            {t('Manufacturing.actions.save')}
          </Button>
        </>
      }
    >
      <form id="routing-steps-form" onSubmit={submit} className="space-y-3">
        <div className="overflow-x-auto rounded-lg border border-slate-200 dark:border-slate-800">
          <table className="w-full text-sm">
            <thead className="bg-slate-50 text-[10px] font-semibold uppercase text-slate-600 dark:bg-slate-800/50 dark:text-slate-300">
              <tr>
                <th className="w-8 px-2 py-1.5 text-center">#</th>
                <th className="px-2 py-1.5 text-left">{t('Manufacturing.step.workCenter')}</th>
                <th className="px-2 py-1.5 text-left">{t('Manufacturing.step.operationName')}</th>
                <th className="px-2 py-1.5 text-left">{t('Manufacturing.step.operationType')}</th>
                <th className="w-20 px-2 py-1.5 text-right">{t('Manufacturing.step.setup')}</th>
                <th className="w-20 px-2 py-1.5 text-right">{t('Manufacturing.step.runUnit')}</th>
                <th className="w-20 px-2 py-1.5 text-right">{t('Manufacturing.step.runSqm')}</th>
                <th className="w-16 px-2 py-1.5 text-right">{t('Manufacturing.step.scrap')}</th>
                <th className="w-8 px-2 py-1.5"></th>
              </tr>
            </thead>
            <tbody>
              {rows.map((r, i) => (
                <tr key={r.key} className="border-t border-slate-100 dark:border-slate-800">
                  <td className="px-2 py-1.5 text-center text-slate-500">{i + 1}</td>
                  <td className="px-2 py-1.5">
                    <select
                      value={r.workCenterId}
                      onChange={(e) => updateRow(r.key, { workCenterId: e.target.value })}
                      className={fieldBaseClasses(false)}
                      aria-label={t('Manufacturing.step.workCenter')}
                    >
                      <option value="">—</option>
                      {workCenters.map((w) => (
                        <option key={w.id} value={w.id}>
                          {w.code}
                        </option>
                      ))}
                    </select>
                  </td>
                  <td className="px-2 py-1.5">
                    <input
                      type="text"
                      value={r.operationName}
                      onChange={(e) => updateRow(r.key, { operationName: e.target.value })}
                      maxLength={100}
                      className={fieldBaseClasses(false)}
                      aria-label={t('Manufacturing.step.operationName')}
                    />
                  </td>
                  <td className="px-2 py-1.5">
                    <select
                      value={r.operationType}
                      onChange={(e) =>
                        updateRow(r.key, { operationType: e.target.value as RoutingOperationType })
                      }
                      className={fieldBaseClasses(false)}
                      aria-label={t('Manufacturing.step.operationType')}
                    >
                      {OP_TYPES.map((op) => (
                        <option key={op} value={op}>
                          {t(`Manufacturing.operationType.${op}`)}
                        </option>
                      ))}
                    </select>
                  </td>
                  <td className="px-2 py-1.5">
                    <input
                      type="number"
                      min={0}
                      step="any"
                      value={r.setupTimeMinutes}
                      onChange={(e) => updateRow(r.key, { setupTimeMinutes: e.target.value })}
                      className={`${fieldBaseClasses(false)} text-right`}
                      aria-label={t('Manufacturing.step.setup')}
                    />
                  </td>
                  <td className="px-2 py-1.5">
                    <input
                      type="number"
                      min={0}
                      step="any"
                      value={r.runTimeMinutesPerUnit}
                      onChange={(e) => updateRow(r.key, { runTimeMinutesPerUnit: e.target.value })}
                      className={`${fieldBaseClasses(false)} text-right`}
                      aria-label={t('Manufacturing.step.runUnit')}
                    />
                  </td>
                  <td className="px-2 py-1.5">
                    <input
                      type="number"
                      min={0}
                      step="any"
                      value={r.runTimeMinutesPerSqm}
                      onChange={(e) => updateRow(r.key, { runTimeMinutesPerSqm: e.target.value })}
                      className={`${fieldBaseClasses(false)} text-right`}
                      aria-label={t('Manufacturing.step.runSqm')}
                    />
                  </td>
                  <td className="px-2 py-1.5">
                    <input
                      type="number"
                      min={0}
                      max={100}
                      step="any"
                      value={r.scrapPercentage}
                      onChange={(e) => updateRow(r.key, { scrapPercentage: e.target.value })}
                      className={`${fieldBaseClasses(false)} text-right`}
                      aria-label={t('Manufacturing.step.scrap')}
                    />
                  </td>
                  <td className="px-2 py-1.5 text-center">
                    <button
                      type="button"
                      onClick={() => removeRow(r.key)}
                      disabled={rows.length === 1}
                      className="rounded p-1 text-slate-400 hover:bg-danger-50 hover:text-danger-700 disabled:opacity-30 dark:hover:bg-danger-500/10"
                      aria-label={t('Manufacturing.actions.remove')}
                    >
                      <Trash2 size={13} />
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        <button
          type="button"
          onClick={addRow}
          className="inline-flex items-center gap-1.5 rounded border border-slate-200 bg-white px-2.5 py-1.5 text-xs font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
        >
          <Plus size={12} />
          {t('Manufacturing.stepsForm.addStep')}
        </button>
      </form>
    </Modal>
  );
};
