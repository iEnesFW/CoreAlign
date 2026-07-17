import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Factory,
  ListOrdered,
  Pencil,
  Plus,
  Trash2,
  UserCog,
  Workflow,
  LayoutGrid,
  CheckCircle2,
  PlayCircle,
  Archive,
  AlertCircle,
} from 'lucide-react';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { Badge } from '@/shared/ui/Badge/Badge';
import { Button } from '@/shared/ui/Button/Button';
import { EmptyState } from '@/shared/ui/EmptyState/EmptyState';
import { QueryError } from '@/shared/ui/QueryError/QueryError';
import { useConfirm } from '@/shared/ui/ConfirmDialog/useConfirm';
import { toast } from 'sonner';
import { toastApiError } from '@/shared/lib/mutationToast';
import {
  useDeleteRouting,
  useOperatorsQuery,
  useRoutingsQuery,
  useRoutingTransition,
  useSetOperatorActive,
  useWorkCentersQuery,
} from '@/features/manufacturing/hooks/useManufacturingQueries';
import { RoutingFormModal } from '@/features/manufacturing/ui/RoutingFormModal';
import { RoutingStepsModal } from '@/features/manufacturing/ui/RoutingStepsModal';
import { WorkCenterFormModal } from '@/features/manufacturing/ui/WorkCenterFormModal';
import { OperatorFormModal } from '@/features/manufacturing/ui/OperatorFormModal';
import type {
  ProductionRoutingSummary,
  RoutingStatus,
  WorkCenter,
  WorkCenterOperator,
} from '@/features/manufacturing/model/manufacturing.types';

type Tab = 'routings' | 'workCenters' | 'operators';

const statusTone: Record<RoutingStatus, 'success' | 'neutral' | 'warning'> = {
  Draft: 'warning',
  Active: 'success',
  Archived: 'neutral',
};

export const ProductionRoutingsPage = () => {
  const { t } = useTranslation();
  const [tab, setTab] = useState<Tab>('routings');
  const [createModal, setCreateModal] = useState<Tab | null>(null);

  const tabs: { id: Tab; label: string; icon: typeof Workflow }[] = [
    { id: 'routings', label: t('Manufacturing.tabs.routings'), icon: Workflow },
    { id: 'workCenters', label: t('Manufacturing.tabs.workCenters'), icon: Factory },
    { id: 'operators', label: t('Manufacturing.tabs.operators'), icon: UserCog },
  ];

  return (
    <div className="flex flex-col flex-1 w-full space-y-8 pb-12">
      <div className="bg-gradient-to-r from-indigo-500/10 via-purple-500/10 to-transparent rounded-3xl p-6 border border-white/20 dark:border-white/5 backdrop-blur-sm">
        <PageHeader
          title={t('Manufacturing.title')}
          subtitle={t('Manufacturing.subtitle')}
          actions={
            <Button
              onClick={() => setCreateModal(tab)}
              className="bg-indigo-600 hover:bg-indigo-700 text-white shadow-lg shadow-indigo-500/30 rounded-xl px-6"
            >
              <Plus className="h-5 w-5 mr-2" />
              {t(`Manufacturing.actions.new_${tab}`)}
            </Button>
          }
        />
      </div>

      <div className="flex justify-center">
        <div className="inline-flex bg-slate-100/80 dark:bg-slate-800/80 backdrop-blur-md p-1.5 rounded-2xl shadow-sm border border-slate-200/50 dark:border-slate-700/50">
          {tabs.map((item) => {
            const Icon = item.icon;
            const active = tab === item.id;
            return (
              <button
                key={item.id}
                type="button"
                onClick={() => setTab(item.id)}
                className={`flex items-center gap-2 rounded-xl px-6 py-2.5 text-sm font-semibold transition-all duration-300 ${
                  active
                    ? 'bg-white dark:bg-slate-700 text-indigo-600 dark:text-indigo-400 shadow-sm scale-100'
                    : 'text-slate-500 dark:text-slate-400 hover:text-slate-700 dark:hover:text-slate-200 hover:bg-slate-200/50 dark:hover:bg-slate-700/50 scale-95 hover:scale-100'
                }`}
              >
                <Icon className={`h-4 w-4 ${active ? 'animate-pulse' : ''}`} />
                {item.label}
              </button>
            );
          })}
        </div>
      </div>

      <div className="animate-in fade-in slide-in-from-bottom-4 duration-500">
        {tab === 'routings' && <RoutingsTab />}
        {tab === 'workCenters' && <WorkCentersTab />}
        {tab === 'operators' && <OperatorsTab />}
      </div>

      {createModal === 'routings' && <RoutingFormModal onClose={() => setCreateModal(null)} />}
      {createModal === 'workCenters' && (
        <WorkCenterFormModal onClose={() => setCreateModal(null)} />
      )}
      {createModal === 'operators' && <OperatorFormModal onClose={() => setCreateModal(null)} />}
    </div>
  );
};

const RoutingsTab = () => {
  const { t } = useTranslation();
  const confirm = useConfirm();
  const query = useRoutingsQuery();
  const transition = useRoutingTransition();
  const deleteRouting = useDeleteRouting();
  const rows = query.data ?? [];
  const [editRouting, setEditRouting] = useState<ProductionRoutingSummary | null>(null);
  const [stepsRouting, setStepsRouting] = useState<ProductionRoutingSummary | null>(null);

  if (query.isError) return <QueryError onRetry={() => query.refetch()} />;

  const runTransition = async (id: string, action: 'activate' | 'archive' | 'restore') => {
    const res = await transition.mutateAsync({ id, action }).catch((err) => {
      toastApiError(err);
      return null;
    });
    if (res?.isSuccess) toast.success(t('Manufacturing.routings.transitioned'));
    else if (res && !res.isSuccess)
      toast.error(res.errors?.[0] ?? t('Manufacturing.routings.actionFailed'));
  };

  const remove = async (r: ProductionRoutingSummary) => {
    const ok = await confirm({
      title: t('Manufacturing.routings.deleteTitle'),
      message: t('Manufacturing.routings.deleteMessage', { code: r.code }),
      tone: 'danger',
    });
    if (!ok) return;
    const res = await deleteRouting.mutateAsync(r.id).catch((err) => {
      toastApiError(err);
      return null;
    });
    if (res?.isSuccess) toast.success(t('Manufacturing.routings.deleted'));
    else if (res && !res.isSuccess)
      toast.error(res.errors?.[0] ?? t('Manufacturing.routings.actionFailed'));
  };

  if (rows.length === 0) {
    return (
      <div className="bg-white/50 dark:bg-slate-900/50 backdrop-blur-lg rounded-3xl p-12 border-2 border-dashed border-slate-200 dark:border-slate-800">
        <EmptyState title={t('Manufacturing.routings.empty')} />
      </div>
    );
  }

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
      {rows.map((r) => (
        <div
          key={r.id}
          className="group relative bg-white/70 dark:bg-slate-800/70 backdrop-blur-xl border border-white/40 dark:border-slate-700/50 shadow-sm hover:shadow-xl rounded-2xl p-5 transition-all duration-300 hover:-translate-y-1 flex flex-col"
        >
          <div className="absolute top-4 right-4">
            <Badge variant={statusTone[r.status]} className="shadow-sm">
              {t(`Manufacturing.routingStatus.${r.status}`)}
            </Badge>
          </div>

          <div className="mb-6 pr-20">
            <div className="inline-flex items-center gap-1.5 text-xs font-bold text-indigo-500 dark:text-indigo-400 bg-indigo-50 dark:bg-indigo-500/10 px-2.5 py-1 rounded-md mb-3">
              <LayoutGrid size={12} />
              {r.code}
            </div>
            <h3 className="text-lg font-extrabold text-slate-800 dark:text-slate-100 leading-tight">
              {r.name}
            </h3>
          </div>

          <div className="flex-1"></div>

          <div className="bg-slate-50/80 dark:bg-slate-900/50 rounded-xl p-3 flex justify-between items-center mb-5 border border-slate-100/50 dark:border-slate-700/50">
            <div className="text-xs font-medium text-slate-500 dark:text-slate-400 flex items-center gap-1.5">
              <ListOrdered size={14} />
              {t('Manufacturing.routing.stepCount')}
            </div>
            <div className="font-bold text-slate-700 dark:text-slate-200 bg-white dark:bg-slate-800 px-2.5 py-0.5 rounded shadow-sm">
              {r.stepCount}
            </div>
          </div>

          <div className="flex flex-wrap items-center justify-end gap-2 border-t border-slate-100 dark:border-slate-700/50 pt-4">
            <IconBtn
              title={t('Manufacturing.actions.edit')}
              onClick={() => setEditRouting(r)}
              icon={<Pencil size={16} />}
            />
            {r.status === 'Draft' && (
              <IconBtn
                title={t('Manufacturing.actions.steps')}
                onClick={() => setStepsRouting(r)}
                icon={<ListOrdered size={16} />}
              />
            )}
            {r.status === 'Draft' && (
              <Button
                size="sm"
                variant="outline"
                className="rounded-lg hover:bg-green-50 hover:text-green-600 hover:border-green-200 dark:hover:bg-green-500/10 dark:hover:text-green-400 dark:hover:border-green-500/30"
                onClick={() => runTransition(r.id, 'activate')}
              >
                <PlayCircle className="w-4 h-4 mr-1.5" />
                {t('Manufacturing.actions.activate')}
              </Button>
            )}
            {r.status !== 'Archived' && (
              <Button
                size="sm"
                variant="ghost"
                className="rounded-lg text-slate-500 hover:text-slate-700 dark:text-slate-400 dark:hover:text-slate-300"
                onClick={() => runTransition(r.id, 'archive')}
              >
                <Archive className="w-4 h-4 mr-1.5" />
                {t('Manufacturing.actions.archive')}
              </Button>
            )}
            {r.status === 'Archived' && (
              <Button
                size="sm"
                variant="ghost"
                className="rounded-lg"
                onClick={() => runTransition(r.id, 'restore')}
              >
                {t('Manufacturing.actions.restore')}
              </Button>
            )}
            {r.status === 'Draft' && (
              <IconBtn
                title={t('Manufacturing.actions.delete')}
                onClick={() => remove(r)}
                icon={<Trash2 size={16} />}
                danger
              />
            )}
          </div>
        </div>
      ))}

      {editRouting && (
        <RoutingFormModal routing={editRouting} onClose={() => setEditRouting(null)} />
      )}
      {stepsRouting && (
        <RoutingStepsModal
          routingId={stepsRouting.id}
          routingCode={stepsRouting.code}
          onClose={() => setStepsRouting(null)}
        />
      )}
    </div>
  );
};

const WorkCentersTab = () => {
  const { t } = useTranslation();
  const query = useWorkCentersQuery(true);
  const rows = query.data ?? [];
  const [editWorkCenter, setEditWorkCenter] = useState<WorkCenter | null>(null);

  if (query.isError) return <QueryError onRetry={() => query.refetch()} />;

  if (rows.length === 0) {
    return (
      <div className="bg-white/50 dark:bg-slate-900/50 backdrop-blur-lg rounded-3xl p-12 border-2 border-dashed border-slate-200 dark:border-slate-800">
        <EmptyState title={t('Manufacturing.workCenters.empty')} />
      </div>
    );
  }

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
      {rows.map((w) => (
        <div
          key={w.id}
          className="group relative bg-white/70 dark:bg-slate-800/70 backdrop-blur-xl border border-white/40 dark:border-slate-700/50 shadow-sm hover:shadow-xl rounded-2xl p-5 transition-all duration-300 hover:-translate-y-1 flex flex-col"
        >
          <div className="absolute top-4 right-4 flex gap-2">
            <Badge variant={w.isActive ? 'success' : 'neutral'} className="shadow-sm">
              {w.isActive ? t('Manufacturing.common.yes') : t('Manufacturing.common.no')}
            </Badge>
          </div>

          <div className="mb-6 pr-20">
            <div className="inline-flex items-center gap-1.5 text-xs font-bold text-amber-600 dark:text-amber-400 bg-amber-50 dark:bg-amber-500/10 px-2.5 py-1 rounded-md mb-3">
              <Factory size={12} />
              {w.code}
            </div>
            <h3 className="text-lg font-extrabold text-slate-800 dark:text-slate-100 leading-tight">
              {w.name}
            </h3>
          </div>

          <div className="flex-1"></div>

          <div className="bg-slate-50/80 dark:bg-slate-900/50 rounded-xl p-3 flex justify-between items-center mb-5 border border-slate-100/50 dark:border-slate-700/50">
            <div className="text-xs font-medium text-slate-500 dark:text-slate-400">
              {t('Manufacturing.workCenter.dailyCapacity')}
            </div>
            <div className="font-bold text-slate-700 dark:text-slate-200 flex items-center gap-1">
              <span className="bg-white dark:bg-slate-800 px-2 py-0.5 rounded shadow-sm text-indigo-600 dark:text-indigo-400">
                {w.dailyCapacityMinutes}
              </span>
              <span className="text-xs text-slate-400">dk</span>
            </div>
          </div>

          <div className="flex items-center justify-end border-t border-slate-100 dark:border-slate-700/50 pt-4">
            <IconBtn
              title={t('Manufacturing.actions.edit')}
              onClick={() => setEditWorkCenter(w)}
              icon={<Pencil size={16} />}
            />
          </div>
        </div>
      ))}

      {editWorkCenter && (
        <WorkCenterFormModal workCenter={editWorkCenter} onClose={() => setEditWorkCenter(null)} />
      )}
    </div>
  );
};

const OperatorsTab = () => {
  const { t } = useTranslation();
  const query = useOperatorsQuery();
  const setActive = useSetOperatorActive();
  const rows = query.data ?? [];
  const [editOperator, setEditOperator] = useState<WorkCenterOperator | null>(null);

  if (query.isError) return <QueryError onRetry={() => query.refetch()} />;

  const toggle = async (op: WorkCenterOperator) => {
    const res = await setActive.mutateAsync({ id: op.id, active: !op.isActive }).catch((err) => {
      toastApiError(err);
      return null;
    });
    if (res?.isSuccess) toast.success(t('Manufacturing.operators.updated'));
    else if (res && !res.isSuccess)
      toast.error(res.errors?.[0] ?? t('Manufacturing.operators.actionFailed'));
  };

  if (rows.length === 0) {
    return (
      <div className="bg-white/50 dark:bg-slate-900/50 backdrop-blur-lg rounded-3xl p-12 border-2 border-dashed border-slate-200 dark:border-slate-800">
        <EmptyState title={t('Manufacturing.operators.empty')} />
      </div>
    );
  }

  return (
    <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-6">
      {rows.map((op) => (
        <div
          key={op.id}
          className="group bg-white/70 dark:bg-slate-800/70 backdrop-blur-xl border border-white/40 dark:border-slate-700/50 shadow-sm hover:shadow-xl rounded-2xl p-5 transition-all duration-300 flex items-center justify-between"
        >
          <div className="flex items-center gap-4">
            <div className="h-12 w-12 rounded-full bg-gradient-to-br from-indigo-100 to-purple-100 dark:from-indigo-900/50 dark:to-purple-900/50 flex items-center justify-center text-indigo-600 dark:text-indigo-400 shrink-0 shadow-inner">
              <UserCog size={24} />
            </div>

            <div>
              <div className="flex items-center gap-2 mb-1">
                <h3 className="font-bold text-slate-800 dark:text-slate-100">{op.employeeName}</h3>
                {!op.employeeActive && (
                  <Badge variant="danger" className="text-[10px] px-1.5 py-0">
                    Pasif Çalışan
                  </Badge>
                )}
                {op.isPrimary && (
                  <Badge
                    variant="success"
                    className="text-[10px] px-1.5 py-0 border border-green-200 dark:border-green-800"
                  >
                    <CheckCircle2 size={10} className="mr-1 inline" /> Asil
                  </Badge>
                )}
              </div>
              <div className="text-sm text-slate-500 dark:text-slate-400 flex items-center gap-1.5">
                <Factory size={12} className="text-slate-400" />
                <span className="font-medium text-slate-600 dark:text-slate-300">
                  {op.workCenterCode}
                </span>
                <span className="text-slate-300 dark:text-slate-600">•</span>
                <span className="truncate max-w-[120px]">{op.workCenterName}</span>
              </div>

              <div className="mt-2 flex items-center gap-2">
                <Badge
                  variant="info"
                  className="text-xs bg-blue-50 text-blue-600 dark:bg-blue-900/30 dark:text-blue-400 shadow-sm"
                >
                  {t(`Manufacturing.qualificationLevel.${op.qualificationLevel}`)}
                </Badge>
                {!op.isActive && (
                  <Badge variant="neutral" className="text-xs">
                    <AlertCircle size={10} className="mr-1 inline" /> {t('Manufacturing.common.no')}
                  </Badge>
                )}
              </div>
            </div>
          </div>

          <div className="flex flex-col gap-2 items-end shrink-0 pl-4 border-l border-slate-100 dark:border-slate-700/50">
            <IconBtn
              title={t('Manufacturing.actions.edit')}
              onClick={() => setEditOperator(op)}
              icon={<Pencil size={16} />}
            />
            <Button
              size="sm"
              variant={op.isActive ? 'ghost' : 'outline'}
              className={`rounded-lg ${op.isActive ? 'text-slate-500 hover:text-danger-600 hover:bg-danger-50 dark:hover:bg-danger-500/10' : 'hover:bg-success-50 text-success-600 hover:border-success-200 dark:hover:bg-success-500/10'}`}
              onClick={() => toggle(op)}
            >
              {op.isActive
                ? t('Manufacturing.actions.deactivate')
                : t('Manufacturing.actions.activate')}
            </Button>
          </div>
        </div>
      ))}

      {editOperator && (
        <OperatorFormModal operator={editOperator} onClose={() => setEditOperator(null)} />
      )}
    </div>
  );
};

interface IconBtnProps {
  title: string;
  onClick: () => void;
  icon: React.ReactNode;
  danger?: boolean;
}

const IconBtn = ({ title, onClick, icon, danger }: IconBtnProps) => (
  <button
    type="button"
    onClick={onClick}
    title={title}
    aria-label={title}
    className={`rounded-xl p-2.5 transition-all duration-200 ${
      danger
        ? 'text-red-400 hover:bg-red-50 hover:text-red-600 dark:hover:bg-red-500/10'
        : 'text-slate-400 hover:bg-indigo-50 hover:text-indigo-600 dark:hover:bg-indigo-500/20 dark:hover:text-indigo-400'
    }`}
  >
    {icon}
  </button>
);
