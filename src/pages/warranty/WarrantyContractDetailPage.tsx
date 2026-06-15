import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate, useParams } from 'react-router-dom';
import { toast } from 'sonner';
import { ArrowLeft, CalendarPlus, ShieldOff } from 'lucide-react';
import { toastApiError } from '@/shared/lib/mutationToast';
import {
  useCancelWarrantyContract,
  useExtendWarrantyContract,
  useWarrantyContractQuery,
} from '@/features/warranty/hooks/useWarrantyContracts';
import { ServiceTicketForm } from '@/features/warranty/ui/ServiceTicketForm';

export const WarrantyContractDetailPage = () => {
  const { t, i18n } = useTranslation();
  const navigate = useNavigate();
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
    <div className="space-y-4 p-4 sm:p-6">
      <button
        type="button"
        onClick={() => navigate('/dashboard/warranty/contracts')}
        className="inline-flex items-center gap-1 text-sm text-indigo-600 hover:text-indigo-500 dark:text-indigo-300"
      >
        <ArrowLeft className="h-4 w-4" />
        {t('Common.Back', { defaultValue: 'Back' })}
      </button>

      <header className="space-y-1">
        <h1 className="text-xl font-semibold text-slate-900 dark:text-slate-100 sm:text-2xl">
          {contract.number}
        </h1>
        <p className="text-sm text-slate-500 dark:text-slate-400">
          {t(`Warranty.CoverageType.${contract.coverageType}`, {
            defaultValue: contract.coverageType,
          })}
          {' • '}
          {t(`Warranty.Status.${contract.status}`, { defaultValue: contract.status })}
        </p>
      </header>

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
          <label className="block text-sm">
            <span className="block font-medium text-slate-700 dark:text-slate-200">
              {t('Warranty.Action.MonthsToAdd', { defaultValue: 'Months' })}
            </span>
            <input
              type="number"
              min={1}
              max={120}
              value={monthsToAdd}
              onChange={(e) => setMonthsToAdd(Number(e.target.value))}
              className="mt-1 w-28 rounded border border-slate-300 bg-white p-2 dark:border-slate-600 dark:bg-slate-800 dark:text-slate-100"
            />
          </label>
          <label className="flex-1 text-sm">
            <span className="block font-medium text-slate-700 dark:text-slate-200">
              {t('Warranty.Action.ReasonOptional', { defaultValue: 'Reason (optional)' })}
            </span>
            <input
              type="text"
              maxLength={500}
              value={extendReason}
              onChange={(e) => setExtendReason(e.target.value)}
              className="mt-1 w-full rounded border border-slate-300 bg-white p-2 dark:border-slate-600 dark:bg-slate-800 dark:text-slate-100"
            />
          </label>
          <button
            type="button"
            onClick={handleExtend}
            disabled={extendMutation.isPending}
            className="rounded bg-indigo-600 px-3 py-2 text-sm font-medium text-white hover:bg-indigo-500 disabled:opacity-60"
          >
            {t('Warranty.Action.Extend', { defaultValue: 'Extend' })}
          </button>
        </div>
      </section>

      {contract.status !== 'Cancelled' ? (
        <section className="rounded-lg border border-rose-200 bg-rose-50 p-4 shadow-sm dark:border-rose-900/50 dark:bg-rose-950/30">
          <h2 className="mb-3 flex items-center gap-2 text-sm font-semibold text-rose-700 dark:text-rose-200">
            <ShieldOff className="h-4 w-4" />
            {t('Warranty.Action.Cancel', { defaultValue: 'Cancel warranty' })}
          </h2>
          <div className="flex flex-col gap-2 sm:flex-row sm:items-end">
            <label className="flex-1 text-sm">
              <span className="block font-medium text-slate-700 dark:text-slate-200">
                {t('Warranty.Action.ReasonRequired', { defaultValue: 'Reason' })}
              </span>
              <input
                type="text"
                maxLength={1000}
                value={cancelReason}
                onChange={(e) => setCancelReason(e.target.value)}
                className="mt-1 w-full rounded border border-slate-300 bg-white p-2 dark:border-slate-600 dark:bg-slate-800 dark:text-slate-100"
              />
            </label>
            <button
              type="button"
              onClick={handleCancel}
              disabled={cancelMutation.isPending}
              className="rounded bg-rose-600 px-3 py-2 text-sm font-medium text-white hover:bg-rose-500 disabled:opacity-60"
            >
              {t('Warranty.Action.Cancel', { defaultValue: 'Cancel' })}
            </button>
          </div>
        </section>
      ) : null}

      <section>
        <div className="mb-2 flex items-center justify-between">
          <h2 className="text-sm font-semibold text-slate-800 dark:text-slate-100">
            {t('Warranty.ServiceTicket.OpenNew', { defaultValue: 'Open a service ticket' })}
          </h2>
          <button
            type="button"
            onClick={() => setShowTicketForm((v) => !v)}
            className="text-xs text-indigo-600 hover:text-indigo-500 dark:text-indigo-300"
          >
            {showTicketForm
              ? t('Common.Hide', { defaultValue: 'Hide' })
              : t('Common.Show', { defaultValue: 'Show' })}
          </button>
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
    </div>
  );
};

export default WarrantyContractDetailPage;
