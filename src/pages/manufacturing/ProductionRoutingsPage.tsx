import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Factory, ListOrdered, Pencil, Plus, Trash2, UserCog, Workflow } from 'lucide-react';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { Card } from '@/shared/ui/Card/Card';
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
    <div className="space-y-6">
      <PageHeader
        title={t('Manufacturing.title')}
        subtitle={t('Manufacturing.subtitle')}
        actions={
          <Button onClick={() => setCreateModal(tab)}>
            <Plus className="h-4 w-4" />
            {t(`Manufacturing.actions.new_${tab}`)}
          </Button>
        }
      />

      <div className="flex flex-wrap gap-2">
        {tabs.map((item) => {
          const Icon = item.icon;
          const active = tab === item.id;
          return (
            <button
              key={item.id}
              type="button"
              onClick={() => setTab(item.id)}
              className={`inline-flex items-center gap-2 rounded-lg px-4 py-2 text-sm font-medium transition ${
                active
                  ? 'bg-primary-600 text-white'
                  : 'bg-slate-100 text-slate-600 hover:bg-slate-200 dark:bg-slate-800 dark:text-slate-300 dark:hover:bg-slate-700'
              }`}
            >
              <Icon className="h-4 w-4" />
              {item.label}
            </button>
          );
        })}
      </div>

      {tab === 'routings' && <RoutingsTab />}
      {tab === 'workCenters' && <WorkCentersTab />}
      {tab === 'operators' && <OperatorsTab />}

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

  return (
    <Card className="overflow-x-auto p-0">
      {rows.length === 0 ? (
        <EmptyState title={t('Manufacturing.routings.empty')} />
      ) : (
        <table className="w-full text-left text-sm">
          <thead className="border-b border-slate-200 text-xs uppercase text-slate-500 dark:border-slate-700">
            <tr>
              <th className="px-4 py-3">{t('Manufacturing.routing.code')}</th>
              <th className="px-4 py-3">{t('Manufacturing.routing.name')}</th>
              <th className="px-4 py-3">{t('Manufacturing.routing.status')}</th>
              <th className="px-4 py-3 text-right">{t('Manufacturing.routing.stepCount')}</th>
              <th className="px-4 py-3 text-right">{t('Manufacturing.actions.actions')}</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
            {rows.map((r) => (
              <tr key={r.id} className="hover:bg-slate-50 dark:hover:bg-slate-800/50">
                <td className="px-4 py-3 font-medium">{r.code}</td>
                <td className="px-4 py-3">{r.name}</td>
                <td className="px-4 py-3">
                  <Badge variant={statusTone[r.status]}>
                    {t(`Manufacturing.routingStatus.${r.status}`)}
                  </Badge>
                </td>
                <td className="px-4 py-3 text-right tabular-nums">{r.stepCount}</td>
                <td className="px-4 py-3">
                  <div className="flex flex-wrap items-center justify-end gap-1">
                    <IconBtn
                      title={t('Manufacturing.actions.edit')}
                      onClick={() => setEditRouting(r)}
                      icon={<Pencil size={15} />}
                    />
                    {r.status === 'Draft' && (
                      <IconBtn
                        title={t('Manufacturing.actions.steps')}
                        onClick={() => setStepsRouting(r)}
                        icon={<ListOrdered size={15} />}
                      />
                    )}
                    {r.status === 'Draft' && (
                      <Button
                        size="sm"
                        variant="outline"
                        onClick={() => runTransition(r.id, 'activate')}
                      >
                        {t('Manufacturing.actions.activate')}
                      </Button>
                    )}
                    {r.status !== 'Archived' && (
                      <Button
                        size="sm"
                        variant="ghost"
                        onClick={() => runTransition(r.id, 'archive')}
                      >
                        {t('Manufacturing.actions.archive')}
                      </Button>
                    )}
                    {r.status === 'Archived' && (
                      <Button
                        size="sm"
                        variant="ghost"
                        onClick={() => runTransition(r.id, 'restore')}
                      >
                        {t('Manufacturing.actions.restore')}
                      </Button>
                    )}
                    {r.status === 'Draft' && (
                      <IconBtn
                        title={t('Manufacturing.actions.delete')}
                        onClick={() => remove(r)}
                        icon={<Trash2 size={15} />}
                        danger
                      />
                    )}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

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
    </Card>
  );
};

const WorkCentersTab = () => {
  const { t } = useTranslation();
  const query = useWorkCentersQuery(true);
  const rows = query.data ?? [];
  const [editWorkCenter, setEditWorkCenter] = useState<WorkCenter | null>(null);

  if (query.isError) return <QueryError onRetry={() => query.refetch()} />;

  return (
    <Card className="overflow-x-auto p-0">
      {rows.length === 0 ? (
        <EmptyState title={t('Manufacturing.workCenters.empty')} />
      ) : (
        <table className="w-full text-left text-sm">
          <thead className="border-b border-slate-200 text-xs uppercase text-slate-500 dark:border-slate-700">
            <tr>
              <th className="px-4 py-3">{t('Manufacturing.workCenter.code')}</th>
              <th className="px-4 py-3">{t('Manufacturing.workCenter.name')}</th>
              <th className="px-4 py-3 text-right">
                {t('Manufacturing.workCenter.dailyCapacity')}
              </th>
              <th className="px-4 py-3">{t('Manufacturing.workCenter.active')}</th>
              <th className="px-4 py-3 text-right">{t('Manufacturing.actions.actions')}</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
            {rows.map((w) => (
              <tr key={w.id} className="hover:bg-slate-50 dark:hover:bg-slate-800/50">
                <td className="px-4 py-3 font-medium">{w.code}</td>
                <td className="px-4 py-3">{w.name}</td>
                <td className="px-4 py-3 text-right tabular-nums">{w.dailyCapacityMinutes}</td>
                <td className="px-4 py-3">
                  <Badge variant={w.isActive ? 'success' : 'neutral'}>
                    {w.isActive ? t('Manufacturing.common.yes') : t('Manufacturing.common.no')}
                  </Badge>
                </td>
                <td className="px-4 py-3 text-right">
                  <IconBtn
                    title={t('Manufacturing.actions.edit')}
                    onClick={() => setEditWorkCenter(w)}
                    icon={<Pencil size={15} />}
                  />
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {editWorkCenter && (
        <WorkCenterFormModal workCenter={editWorkCenter} onClose={() => setEditWorkCenter(null)} />
      )}
    </Card>
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

  return (
    <Card className="overflow-x-auto p-0">
      {rows.length === 0 ? (
        <EmptyState title={t('Manufacturing.operators.empty')} />
      ) : (
        <table className="w-full text-left text-sm">
          <thead className="border-b border-slate-200 text-xs uppercase text-slate-500 dark:border-slate-700">
            <tr>
              <th className="px-4 py-3">{t('Manufacturing.operator.workCenter')}</th>
              <th className="px-4 py-3">{t('Manufacturing.operator.employee')}</th>
              <th className="px-4 py-3">{t('Manufacturing.operator.level')}</th>
              <th className="px-4 py-3">{t('Manufacturing.operator.primary')}</th>
              <th className="px-4 py-3">{t('Manufacturing.operator.active')}</th>
              <th className="px-4 py-3 text-right">{t('Manufacturing.actions.actions')}</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
            {rows.map((op) => (
              <tr key={op.id} className="hover:bg-slate-50 dark:hover:bg-slate-800/50">
                <td className="px-4 py-3 font-medium">
                  {op.workCenterCode}
                  <span className="text-slate-400"> · {op.workCenterName}</span>
                </td>
                <td className="px-4 py-3">
                  {op.employeeName}
                  {!op.employeeActive && (
                    <Badge variant="danger" className="ml-2">
                      {t('Manufacturing.operators.employeeInactive')}
                    </Badge>
                  )}
                </td>
                <td className="px-4 py-3">
                  <Badge variant="info">
                    {t(`Manufacturing.qualificationLevel.${op.qualificationLevel}`)}
                  </Badge>
                </td>
                <td className="px-4 py-3">
                  {op.isPrimary ? t('Manufacturing.common.yes') : t('Manufacturing.common.no')}
                </td>
                <td className="px-4 py-3">
                  <Badge variant={op.isActive ? 'success' : 'neutral'}>
                    {op.isActive ? t('Manufacturing.common.yes') : t('Manufacturing.common.no')}
                  </Badge>
                </td>
                <td className="px-4 py-3">
                  <div className="flex items-center justify-end gap-1">
                    <IconBtn
                      title={t('Manufacturing.actions.edit')}
                      onClick={() => setEditOperator(op)}
                      icon={<Pencil size={15} />}
                    />
                    <Button size="sm" variant="ghost" onClick={() => toggle(op)}>
                      {op.isActive
                        ? t('Manufacturing.actions.deactivate')
                        : t('Manufacturing.actions.activate')}
                    </Button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {editOperator && (
        <OperatorFormModal operator={editOperator} onClose={() => setEditOperator(null)} />
      )}
    </Card>
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
    className={`rounded p-1.5 text-slate-500 ${
      danger
        ? 'hover:bg-danger-50 hover:text-danger-700 dark:hover:bg-danger-500/10'
        : 'hover:bg-primary-50 hover:text-primary-700 dark:hover:bg-primary-500/10'
    }`}
  >
    {icon}
  </button>
);
