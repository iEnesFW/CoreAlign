import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Download, FileSpreadsheet, FileText } from 'lucide-react';
import { customersApi } from '@/features/customers/api/customersApi';
import { safeRequest } from '@/shared/lib/safeRequest';
import { logger } from '@/shared/lib/logger';

interface CustomerStatementButtonProps {
  customerId: string;
  customerName: string;
}

const todayIso = () => new Date().toISOString().slice(0, 10);

const oneYearAgoIso = () => {
  const d = new Date();
  d.setFullYear(d.getFullYear() - 1);
  return d.toISOString().slice(0, 10);
};

export const CustomerStatementButton = ({
  customerId,
  customerName,
}: CustomerStatementButtonProps) => {
  const { t } = useTranslation();
  const [open, setOpen] = useState(false);
  const [from, setFrom] = useState<string>(oneYearAgoIso());
  const [to, setTo] = useState<string>(todayIso());
  const [busyFormat, setBusyFormat] = useState<'pdf' | 'xlsx' | null>(null);

  const triggerDownload = async (format: 'pdf' | 'xlsx') => {
    setBusyFormat(format);
    const [response, error] = await safeRequest(
      customersApi.downloadStatement(customerId, {
        from: from || null,
        to: to || null,
        format,
      }),
    );
    setBusyFormat(null);
    if (error || !response) {
      logger.warn('Statement download failed', { customerId, error: String(error) });
      return;
    }

    const blob = response.data instanceof Blob ? response.data : new Blob([response.data]);
    const url = URL.createObjectURL(blob);
    const safeName = customerName.replace(/[^\w-]+/g, '-').toLowerCase();
    const stamp = `${from || 'all'}-${to || 'all'}`;
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = `statement-${safeName}-${stamp}.${format}`;
    anchor.rel = 'noopener';
    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
    URL.revokeObjectURL(url);
    setOpen(false);
  };

  return (
    <div className="relative">
      <button
        type="button"
        onClick={() => setOpen((prev) => !prev)}
        className="inline-flex items-center gap-2 rounded-md border border-slate-300 bg-white px-3 py-1.5 text-sm font-medium text-slate-700 shadow-sm transition hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
        aria-label={t('customers.statement.openMenu')}
      >
        <Download size={14} />
        {t('customers.statement.title')}
      </button>

      {open && (
        <div className="absolute right-0 z-30 mt-2 w-80 rounded-lg border border-slate-200 bg-white p-4 shadow-lg dark:border-slate-700 dark:bg-slate-900">
          <p className="mb-3 text-xs text-slate-500 dark:text-slate-400">
            {t('customers.statement.subtitle')}
          </p>

          <label className="mb-2 block text-xs font-medium text-slate-600 dark:text-slate-300">
            {t('customers.statement.from')}
            <input
              type="date"
              value={from}
              onChange={(e) => setFrom(e.target.value)}
              className="mt-1 w-full rounded-md border border-slate-300 bg-white px-2 py-1.5 text-sm text-slate-900 focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100"
            />
          </label>

          <label className="mb-3 block text-xs font-medium text-slate-600 dark:text-slate-300">
            {t('customers.statement.to')}
            <input
              type="date"
              value={to}
              onChange={(e) => setTo(e.target.value)}
              className="mt-1 w-full rounded-md border border-slate-300 bg-white px-2 py-1.5 text-sm text-slate-900 focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100"
            />
          </label>

          <div className="flex gap-2">
            <button
              type="button"
              onClick={() => triggerDownload('pdf')}
              disabled={busyFormat !== null}
              className="inline-flex flex-1 items-center justify-center gap-2 rounded-md bg-primary-600 px-3 py-1.5 text-sm font-medium text-white shadow-sm transition hover:bg-primary-700 disabled:cursor-not-allowed disabled:opacity-60"
            >
              <FileText size={14} />
              {busyFormat === 'pdf'
                ? t('customers.statement.downloading')
                : t('customers.statement.pdf')}
            </button>
            <button
              type="button"
              onClick={() => triggerDownload('xlsx')}
              disabled={busyFormat !== null}
              className="inline-flex flex-1 items-center justify-center gap-2 rounded-md border border-slate-300 bg-white px-3 py-1.5 text-sm font-medium text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
            >
              <FileSpreadsheet size={14} />
              {busyFormat === 'xlsx'
                ? t('customers.statement.downloading')
                : t('customers.statement.xlsx')}
            </button>
          </div>
        </div>
      )}
    </div>
  );
};
