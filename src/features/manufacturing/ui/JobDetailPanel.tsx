import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { CheckCircle2, ChevronRight, Play, X, SkipForward } from 'lucide-react';
import { Button } from '@/shared/ui/Button/Button';
import { Badge } from '@/shared/ui/Badge/Badge';
import { Input } from '@/shared/ui/Input/Input';
import { toastApiError } from '@/shared/lib/mutationToast';
import { toast } from 'sonner';
import {
  useJobQuery,
  useReleaseJob,
  useStartJobStep,
  useFinishJobStep,
  useSkipJobStep,
  useCompleteJob,
  useOperatorsQuery,
} from '@/features/manufacturing/hooks/useManufacturingQueries';
import { useWarehousesQuery } from '@/shared/master-data/hooks/useMasterData';
import { Select } from '@/shared/ui/Select/Select';

interface Props {
  jobId: string;
  onClose: () => void;
}

export const JobDetailPanel = ({ jobId, onClose }: Props) => {
  const { t } = useTranslation();
  const { data: job, isLoading } = useJobQuery(jobId);
  const { data: warehouses } = useWarehousesQuery(true);

  const { mutateAsync: releaseJob } = useReleaseJob();
  const { mutateAsync: completeJob } = useCompleteJob();

  const [warehouseId, setWarehouseId] = useState('');

  const handleRelease = async () => {
    if (!warehouseId) {
      toast.error(t('ProductionJobs.fields.warehouse_required'));
      return;
    }
    try {
      await releaseJob({ id: jobId, input: { warehouseId } });
      toast.success(t('ProductionJobs.release_success'));
    } catch (e) {
      toastApiError(e, t('ProductionJobs.release_error'));
    }
  };

  const handleComplete = async () => {
    if (!warehouseId && !job?.warehouseId) {
      toast.error(t('ProductionJobs.fields.warehouse_required'));
      return;
    }
    try {
      await completeJob({
        id: jobId,
        input: {
          completedQuantity: job?.plannedQuantity ?? 0,
          warehouseId: warehouseId || job?.warehouseId,
        },
      });
      toast.success(t('ProductionJobs.complete_success'));
      onClose();
    } catch (e) {
      toastApiError(e, t('ProductionJobs.complete_error'));
    }
  };

  if (isLoading || !job) {
    return (
      <div className="fixed inset-y-0 right-0 z-50 w-full max-w-2xl bg-white shadow-2xl dark:bg-slate-900 border-l border-slate-100 dark:border-slate-800 flex items-center justify-center">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary-500 border-t-transparent" />
      </div>
    );
  }

  return (
    <div className="fixed inset-y-0 right-0 z-50 flex w-full max-w-3xl flex-col bg-white shadow-2xl dark:bg-slate-900 border-l border-slate-100 dark:border-slate-800">
      <div className="flex items-center justify-between border-b border-slate-100 px-6 py-4 dark:border-slate-800">
        <div>
          <h2 className="text-lg font-semibold text-slate-900 dark:text-white flex items-center gap-2">
            {job.jobNumber}
            <Badge variant="neutral">{t(`ProductionJobs.status.${job.status}`)}</Badge>
          </h2>
          <p className="text-sm text-slate-500">
            {job.productName} — {job.plannedQuantity} {job.unitOfMeasure}
          </p>
        </div>
        <button
          onClick={onClose}
          className="rounded-lg p-2 hover:bg-slate-100 dark:hover:bg-slate-800"
        >
          <X className="h-5 w-5" />
        </button>
      </div>

      <div className="flex-1 overflow-y-auto p-6 space-y-8 bg-slate-50 dark:bg-slate-950/50">
        {/* Actions Bar */}
        {job.status === 'Draft' && (
          <div className="bg-white p-4 rounded-xl border border-slate-200 dark:bg-slate-900 dark:border-slate-800 flex gap-4 items-end shadow-sm">
            <div className="flex-1">
              <Select
                label={t('ProductionJobs.fields.warehouse')}
                value={warehouseId}
                onChange={(e) => setWarehouseId(e.target.value)}
              >
                <option value="">{t('Common.actions.select')}</option>
                {warehouses?.data?.map((w: { id: string; name: string }) => (
                  <option key={w.id} value={w.id}>
                    {w.name}
                  </option>
                ))}
              </Select>
            </div>
            <Button onClick={handleRelease} className="mb-1">
              <Play className="h-4 w-4" /> {t('ProductionJobs.actions.release')}
            </Button>
          </div>
        )}

        {job.status === 'ReadyToComplete' && (
          <div className="bg-white p-4 rounded-xl border border-slate-200 dark:bg-slate-900 dark:border-slate-800 flex justify-between items-center shadow-sm">
            <div>
              <h3 className="font-medium">{t('ProductionJobs.ready_to_complete_title')}</h3>
              <p className="text-sm text-slate-500">{t('ProductionJobs.ready_to_complete_desc')}</p>
            </div>
            <Button onClick={handleComplete} tone="success">
              <CheckCircle2 className="h-4 w-4" /> {t('ProductionJobs.actions.complete_job')}
            </Button>
          </div>
        )}

        {/* Steps */}
        <div className="space-y-4">
          <h3 className="font-semibold text-slate-900 dark:text-white px-1 flex items-center gap-2">
            <ChevronRight className="h-5 w-5 text-primary-500" />
            {t('ProductionJobs.traveler_steps')}
          </h3>

          <div className="space-y-3 relative before:absolute before:inset-0 before:ml-5 before:-translate-x-px md:before:mx-auto md:before:translate-x-0 before:h-full before:w-0.5 before:bg-gradient-to-b before:from-transparent before:via-slate-200 before:to-transparent dark:before:via-slate-800">
            {job.steps.map((step) => (
              <StepCard key={step.id} job={job} step={step} />
            ))}
            {job.steps.length === 0 && (
              <div className="text-center py-8 text-slate-500">{t('ProductionJobs.no_steps')}</div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};

const StepCard = ({ job, step }: { job: Record<string, any>; step: Record<string, any> }) => {
  const { t } = useTranslation();
  const [operatorId, setOperatorId] = useState('');
  const [goodQty, setGoodQty] = useState(step.inputQuantity || job.plannedQuantity);
  const [scrappedQty, setScrappedQty] = useState(0);

  const { data: operators } = useOperatorsQuery(step.workCenterId || undefined);
  const { mutateAsync: startStep } = useStartJobStep();
  const { mutateAsync: finishStep } = useFinishJobStep();
  const { mutateAsync: skipStep } = useSkipJobStep();

  const handleStart = async () => {
    if (!operatorId) {
      toast.error(t('ProductionJobs.operator_required'));
      return;
    }
    try {
      await startStep({ id: job.id, stepNumber: step.stepNumber, input: { operatorId } });
      toast.success(t('ProductionJobs.step_started'));
    } catch (e) {
      toastApiError(e);
    }
  };

  const handleFinish = async () => {
    if (!operatorId) {
      toast.error(t('ProductionJobs.operator_required'));
      return;
    }
    try {
      await finishStep({
        id: job.id,
        stepNumber: step.stepNumber,
        input: { operatorId, goodQuantity: goodQty, scrappedQuantity: scrappedQty },
      });
      toast.success(t('ProductionJobs.step_finished'));
    } catch (e) {
      toastApiError(e);
    }
  };

  const handleSkip = async () => {
    try {
      await skipStep({ id: job.id, stepNumber: step.stepNumber });
      toast.success(t('ProductionJobs.step_skipped'));
    } catch (e) {
      toastApiError(e);
    }
  };

  const isActive =
    job.currentStepNumber === step.stepNumber &&
    (job.status === 'Released' || job.status === 'InProgress');
  const isCompleted = step.status === 'Completed';
  const isSkipped = step.status === 'Skipped';

  return (
    <div
      className={`relative flex items-center justify-between md:justify-normal md:odd:flex-row-reverse group is-active`}
    >
      {/* Timeline dot */}
      <div
        className={`flex items-center justify-center w-10 h-10 rounded-full border-4 border-white dark:border-slate-900 bg-white dark:bg-slate-900 shadow shrink-0 md:order-1 md:group-odd:-translate-x-1/2 md:group-even:translate-x-1/2 z-10 ${isActive ? 'ring-2 ring-primary-500' : ''}`}
      >
        {isCompleted ? (
          <CheckCircle2 className="w-5 h-5 text-emerald-500" />
        ) : isSkipped ? (
          <SkipForward className="w-5 h-5 text-slate-400" />
        ) : (
          <div
            className={`w-3 h-3 rounded-full ${isActive ? 'bg-primary-500 animate-pulse' : 'bg-slate-300 dark:bg-slate-600'}`}
          />
        )}
      </div>

      {/* Card */}
      <div className="w-[calc(100%-4rem)] md:w-[calc(50%-2.5rem)] p-4 rounded-xl border border-slate-200 bg-white shadow-sm dark:border-slate-800 dark:bg-slate-900">
        <div className="flex justify-between items-start mb-2">
          <div>
            <div className="text-xs font-semibold text-primary-600 dark:text-primary-400 tracking-wide uppercase">
              {t('ProductionJobs.step')} {step.stepNumber}
            </div>
            <h4 className="font-medium text-slate-900 dark:text-white mt-1">
              {step.operationName}
            </h4>
            <p className="text-sm text-slate-500">{step.workCenterName}</p>
          </div>
          <Badge variant={isCompleted ? 'success' : 'neutral'}>
            {t(`ProductionJobs.stepStatus.${step.status}`)}
          </Badge>
        </div>

        {step.instructions && (
          <div className="text-sm bg-slate-50 dark:bg-slate-800 p-2 rounded mb-3 text-slate-600 dark:text-slate-300">
            {step.instructions}
          </div>
        )}

        {isActive && step.status === 'Pending' && (
          <div className="mt-4 pt-4 border-t border-slate-100 dark:border-slate-800 space-y-3">
            <Select value={operatorId} onChange={(e) => setOperatorId(e.target.value)} size="sm">
              <option value="">{t('ProductionJobs.select_operator')}</option>
              {operators?.map((o) => (
                <option key={o.id} value={o.employeeId}>
                  {o.employeeName}
                </option>
              ))}
            </Select>
            <div className="flex gap-2">
              <Button size="sm" variant="primary" onClick={handleStart} className="flex-1">
                <Play className="w-4 h-4" /> {t('ProductionJobs.actions.start')}
              </Button>
              {step.isOptional && (
                <Button size="sm" variant="ghost" onClick={handleSkip}>
                  <SkipForward className="w-4 h-4" />
                </Button>
              )}
            </div>
          </div>
        )}

        {isActive && step.status === 'InProgress' && (
          <div className="mt-4 pt-4 border-t border-slate-100 dark:border-slate-800 space-y-3">
            <Select value={operatorId} onChange={(e) => setOperatorId(e.target.value)} size="sm">
              <option value="">{t('ProductionJobs.select_operator')}</option>
              {operators?.map((o) => (
                <option key={o.id} value={o.employeeId}>
                  {o.employeeName}
                </option>
              ))}
            </Select>
            <div className="grid grid-cols-2 gap-2">
              <Input
                type="number"
                label={t('ProductionJobs.fields.good')}
                value={goodQty}
                onChange={(e) => setGoodQty(Number(e.target.value))}
                size="sm"
              />
              <Input
                type="number"
                label={t('ProductionJobs.fields.scrapped')}
                value={scrappedQty}
                onChange={(e) => setScrappedQty(Number(e.target.value))}
                size="sm"
              />
            </div>
            <Button size="sm" onClick={handleFinish} tone="success" className="w-full">
              <CheckCircle2 className="w-4 h-4" /> {t('ProductionJobs.actions.finish')}
            </Button>
          </div>
        )}

        {isCompleted && (
          <div className="mt-3 text-sm text-slate-500 grid grid-cols-2 gap-2">
            <div>
              {t('ProductionJobs.fields.good')}:{' '}
              <span className="font-medium text-emerald-600">{step.goodQuantity}</span>
            </div>
            {step.scrappedQuantity > 0 && (
              <div>
                {t('ProductionJobs.fields.scrapped')}:{' '}
                <span className="font-medium text-red-600">{step.scrappedQuantity}</span>
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
};
