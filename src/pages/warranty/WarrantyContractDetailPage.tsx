import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router-dom';
import { toast } from 'sonner';
import { CalendarPlus, ShieldCheck, ShieldOff } from 'lucide-react';
import { toastApiError } from '@/shared/lib/mutationToast';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { DetailPageTemplate } from '@/shared/ui/PageTemplate/PageTemplate';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import {
  useCancelWarrantyContract,
  useExtendWarrantyContract,
  useWarrantyContractQuery,
} from '@/features/warranty/hooks/useWarrantyContracts';
import { ServiceTicketForm } from '@/features/warranty/ui/ServiceTicketForm';

export const WarrantyContractDetailPage = () => {
  const { t, i18n } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const { data, isLoading } = useWarrantyContractQuery(id);
  const contract = data?.data;

  const extendMutation = useExtendWarrantyContract();
  const cancelMutation = useCancelWarrantyContract();

  const [showTicketForm, setShowTicketForm] = useState(false);
  const [monthsToAdd, setMonthsToAdd] = useState(12);
  const [extendReason, setExtendReason] = useState('');
  const [cancelReason, setCancelReason] = useState('');

  if (!id || isLoading || !contract) {
    return (
      <div className="p-6 text-sm text-slate-500 dark:text-slate-400">
        {t('Common.Loading', { defaultValue: 'Loading...' })}
      </div>
    );
  }

  const handleExtend = async () => {
    if (monthsToAdd <= 0) return;
    try {
      await extendMutation.mutateAsync({
        id: contract.id,
        monthsAdded: monthsToAdd,
        reason: extendReason.trim() || null,
      });
      toast.success(t('Warranty.Action.ExtendedToast', { defaultValue: 'Warranty extended.' }));
      setExtendReason('');
    } catch (err) {
      toastApiError(err);
    }
  };

  const handleCancel = async () => {
    if (!cancelReason.trim()) {
      toast.error(
        t('Warranty.Action.CancelReasonRequired', {
          defaultValue: 'Cancellation reason is required.',
        }),
      );
      return;
    }
    try {
      await cancelMutation.mutateAsync({ id: contract.id, reason: cancelReason.trim() });
      toast.success(t('Warranty.Action.CancelledToast', { defaultValue: 'Warranty cancelled.' }));
      setCancelReason('');
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <DetailPageTemplate
      header={
        <PageHeader
          icon={<ShieldCheck size={20} />}
          title={contract.number}
          subtitle={`${t(`Warranty.CoverageType.${contract.coverageType}`, {
            defaultValue: contract.coverageType,
          })} • ${t(`Warranty.Status.${contract.status}`, { defaultValue: contract.status })}`}
          crumbs={[
            {
              label: t('Common.Back', { defaultValue: 'Back' }),
              to: '/dashboard/warranty/contracts',
            },
            { label: contract.number },
          ]}
        />
      }
    >
      <section className="grid grid-cols-1 gap-3 sm:grid-cols-2">
        <div className="rounded-lg border border-slate-200 bg-white p-3 text-sm shadow-sm dark:border-slate-700 dark:bg-slate-900">
          <span className="block text-xs font-medium uppercase text-slate-500 dark:text-slate-400">
            {t('Warranty.StartDate', { defaultValue: 'Start' })}
          </span>
          <span className="mt-1 block text-slate-800 dark:text-slate-100">
            {new Date(contract.startDate).toLocaleDateString(i18n.language)}
          </span>
        </div>
        <div className="rounded-lg border border-slate-200 bg-white p-3 text-sm shadow-sm dark:border-slate-700 dark:bg-slate-900">
          <span className="block text-xs font-medium uppercase text-slate-500 dark:text-slate-400">
            {t('Warranty.EndDate', { defaultValue: 'End' })}
          </span>
          <span className="mt-1 block text-slate-800 dark:text-slate-100">
            {new Date(contract.endDate).toLocaleDateString(i18n.language)}
          </span>
        </div>
      </section>

      <section className="rounded-lg border border-slate-200 bg-white p-4 shadow-sm dark:border-slate-700 dark:bg-slate-900">
        <h2 className="mb-3 flex items-center gap-2 text-sm font-semibold text-slate-800 dark:text-slate-100">
          <CalendarPlus className="h-4 w-4" />
          {t('Warranty.Action.Extend', { defaultValue: 'Extend coverage' })}
        </h2>
        <div className="flex flex-col gap-2 sm:flex-row sm:items-end">
          <Input
            label={t('Warranty.Action.MonthsToAdd', { defaultValue: 'Months' })}
            type="number"
            min={1}
            max={120}
            value={monthsToAdd}
            onChange={(e) => setMonthsToAdd(Number(e.target.value))}
            className="w-28"
          />
          <Input
            label={t('Warranty.Action.ReasonOptional', { defaultValue: 'Reason (optional)' })}
            type="text"
            maxLength={500}
            value={extendReason}
            onChange={(e) => setExtendReason(e.target.value)}
            className="flex-1"
          />
          <Button
            type="button"
            size="sm"
            onClick={handleExtend}
            isLoading={extendMutation.isPending}
          >
            {t('Warranty.Action.Extend', { defaultValue: 'Extend' })}
          </Button>
        </div>
      </section>

      {contract.status !== 'Cancelled' ? (
        <section className="rounded-lg border border-danger-200 bg-danger-50 p-4 shadow-sm dark:border-danger-900/50 dark:bg-danger-950/30">
          <h2 className="mb-3 flex items-center gap-2 text-sm font-semibold text-danger-700 dark:text-danger-200">
            <ShieldOff className="h-4 w-4" />
            {t('Warranty.Action.Cancel', { defaultValue: 'Cancel warranty' })}
          </h2>
          <div className="flex flex-col gap-2 sm:flex-row sm:items-end">
            <Input
              label={t('Warranty.Action.ReasonRequired', { defaultValue: 'Reason' })}
              type="text"
              maxLength={1000}
              value={cancelReason}
              onChange={(e) => setCancelReason(e.target.value)}
              className="flex-1"
            />
            <Button
              type="button"
              variant="danger"
              size="sm"
              onClick={handleCancel}
              isLoading={cancelMutation.isPending}
            >
              {t('Warranty.Action.Cancel', { defaultValue: 'Cancel' })}
            </Button>
          </div>
        </section>
      ) : null}

      <section>
        <div className="mb-2 flex items-center justify-between">
          <h2 className="text-sm font-semibold text-slate-800 dark:text-slate-100">
            {t('Warranty.ServiceTicket.OpenNew', { defaultValue: 'Open a service ticket' })}
          </h2>
          <Button
            type="button"
            variant="ghost"
            size="sm"
            onClick={() => setShowTicketForm((v) => !v)}
          >
            {showTicketForm
              ? t('Common.Hide', { defaultValue: 'Hide' })
              : t('Common.Show', { defaultValue: 'Show' })}
          </Button>
        </div>
        {showTicketForm ? (
          <ServiceTicketForm
            customerId={contract.customerId}
            warrantyContractId={contract.id}
            onCreated={() => setShowTicketForm(false)}
            onCancel={() => setShowTicketForm(false)}
          />
        ) : null}
      </section>
    </DetailPageTemplate>
  );
};

export default WarrantyContractDetailPage;
