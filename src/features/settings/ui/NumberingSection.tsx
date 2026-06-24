import { useState } from 'react';
import type { TFunction } from 'i18next';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { toastApiError } from '@/shared/lib/mutationToast';
import {
  useConfigureDocumentSequence,
  useDocumentSequencesQuery,
} from '../hooks/useSettingsQueries';
import type { DocumentSequenceConfig, DocumentSequenceType } from '../model/settings.types';

const getTypeLabel = (t: TFunction, type: DocumentSequenceType): string => {
  const labels: Partial<Record<DocumentSequenceType, string>> = {
    CustomerCode: t('numbering.typeCustomerCode', { defaultValue: 'Müşteri Kodu' }),
    ProductSku: t('numbering.typeProductSku', { defaultValue: 'Ürün Kodu (SKU)' }),
    OrderNumber: t('numbering.typeOrderNumber', { defaultValue: 'Sipariş No' }),
    InvoiceNumber: t('numbering.typeInvoiceNumber', { defaultValue: 'Fatura No' }),
    CreditNoteNumber: t('numbering.typeCreditNoteNumber', { defaultValue: 'İade Faturası No' }),
    DebitNoteNumber: t('numbering.typeDebitNoteNumber', { defaultValue: 'Borç Dekontu No' }),
    PaymentNumber: t('numbering.typePaymentNumber', { defaultValue: 'Tahsilat/Ödeme No' }),
    ShipmentNumber: t('numbering.typeShipmentNumber', { defaultValue: 'Sevkiyat No' }),
    JournalNumber: t('numbering.typeJournalNumber', { defaultValue: 'Yevmiye Fişi No' }),
  };
  return labels[type] ?? type;
};

export const NumberingSection = () => {
  const { t } = useTranslation();
  const query = useDocumentSequencesQuery();
  const sequences = query.data?.data ?? [];

  return (
    <div className="space-y-3">
      <p className="text-xs text-slate-500 dark:text-slate-400">
        {t('numbering.formatHelpIntro', {
          defaultValue:
            'Belge numaralarının önekini, basamak sayısını ve formatını belirleyin. Format kullanılırsa',
        })}{' '}
        <code className="rounded bg-slate-100 px-1 dark:bg-slate-800">{'{P}'}</code>{' '}
        {t('numbering.formatHelpPrefix', { defaultValue: 'önek,' })}{' '}
        <code className="rounded bg-slate-100 px-1 dark:bg-slate-800">{'{Y}'}</code>{' '}
        {t('numbering.formatHelpYear', { defaultValue: 'yıl,' })}{' '}
        <code className="rounded bg-slate-100 px-1 dark:bg-slate-800">{'{N}'}</code>{' '}
        {t('numbering.formatHelpSequence', { defaultValue: 'sıra numarasıdır.' })}
      </p>

      <div className="overflow-hidden rounded-lg border border-slate-200 dark:border-slate-800">
        <table className="w-full text-sm">
          <thead className="bg-slate-50 text-[10px] font-semibold uppercase text-slate-600 dark:bg-slate-800/50 dark:text-slate-300">
            <tr>
              <th className="px-3 py-2 text-left">
                {t('numbering.document', { defaultValue: 'Belge' })}
              </th>
              <th className="px-3 py-2 text-left">
                {t('numbering.prefix', { defaultValue: 'Önek' })}
              </th>
              <th className="w-20 px-3 py-2 text-right">
                {t('numbering.pad', { defaultValue: 'Basamak' })}
              </th>
              <th className="px-3 py-2 text-left">
                {t('numbering.format', { defaultValue: 'Format' })}
              </th>
              <th className="w-24 px-3 py-2 text-right">
                {t('numbering.next', { defaultValue: 'Sıradaki' })}
              </th>
              <th className="px-3 py-2 text-left">
                {t('numbering.preview', { defaultValue: 'Önizleme' })}
              </th>
              <th className="px-3 py-2" />
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
            {sequences.map((seq: DocumentSequenceConfig) => (
              <SequenceRow key={seq.type} seq={seq} />
            ))}
            {sequences.length === 0 && !query.isPending && (
              <tr>
                <td colSpan={7} className="px-3 py-4 text-center text-xs text-slate-500">
                  {t('common.loading', { defaultValue: 'Yükleniyor…' })}
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
};

const SequenceRow = ({ seq }: { seq: DocumentSequenceConfig }) => {
  const { t } = useTranslation();
  const configure = useConfigureDocumentSequence();
  const [prefix, setPrefix] = useState(seq.prefix);
  const [padLength, setPadLength] = useState(String(seq.padLength));
  const [format, setFormat] = useState(seq.format ?? '');
  const [nextNumber, setNextNumber] = useState(String(seq.nextNumber));

  const dirty =
    prefix !== seq.prefix ||
    Number(padLength) !== seq.padLength ||
    (format || null) !== seq.format ||
    Number(nextNumber) !== seq.nextNumber;

  const save = async () => {
    if (!prefix.trim()) {
      toast.error(t('numbering.prefixRequired', { defaultValue: 'Önek zorunludur.' }));
      return;
    }
    try {
      await configure.mutateAsync({
        type: seq.type,
        prefix: prefix.trim(),
        padLength: Number(padLength) || 1,
        format: format.trim() || null,
        nextNumber: Number(nextNumber) || 1,
      });
      toast.success(t('numbering.saved', { defaultValue: 'Numaralandırma kaydedildi.' }));
    } catch (err) {
      toastApiError(err);
    }
  };

  const cellInput =
    'w-full rounded border border-slate-200 bg-white px-2 py-1 text-xs dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100';

  return (
    <tr className="hover:bg-slate-50/40 dark:hover:bg-slate-800/30">
      <td className="px-3 py-2">
        <div className="font-medium text-slate-800 dark:text-slate-100">
          {getTypeLabel(t, seq.type)}
        </div>
        {!seq.isConfigured && (
          <span className="text-[10px] text-warning-600 dark:text-warning-400">
            {t('numbering.notConfigured', { defaultValue: 'henüz ayarlanmadı' })}
          </span>
        )}
      </td>
      <td className="px-3 py-2">
        <input
          value={prefix}
          onChange={(e) => setPrefix(e.target.value)}
          maxLength={20}
          className={`${cellInput} font-mono`}
        />
      </td>
      <td className="px-3 py-2">
        <input
          type="number"
          min={1}
          max={12}
          value={padLength}
          onChange={(e) => setPadLength(e.target.value)}
          className={`${cellInput} text-right`}
        />
      </td>
      <td className="px-3 py-2">
        <input
          value={format}
          onChange={(e) => setFormat(e.target.value)}
          maxLength={60}
          placeholder="{P}-{Y}-{N}"
          className={`${cellInput} font-mono`}
        />
      </td>
      <td className="px-3 py-2">
        <input
          type="number"
          min={1}
          value={nextNumber}
          onChange={(e) => setNextNumber(e.target.value)}
          className={`${cellInput} text-right`}
        />
      </td>
      <td className="px-3 py-2 font-mono text-xs text-slate-600 dark:text-slate-300">
        {seq.preview}
      </td>
      <td className="px-3 py-2 text-right">
        <button
          type="button"
          onClick={save}
          disabled={!dirty || configure.isPending}
          className="rounded bg-primary-600 px-2.5 py-1 text-[11px] font-semibold text-white hover:bg-primary-700 disabled:opacity-40"
        >
          {t('common.save', { defaultValue: 'Kaydet' })}
        </button>
      </td>
    </tr>
  );
};
