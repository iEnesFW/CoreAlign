import type { FormEvent } from 'react';
import { useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { ChevronLeft } from 'lucide-react';
import { useInitiatePayment } from '@/features/customer-portal/hooks/useCustomerPortalQueries';

export const InitiatePaymentPage = () => {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const initialInvoiceId = searchParams.get('invoiceId') ?? '';
  const initiate = useInitiatePayment();

  const [invoiceId, setInvoiceId] = useState(initialInvoiceId);
  const [error, setError] = useState<string | null>(null);

  const onSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError(null);
    if (!invoiceId.trim()) {
      setError(t('CustomerPortal.Payment.InvoiceIdRequired'));
      return;
    }
    try {
      const result = await initiate.mutateAsync({ invoiceId: invoiceId.trim() });
      const redirectUrl = result?.data?.redirectUrl;
      if (redirectUrl) {
        window.location.href = redirectUrl;
      } else {
        navigate('/customer-portal/payments');
      }
    } catch {
      setError(t('CustomerPortal.Payment.InitiateError'));
    }
  };

  return (
    <div className="space-y-4 max-w-xl">
      <Link
        to="/customer-portal/payments"
        className="inline-flex items-center gap-1 text-sm text-blue-600 hover:underline"
      >
        <ChevronLeft size={16} /> {t('CustomerPortal.Common.Back')}
      </Link>

      <h1 className="text-xl font-semibold">{t('CustomerPortal.Payment.InitiateTitle')}</h1>

      <form
        onSubmit={onSubmit}
        className="rounded-lg border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-4 sm:p-6 space-y-4"
      >
        <div>
          <label className="block text-xs text-slate-500 mb-1">
            {t('CustomerPortal.Payment.FieldInvoiceId')}
          </label>
          <input
            type="text"
            value={invoiceId}
            onChange={(e) => setInvoiceId(e.target.value)}
            required
            className="w-full px-3 py-2 rounded-md border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-950 text-sm font-mono"
          />
          <p className="text-xs text-slate-500 mt-1">{t('CustomerPortal.Payment.InvoiceIdHint')}</p>
        </div>
        {error ? <div className="text-sm text-red-600">{error}</div> : null}
        <div className="flex items-center justify-end gap-2">
          <Link
            to="/customer-portal/payments"
            className="px-3 py-2 rounded-md text-sm border border-slate-300 dark:border-slate-700"
          >
            {t('CustomerPortal.Common.Cancel')}
          </Link>
          <button
            type="submit"
            disabled={initiate.isPending}
            className="px-3 py-2 rounded-md text-sm bg-blue-600 text-white hover:bg-blue-700 disabled:opacity-60"
          >
            {initiate.isPending
              ? t('CustomerPortal.Common.Submitting')
              : t('CustomerPortal.Payment.PayNow')}
          </button>
        </div>
      </form>
    </div>
  );
};

export default InitiatePaymentPage;
