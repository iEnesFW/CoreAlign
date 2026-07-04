import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ArrowRightLeft } from 'lucide-react';
import { toast } from 'sonner';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useProcessIncomingInvoice } from '../hooks/useIncomingInvoiceQueries';
import type { IncomingInvoiceDto } from '../model/incomingInvoice.types';

interface Props {
  invoice: IncomingInvoiceDto;
  onClose: () => void;
  onProcessed?: () => void;
}

const CURRENCIES = ['TRY', 'USD', 'EUR'];

const fieldCls =
  'w-full rounded-md border border-slate-300 bg-white px-2.5 py-1.5 text-sm text-slate-800 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100';

export const ProcessIncomingInvoiceModal = ({ invoice, onClose, onProcessed }: Props) => {
  const { t } = useTranslation();
  const processMutation = useProcessIncomingInvoice();

  const [subtotal, setSubtotal] = useState(0);
  const [taxAmount, setTaxAmount] = useState(0);
  const [vendorName, setVendorName] = useState(invoice.senderName ?? '');
  const [currency, setCurrency] = useState('TRY');

  const total = useMemo(() => subtotal + taxAmount, [subtotal, taxAmount]);
  const pending = processMutation.isPending;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (subtotal <= 0) {
      toast.error(
        t('incomingInvoices.process.subtotalRequired', {
          defaultValue: 'Ara toplam sıfırdan büyük olmalı.',
        }),
      );
      return;
    }
    processMutation.mutate(
      {
        id: invoice.id,
        input: {
          subtotal,
          taxAmount,
          vendorName: vendorName.trim() || null,
          currency,
        },
      },
      {
        onSuccess: (response) => {
          if (!response.isSuccess) {
            toast.error(response.errors[0] ?? t('auth.common.unexpectedError'));
            return;
          }
          toast.success(
            t('incomingInvoices.process.success', {
              defaultValue: 'Fatura sisteme işlendi.',
            }),
          );
          if (response.data?.vendorBillId) {
            toast.success(
              t('incomingInvoices.process.vendorCreated', {
                defaultValue: 'Tedarikçi Faturası oluşturuldu.',
              }),
            );
          }
          onProcessed?.();
          onClose();
        },
        onError: (err: unknown) => toastApiError(err),
      },
    );
  };

  return (
    <Modal
      open
      title={t('incomingInvoices.process.title', { defaultValue: 'Faturayı Sisteme İşle' })}
      subtitle={`${invoice.invoiceNumber} · ${invoice.senderName ?? invoice.senderVkn}`}
      icon={<ArrowRightLeft size={18} />}
      onClose={onClose}
      size="md"
      footer={
        <div className="flex justify-end gap-2">
          <Button variant="ghost" onClick={onClose} type="button">
            {t('common.cancel', { defaultValue: 'İptal' })}
          </Button>
          <Button type="submit" form="process-incoming-invoice-form" isLoading={pending}>
            {t('incomingInvoices.process.submit', { defaultValue: 'Sisteme İşle' })}
          </Button>
        </div>
      }
    >
      <form id="process-incoming-invoice-form" onSubmit={handleSubmit} className="space-y-4">
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <label className="flex flex-col gap-1 text-xs font-medium text-slate-600 dark:text-slate-300">
            <span>
              {t('incomingInvoices.process.subtotal', { defaultValue: 'Ara toplam' })}
              <span className="text-danger-500"> *</span>
            </span>
            <input
              type="number"
              min={0}
              step={0.01}
              value={Number.isFinite(subtotal) ? subtotal : 0}
              onChange={(e) => setSubtotal(Number.parseFloat(e.target.value) || 0)}
              className={fieldCls}
              required
            />
          </label>
          <label className="flex flex-col gap-1 text-xs font-medium text-slate-600 dark:text-slate-300">
            <span>{t('incomingInvoices.process.taxAmount', { defaultValue: 'KDV tutarı' })}</span>
            <input
              type="number"
              min={0}
              step={0.01}
              value={Number.isFinite(taxAmount) ? taxAmount : 0}
              onChange={(e) => setTaxAmount(Number.parseFloat(e.target.value) || 0)}
              className={fieldCls}
            />
          </label>
          <label className="flex flex-col gap-1 text-xs font-medium text-slate-600 dark:text-slate-300">
            <span>
              {t('incomingInvoices.process.vendorName', { defaultValue: 'Tedarikçi adı' })}
            </span>
            <input
              type="text"
              value={vendorName}
              maxLength={200}
              placeholder={t('incomingInvoices.process.vendorNamePlaceholder', {
                defaultValue: 'Boş bırakılırsa gönderen adı kullanılır',
              })}
              onChange={(e) => setVendorName(e.target.value)}
              className={fieldCls}
            />
          </label>
          <label className="flex flex-col gap-1 text-xs font-medium text-slate-600 dark:text-slate-300">
            <span>{t('incomingInvoices.process.currency', { defaultValue: 'Para birimi' })}</span>
            <select
              value={currency}
              onChange={(e) => setCurrency(e.target.value)}
              className={fieldCls}
            >
              {CURRENCIES.map((c) => (
                <option key={c} value={c}>
                  {c}
                </option>
              ))}
            </select>
          </label>
        </div>

        <div className="flex justify-end border-t border-slate-200 pt-2 text-xs text-slate-700 dark:border-slate-800 dark:text-slate-200">
          <span>
            {t('incomingInvoices.process.total', { defaultValue: 'Genel toplam' })}:{' '}
            <strong className="tabular-nums">
              {total.toFixed(2)} {currency}
            </strong>
          </span>
        </div>
      </form>
    </Modal>
  );
};
