import { useState, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { Factory, Plus, XCircle, LayoutGrid, List } from 'lucide-react';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { Button } from '@/shared/ui/Button/Button';
import { EmptyState } from '@/shared/ui/EmptyState/EmptyState';
import { QueryError } from '@/shared/ui/QueryError/QueryError';
import { useConfirm } from '@/shared/ui/ConfirmDialog/useConfirm';
import { toastApiError } from '@/shared/lib/mutationToast';
import { toast } from 'sonner';
import { useCancelJob, useJobsQuery } from '@/features/manufacturing/hooks/useManufacturingQueries';
import type {
  ProductionJobListSummary,
  ProductionJobStatus,
} from '@/features/manufacturing/model/productionJob.types';
import { JobFormModal } from '@/features/manufacturing/ui/JobFormModal';
import { JobDetailPanel } from '@/features/manufacturing/ui/JobDetailPanel';

const KANBAN_COLUMNS: ProductionJobStatus[] = [
  'Draft',
  'Released',
  'InProgress',
  'OnHold',
  'ReadyToComplete',
  'Completed',
];

const statusColors: Record<ProductionJobStatus, string> = {
  Draft: 'bg-slate-200 dark:bg-slate-700',
  Released: 'bg-blue-100 dark:bg-blue-900/40',
  InProgress: 'bg-indigo-100 dark:bg-indigo-900/40',
  OnHold: 'bg-yellow-100 dark:bg-yellow-900/40',
  ReadyToComplete: 'bg-emerald-100 dark:bg-emerald-900/40',
  Completed: 'bg-green-100 dark:bg-green-900/40',
  Cancelled: 'bg-red-100 dark:bg-red-900/40',
};

const statusBorderColors: Record<ProductionJobStatus, string> = {
  Draft: 'border-slate-300 dark:border-slate-600',
  Released: 'border-blue-300 dark:border-blue-700',
  InProgress: 'border-indigo-300 dark:border-indigo-700',
  OnHold: 'border-yellow-300 dark:border-yellow-700',
  ReadyToComplete: 'border-emerald-300 dark:border-emerald-700',
  Completed: 'border-green-300 dark:border-green-700',
  Cancelled: 'border-red-300 dark:border-red-700',
};

export const ProductionJobsPage = () => {
  const { t } = useTranslation();
  const [viewMode, setViewMode] = useState<'kanban' | 'list'>('kanban');
  const [isCreateModalOpen, setCreateModalOpen] = useState(false);
  const [selectedJobId, setSelectedJobId] = useState<string | null>(null);

  const { data: jobs, isLoading, error, refetch } = useJobsQuery();
  const { mutateAsync: cancelJob, isPending: isCancelling } = useCancelJob();
  const confirm = useConfirm();

  const handleCancel = async (job: ProductionJobListSummary, e: React.MouseEvent) => {
    e.stopPropagation();
    if (
      await confirm({
        title: t('ProductionJobs.cancel_title'),
        message: t('ProductionJobs.cancel_message', { number: job.jobNumber }),
        confirmLabel: t('Common.Cancel'),
        tone: 'danger',
      })
    ) {
      try {
        await cancelJob({ id: job.id, input: { reason: 'User cancelled via UI' } });
        toast.success(t('ProductionJobs.cancel_success'));
      } catch (err) {
        toastApiError(err, t('ProductionJobs.cancel_error'));
      }
    }
  };

  const columns = useMemo(() => {
    if (!jobs) return {};
    const cols: Record<string, ProductionJobListSummary[]> = {};
    KANBAN_COLUMNS.forEach((col) => {
      cols[col] = jobs.filter((j) => j.status === col);
    });
    return cols;
  }, [jobs]);

  const renderJobCard = (job: ProductionJobListSummary) => (
    <div
      key={job.id}
      onClick={() => setSelectedJobId(job.id)}
      className="group relative bg-white dark:bg-slate-800 rounded-xl p-4 shadow-sm border border-slate-200 dark:border-slate-700 hover:shadow-md transition-all cursor-pointer hover:-translate-y-1"
    >
      <div className="absolute top-3 right-3 opacity-0 group-hover:opacity-100 transition-opacity flex gap-1">
        {(job.status === 'Draft' || job.status === 'Released' || job.status === 'InProgress') && (
          <button
            onClick={(e) => handleCancel(job, e)}
            disabled={isCancelling}
            className="p-1.5 text-slate-400 hover:text-red-500 hover:bg-red-50 dark:hover:bg-red-950/30 rounded-md transition-colors"
          >
            <XCircle size={16} />
          </button>
        )}
      </div>

      <div className="flex justify-between items-start mb-3 pr-8">
        <div>
          <h4 className="font-bold text-slate-900 dark:text-white text-sm">{job.jobNumber}</h4>
          <p className="text-xs text-slate-500 dark:text-slate-400 mt-0.5 line-clamp-1">
            {job.productName}
          </p>
        </div>
      </div>

      <div className="space-y-2 mt-3">
        <div className="flex justify-between text-xs">
          <span className="text-slate-500 dark:text-slate-400">
            {t('ProductionJobs.fields.qty', 'Qty:')}
          </span>
          <span className="font-medium text-slate-700 dark:text-slate-300">
            {job.plannedQuantity} {job.unitOfMeasure}
          </span>
        </div>

        {job.stepCount > 0 && (
          <div className="pt-2">
            <div className="flex justify-between text-xs mb-1">
              <span className="text-slate-500">
                {t('ProductionJobs.fields.progress', 'Progress')}
              </span>
              <span className="text-slate-700 dark:text-slate-300 font-medium">
                {job.currentStepNumber ?? 0}/{job.stepCount}
              </span>
            </div>
            <div className="w-full bg-slate-100 dark:bg-slate-700 rounded-full h-1.5">
              <div
                className="bg-indigo-500 h-1.5 rounded-full transition-all"
                style={{
                  width: `${Math.min(100, Math.max(0, ((job.currentStepNumber ?? 0) / job.stepCount) * 100))}%`,
                }}
              ></div>
            </div>
          </div>
        )}
      </div>
    </div>
  );

  return (
    <div className="flex flex-col h-[calc(100vh-6rem)]">
      <div className="shrink-0 mb-6">
        <PageHeader
          title={t('ProductionJobs.title')}
          subtitle={t('ProductionJobs.subtitle')}
          actions={
            <div className="flex items-center gap-3">
              <div className="flex items-center bg-slate-100 dark:bg-slate-800 p-1 rounded-lg border border-slate-200 dark:border-slate-700">
                <button
                  onClick={() => setViewMode('kanban')}
                  className={`p-1.5 rounded-md transition-all ${viewMode === 'kanban' ? 'bg-white dark:bg-slate-600 shadow-sm text-indigo-600 dark:text-indigo-400' : 'text-slate-500 hover:text-slate-700 dark:hover:text-slate-300'}`}
                >
                  <LayoutGrid size={18} />
                </button>
                <button
                  onClick={() => setViewMode('list')}
                  className={`p-1.5 rounded-md transition-all ${viewMode === 'list' ? 'bg-white dark:bg-slate-600 shadow-sm text-indigo-600 dark:text-indigo-400' : 'text-slate-500 hover:text-slate-700 dark:hover:text-slate-300'}`}
                >
                  <List size={18} />
                </button>
              </div>
              <Button
                onClick={() => setCreateModalOpen(true)}
                className="bg-indigo-600 hover:bg-indigo-700 text-white shadow-sm shadow-indigo-600/20"
              >
                <Plus className="h-4 w-4 mr-1" />
                {t('ProductionJobs.actions.new_job')}
              </Button>
            </div>
          }
        />
      </div>

      <div className="flex-1 overflow-hidden min-h-0">
        {error ? (
          <QueryError onRetry={() => refetch()} />
        ) : isLoading ? (
          <div className="flex h-full items-center justify-center">
            <div className="h-8 w-8 animate-spin rounded-full border-4 border-indigo-500 border-t-transparent" />
          </div>
        ) : jobs?.length === 0 ? (
          <EmptyState
            icon={<Factory size={32} className="text-indigo-400" />}
            title={t('ProductionJobs.empty_title')}
            description={t('ProductionJobs.empty_desc')}
            action={
              <Button
                onClick={() => setCreateModalOpen(true)}
                className="bg-indigo-600 hover:bg-indigo-700"
              >
                {t('ProductionJobs.actions.new_job')}
              </Button>
            }
          />
        ) : viewMode === 'kanban' ? (
          <div className="flex gap-6 h-full overflow-x-auto pb-4 px-1 snap-x">
            {KANBAN_COLUMNS.map((col) => (
              <div key={col} className="flex flex-col flex-none w-80 snap-start">
                <div
                  className={`flex items-center justify-between mb-3 px-3 py-2 rounded-lg border ${statusColors[col]} ${statusBorderColors[col]}`}
                >
                  <h3 className="font-semibold text-slate-800 dark:text-slate-200 text-sm">
                    {t(`ProductionJobs.status.${col}`)}
                  </h3>
                  <span className="bg-white/50 dark:bg-black/20 text-slate-600 dark:text-slate-300 text-xs font-bold px-2 py-0.5 rounded-full">
                    {columns[col]?.length || 0}
                  </span>
                </div>

                <div className="flex-1 overflow-y-auto space-y-3 pr-1 scrollbar-thin scrollbar-thumb-slate-200 dark:scrollbar-thumb-slate-700">
                  {columns[col]?.map(renderJobCard)}
                  {columns[col]?.length === 0 && (
                    <div className="h-[132px] rounded-xl border-2 border-dashed border-slate-200 dark:border-slate-700 flex items-center justify-center text-slate-400 dark:text-slate-500 text-sm">
                      {t('ProductionJobs.empty_title')}
                    </div>
                  )}
                </div>
              </div>
            ))}
          </div>
        ) : (
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 h-full overflow-y-auto pb-4">
            {jobs?.map(renderJobCard)}
          </div>
        )}
      </div>

      {isCreateModalOpen && <JobFormModal onClose={() => setCreateModalOpen(false)} />}

      {selectedJobId && (
        <JobDetailPanel jobId={selectedJobId} onClose={() => setSelectedJobId(null)} />
      )}
    </div>
  );
};
