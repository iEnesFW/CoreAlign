import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { X } from 'lucide-react';
import { useVendorsQuery } from '@/features/vendors/hooks/useVendorQueries';
import type { ConvertRequisitionInput } from '../model/mrp.types';

interface Props {
  requisitionId: string;
  requisitionNumber: string;
  defaultVendorId?: string | null;
  isSubmitting?: boolean;
  onConfirm: (input: ConvertRequisitionInput) => void;
  onCancel: () => void;
}

const CURRENCIES = ['TRY', 'USD', 'EUR', 'GBP'];

export const ConvertRequisitionDialog = ({
  requisitionId,
  requisitionNumber,
  defaultVendorId,
  isSubmitting = false,
  onConfirm,
  onCancel,
}: Props) => {
  const { t } = useTranslation();
  const vendors = useVendorsQuery({ page: 1, pageSize: 100 });
  const [vendorId, setVendorId] = useState<string>(defaultVendorId ?? '');
  const [currency, setCurrency] = useState<string>('TRY');
  const [expectedDate, setExpectedDate] = useState<string>('');

  const canSubmit = vendorId.length > 0 && !isSubmitting;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!canSubmit) return;
    onConfirm({
      id: requisitionId,
      vendorId,
      currency,
      expectedDate: expectedDate ? new Date(expectedDate).toISOString() : null,
    });
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 p-4">
      <div
        role="dialog"
        aria-label={t('Mrp.Convert.Title', { number: requisitionNumber }) ?? 'Convert'}
        className="w-full max-w-md rounded-lg border border-slate-200 bg-white p-4 shadow-xl dark:border-slate-700 dark:bg-slate-900"
      >
        <header className="mb-3 flex items-center justify-between">
          <h2 className="text-sm font-semibold text-slate-800 dark:text-slate-100">
            {t('Mrp.Convert.Title', { number: requisitionNumber })}
          </h2>
          <button
            type="button"
            onClick={onCancel}
            aria-label={t('Common.Close') ?? 'Close'}
            className="rounded p-1 text-slate-500 hover:bg-slate-100 dark:text-slate-400 dark:hover:bg-slate-700"
          >
            <X className="h-4 w-4" />
          </button>
        </header>
        <form onSubmit={handleSubmit} className="space-y-3">
          <label className="flex flex-col gap-1">
            <span className="text-xs font-medium text-slate-600 dark:text-slate-300">
              {t('Mrp.Convert.Vendor')}
            </span>
            <select
              value={vendorId}
              onChange={(e) => setVendorId(e.target.value)}
              className="rounded-md border border-slate-300 bg-white px-3 py-2 text-sm dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100"
            >
              <option value="">{t('Mrp.Convert.SelectVendor')}</option>
              {(vendors.data?.data?.items ?? []).map((v) => (
                <option key={v.id} value={v.id}>
                  {v.name}
                </option>
              ))}
            </select>
          </label>
          <div className="grid grid-cols-2 gap-3">
            <label className="flex flex-col gap-1">
              <span className="text-xs font-medium text-slate-600 dark:text-slate-300">
                {t('Mrp.Convert.Currency')}
              </span>
              <select
                value={currency}
                onChange={(e) => setCurrency(e.target.value)}
                className="rounded-md border border-slate-300 bg-white px-3 py-2 text-sm dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100"
              >
                {CURRENCIES.map((c) => (
                  <option key={c} value={c}>
                    {c}
                  </option>
                ))}
              </select>
            </label>
            <label className="flex flex-col gap-1">
              <span className="text-xs font-medium text-slate-600 dark:text-slate-300">
                {t('Mrp.Convert.ExpectedDate')}
              </span>
              <input
                type="date"
                value={expectedDate}
                onChange={(e) => setExpectedDate(e.target.value)}
                className="rounded-md border border-slate-300 bg-white px-3 py-2 text-sm dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100"
              />
            </label>
          </div>
          <div className="flex justify-end gap-2 pt-2">
            <button
              type="button"
              onClick={onCancel}
              className="rounded-md border border-slate-300 px-3 py-2 text-sm text-slate-700 hover:bg-slate-100 dark:border-slate-600 dark:text-slate-200 dark:hover:bg-slate-700"
            >
              {t('Common.Cancel')}
            </button>
            <button
              type="submit"
              disabled={!canSubmit}
              className="rounded-md bg-indigo-600 px-3 py-2 text-sm font-medium text-white hover:bg-indigo-500 disabled:cursor-not-allowed disabled:bg-indigo-400"
            >
              {t('Mrp.Action.Convert')}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
