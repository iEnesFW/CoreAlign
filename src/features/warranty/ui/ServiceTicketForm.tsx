import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useCreateServiceTicket } from '../hooks/useServiceTickets';
import type { ServiceTicketPriority, ServiceTicketType } from '../model/warranty.types';

interface Props {
  customerId: string;
  warrantyContractId?: string | null;
  onCreated?: () => void;
  onCancel?: () => void;
}

const TYPES: ServiceTicketType[] = [
  'PreventiveMaintenance',
  'WarrantyClaim',
  'OutOfWarrantyRepair',
  'Inspection',
];

const PRIORITIES: ServiceTicketPriority[] = ['Low', 'Normal', 'High', 'Urgent'];

export const ServiceTicketForm = ({
  customerId,
  warrantyContractId,
  onCreated,
  onCancel,
}: Props) => {
  const { t } = useTranslation();
  const createMutation = useCreateServiceTicket();

  const [type, setType] = useState<ServiceTicketType>('WarrantyClaim');
  const [priority, setPriority] = useState<ServiceTicketPriority>('Normal');
  const [title, setTitle] = useState('');
  const [descriptionMd, setDescriptionMd] = useState('');

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!title.trim() || !descriptionMd.trim()) {
      toast.error(
        t('Warranty.ServiceTicket.Validation.Required', {
          defaultValue: 'Title and description are required.',
        }),
      );
      return;
    }
    try {
      await createMutation.mutateAsync({
        customerId,
        type,
        priority,
        title: title.trim(),
        descriptionMd: descriptionMd.trim(),
        warrantyContractId: warrantyContractId ?? null,
      });
      toast.success(
        t('Warranty.ServiceTicket.Toast.Created', { defaultValue: 'Service ticket created.' }),
      );
      onCreated?.();
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <form
      onSubmit={submit}
      className="space-y-3 rounded-lg border border-slate-200 bg-white p-4 shadow-sm dark:border-slate-700 dark:bg-slate-900"
    >
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
        <label className="block text-sm">
          <span className="block font-medium text-slate-700 dark:text-slate-200">
            {t('Warranty.ServiceTicket.Type.Label', { defaultValue: 'Type' })}
          </span>
          <select
            className="mt-1 w-full rounded border border-slate-300 bg-white p-2 dark:border-slate-600 dark:bg-slate-800 dark:text-slate-100"
            value={type}
            onChange={(e) => setType(e.target.value as ServiceTicketType)}
          >
            {TYPES.map((tt) => (
              <option key={tt} value={tt}>
                {t(`Warranty.ServiceTicket.Type.${tt}`, { defaultValue: tt })}
              </option>
            ))}
          </select>
        </label>
        <label className="block text-sm">
          <span className="block font-medium text-slate-700 dark:text-slate-200">
            {t('Warranty.ServiceTicket.Priority.Label', { defaultValue: 'Priority' })}
          </span>
          <select
            className="mt-1 w-full rounded border border-slate-300 bg-white p-2 dark:border-slate-600 dark:bg-slate-800 dark:text-slate-100"
            value={priority}
            onChange={(e) => setPriority(e.target.value as ServiceTicketPriority)}
          >
            {PRIORITIES.map((p) => (
              <option key={p} value={p}>
                {t(`Warranty.ServiceTicket.Priority.${p}`, { defaultValue: p })}
              </option>
            ))}
          </select>
        </label>
      </div>
      <label className="block text-sm">
        <span className="block font-medium text-slate-700 dark:text-slate-200">
          {t('Warranty.ServiceTicket.Title', { defaultValue: 'Title' })}
        </span>
        <input
          type="text"
          maxLength={200}
          className="mt-1 w-full rounded border border-slate-300 bg-white p-2 dark:border-slate-600 dark:bg-slate-800 dark:text-slate-100"
          value={title}
          onChange={(e) => setTitle(e.target.value)}
        />
      </label>
      <label className="block text-sm">
        <span className="block font-medium text-slate-700 dark:text-slate-200">
          {t('Warranty.ServiceTicket.Description', { defaultValue: 'Description' })}
        </span>
        <textarea
          rows={5}
          maxLength={8000}
          className="mt-1 w-full rounded border border-slate-300 bg-white p-2 dark:border-slate-600 dark:bg-slate-800 dark:text-slate-100"
          value={descriptionMd}
          onChange={(e) => setDescriptionMd(e.target.value)}
        />
      </label>
      <div className="flex items-center justify-end gap-2 pt-2">
        {onCancel ? (
          <button
            type="button"
            className="rounded border border-slate-300 px-3 py-1.5 text-sm text-slate-700 hover:bg-slate-100 dark:border-slate-600 dark:text-slate-200 dark:hover:bg-slate-800"
            onClick={onCancel}
          >
            {t('Common.Cancel', { defaultValue: 'Cancel' })}
          </button>
        ) : null}
        <button
          type="submit"
          disabled={createMutation.isPending}
          className="rounded bg-indigo-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-indigo-500 disabled:opacity-60"
        >
          {t('Warranty.ServiceTicket.Action.Submit', { defaultValue: 'Submit ticket' })}
        </button>
      </div>
    </form>
  );
};

export default ServiceTicketForm;
