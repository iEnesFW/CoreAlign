import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Factory, FileText, Plus, XCircle } from 'lucide-react';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { Card } from '@/shared/ui/Card/Card';
import { Badge } from '@/shared/ui/Badge/Badge';
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

const statusTone: Record<ProductionJobStatus, 'success' | 'neutral' | 'warning' | 'danger'> = {
  Draft: 'warning',
  Released: 'neutral',
  InProgress: 'neutral',
  OnHold: 'warning',
  ReadyToComplete: 'success',
  Completed: 'success',
  Cancelled: 'danger',
};

export const ProductionJobsPage = () => {
  const { t } = useTranslation();
  const [statusFilter, setStatusFilter] = useState<ProductionJobStatus | 'all'>('all');
  const [isCreateModalOpen, setCreateModalOpen] = useState(false);
  const [selectedJobId, setSelectedJobId] = useState<string | null>(null);

  const {
    data: jobs,
    isLoading,
    error,
    refetch,
  } = useJobsQuery(statusFilter === 'all' ? undefined : statusFilter);

  const { mutateAsync: cancelJob, isPending: isCancelling } = useCancelJob();
  const confirm = useConfirm();

  const handleCancel = async (job: ProductionJobListSummary) => {
    if (
      await confirm({
        title: t('ProductionJobs.cancel_title'),
        message: t('ProductionJobs.cancel_message', { number: job.jobNumber }),
        confirmLabel: t('Common.actions.cancel_it'),
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

  const filters: Array<{ id: ProductionJobStatus | 'all'; label: string }> = [
    { id: 'all', label: t('ProductionJobs.status.all') },
    { id: 'Draft', label: t('ProductionJobs.status.Draft') },
    { id: 'Released', label: t('ProductionJobs.status.Released') },
    { id: 'InProgress', label: t('ProductionJobs.status.InProgress') },
    { id: 'ReadyToComplete', label: t('ProductionJobs.status.ReadyToComplete') },
    { id: 'Completed', label: t('ProductionJobs.status.Completed') },
  ];

  return (
    <div className="space-y-6">
      <PageHeader
        title={t('ProductionJobs.title')}
        subtitle={t('ProductionJobs.subtitle')}
        actions={
          <Button onClick={() => setCreateModalOpen(true)}>
            <Plus className="h-4 w-4" />
            {t('ProductionJobs.actions.new_job')}
          </Button>
        }
      />

      <div className="flex flex-wrap gap-2">
        {filters.map((f) => (
          <button
            key={f.id}
            onClick={() => setStatusFilter(f.id)}
            className={`rounded-full px-4 py-1.5 text-sm font-medium transition ${
              statusFilter === f.id
                ? 'bg-primary-600 text-white'
                : 'bg-slate-100 text-slate-600 hover:bg-slate-200 dark:bg-slate-800 dark:text-slate-300 dark:hover:bg-slate-700'
            }`}
          >
            {f.label}
          </button>
        ))}
      </div>

      {error ? (
        <QueryError onRetry={() => refetch()} />
      ) : isLoading ? (
        <Card className="p-8">
          <div className="flex justify-center">
            <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary-500 border-t-transparent" />
          </div>
        </Card>
      ) : jobs?.length === 0 ? (
        <EmptyState
          icon={<Factory size={24} />}
          title={t('ProductionJobs.empty_title')}
          description={t('ProductionJobs.empty_desc')}
          action={
            <Button onClick={() => setCreateModalOpen(true)}>
              {t('ProductionJobs.actions.new_job')}
            </Button>
          }
        />
      ) : (
        <div className="grid gap-4 lg:grid-cols-2 xl:grid-cols-3">
          {jobs.map((job) => (
            <Card
              key={job.id}
              className="flex flex-col p-5 hover:border-primary-300 transition-colors cursor-pointer"
              onClick={() => setSelectedJobId(job.id)}
            >
              <div className="mb-4 flex items-start justify-between">
                <div>
                  <h3 className="font-semibold text-slate-900 dark:text-white">{job.jobNumber}</h3>
                  <p className="text-sm text-slate-500 dark:text-slate-400">{job.productName}</p>
                </div>
                <Badge variant={statusTone[job.status]}>
                  {t(`ProductionJobs.status.${job.status}`)}
                </Badge>
              </div>

              <div className="mb-4 grid grid-cols-2 gap-4 text-sm">
                <div>
                  <span className="block text-slate-500 dark:text-slate-400">
                    {t('ProductionJobs.fields.qty')}
                  </span>
                  <span className="font-medium text-slate-900 dark:text-white">
                    {job.plannedQuantity} {job.unitOfMeasure}
                  </span>
                </div>
                <div>
                  <span className="block text-slate-500 dark:text-slate-400">
                    {t('ProductionJobs.fields.completed')}
                  </span>
                  <span className="font-medium text-slate-900 dark:text-white">
                    {job.completedQuantity} {job.unitOfMeasure}
                  </span>
                </div>
                {job.dueDateUtc && (
                  <div className="col-span-2">
                    <span className="block text-slate-500 dark:text-slate-400">
                      {t('ProductionJobs.fields.dueDate')}
                    </span>
                    <span className="font-medium text-slate-900 dark:text-white">
                      {new Date(job.dueDateUtc).toLocaleString()}
                    </span>
                  </div>
                )}
                {job.stepCount > 0 && (
                  <div className="col-span-2">
                    <span className="block text-slate-500 dark:text-slate-400 mb-1">
                      {t('ProductionJobs.fields.progress')} ({job.currentStepNumber ?? 0}/
                      {job.stepCount})
                    </span>
                    <div className="w-full bg-slate-200 rounded-full h-2 dark:bg-slate-700">
                      <div
                        className="bg-primary-600 h-2 rounded-full transition-all"
                        style={{
                          width: `${Math.min(100, Math.max(0, ((job.currentStepNumber ?? 0) / job.stepCount) * 100))}%`,
                        }}
                      ></div>
                    </div>
                  </div>
                )}
              </div>

              <div className="mt-auto flex justify-end gap-2 border-t border-slate-100 pt-4 dark:border-slate-800">
                {job.status === 'Draft' ||
                job.status === 'Released' ||
                job.status === 'InProgress' ? (
                  <Button
                    variant="ghost"
                    size="sm"
                    className="text-red-600 hover:bg-red-50 hover:text-red-700 dark:text-red-400 dark:hover:bg-red-950"
                    onClick={(e) => {
                      e.stopPropagation();
                      handleCancel(job);
                    }}
                    disabled={isCancelling}
                  >
                    <XCircle className="h-4 w-4" />
                  </Button>
                ) : null}
                <Button variant="secondary" size="sm" onClick={() => setSelectedJobId(job.id)}>
                  <FileText className="h-4 w-4" />
                  {t('Common.actions.details')}
                </Button>
              </div>
            </Card>
          ))}
        </div>
      )}

      {isCreateModalOpen && <JobFormModal onClose={() => setCreateModalOpen(false)} />}

      {selectedJobId && (
        <JobDetailPanel jobId={selectedJobId} onClose={() => setSelectedJobId(null)} />
      )}
    </div>
  );
};
