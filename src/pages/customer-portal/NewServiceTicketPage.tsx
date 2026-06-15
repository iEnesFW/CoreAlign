import type { FormEvent } from 'react';
import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { ChevronLeft } from 'lucide-react';
import { useCreateMyServiceTicket } from '@/features/customer-portal/hooks/useCustomerPortalQueries';
import type {
  ServiceTicketPriority,
  ServiceTicketType,
} from '@/features/warranty/model/warranty.types';

const TYPES: ServiceTicketType[] = [
  'PreventiveMaintenance',
  'WarrantyClaim',
  'OutOfWarrantyRepair',
  'Inspection',
];

const PRIORITIES: ServiceTicketPriority[] = ['Low', 'Normal', 'High', 'Urgent'];

export const NewServiceTicketPage = () => {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const createTicket = useCreateMyServiceTicket();

  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [type, setType] = useState<ServiceTicketType>('WarrantyClaim');
  const [priority, setPriority] = useState<ServiceTicketPriority>('Normal');

  const onSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!title.trim() || !description.trim()) return;
    const result = await createTicket.mutateAsync({
      type,
      priority,
      title: title.trim(),
      descriptionMd: description.trim(),
    });
    const newId = result?.data?.id;
    if (newId) {
      navigate(`/customer-portal/service-tickets/${newId}`);
    } else {
      navigate('/customer-portal/service-tickets');
    }
  };

  return (
    <div className="space-y-4 max-w-2xl">
      <Link
        to="/customer-portal/service-tickets"
        className="inline-flex items-center gap-1 text-sm text-blue-600 hover:underline"
      >
        <ChevronLeft size={16} /> {t('CustomerPortal.Common.Back')}
      </Link>

      <h1 className="text-xl font-semibold">{t('CustomerPortal.ServiceTicket.NewTitle')}</h1>

      <form
        onSubmit={onSubmit}
        className="rounded-lg border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-4 sm:p-6 space-y-4"
      >
        <div>
          <label className="block text-xs text-slate-500 mb-1">
            {t('CustomerPortal.ServiceTicket.FieldTitle')}
          </label>
          <input
            type="text"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            required
            maxLength={200}
            className="w-full px-3 py-2 rounded-md border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-950 text-sm"
          />
        </div>
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <div>
            <label className="block text-xs text-slate-500 mb-1">
              {t('CustomerPortal.ServiceTicket.FieldType')}
            </label>
            <select
              value={type}
              onChange={(e) => setType(e.target.value as ServiceTicketType)}
              className="w-full px-3 py-2 rounded-md border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-950 text-sm"
            >
              {TYPES.map((tp) => (
                <option key={tp} value={tp}>
                  {t(`CustomerPortal.ServiceTicket.Type.${tp}`)}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label className="block text-xs text-slate-500 mb-1">
              {t('CustomerPortal.ServiceTicket.FieldPriority')}
            </label>
            <select
              value={priority}
              onChange={(e) => setPriority(e.target.value as ServiceTicketPriority)}
              className="w-full px-3 py-2 rounded-md border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-950 text-sm"
            >
              {PRIORITIES.map((pr) => (
                <option key={pr} value={pr}>
                  {t(`CustomerPortal.ServiceTicket.Priority.${pr}`)}
                </option>
              ))}
            </select>
          </div>
        </div>
        <div>
          <label className="block text-xs text-slate-500 mb-1">
            {t('CustomerPortal.ServiceTicket.FieldDescription')}
          </label>
          <textarea
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            required
            rows={6}
            className="w-full px-3 py-2 rounded-md border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-950 text-sm"
          />
        </div>
        <div className="flex items-center justify-end gap-2">
          <Link
            to="/customer-portal/service-tickets"
            className="px-3 py-2 rounded-md text-sm border border-slate-300 dark:border-slate-700"
          >
            {t('CustomerPortal.Common.Cancel')}
          </Link>
          <button
            type="submit"
            disabled={createTicket.isPending}
            className="px-3 py-2 rounded-md text-sm bg-blue-600 text-white hover:bg-blue-700 disabled:opacity-60"
          >
            {createTicket.isPending
              ? t('CustomerPortal.Common.Submitting')
              : t('CustomerPortal.ServiceTicket.Submit')}
          </button>
        </div>
      </form>
    </div>
  );
};

export default NewServiceTicketPage;
