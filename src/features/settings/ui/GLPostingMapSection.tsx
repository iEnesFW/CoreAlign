import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { CheckCircle2, AlertTriangle } from 'lucide-react';
import { toastApiError } from '@/shared/lib/mutationToast';
import {
  useConfigureGLPostingMapping,
  useGLPostingMapQuery,
} from '@/features/accounting/hooks/useGLPostingMap';
import { AccountPicker } from '@/features/accounting/ui/AccountPicker';
import type {
  GLPostingKey,
  GLPostingMapping,
} from '@/features/accounting/model/glPostingMap.types';

const KEY_DEFAULTS: Record<GLPostingKey, string> = {
  AccountsReceivable: 'Alıcılar (AR)',
  SalesRevenue: 'Satış Geliri',
  OutputVat: 'Hesaplanan KDV',
  Cash: 'Kasa',
  Bank: 'Banka',
  AccountsPayable: 'Satıcılar (AP)',
  InputVat: 'İndirilecek KDV',
  Inventory: 'Stok',
  CostOfGoodsSold: 'Satılan Malın Maliyeti (SMM)',
  GoodsReceiptClearing: 'Mal Alım Tahakkuk (GR/IR)',
  PurchaseExpense: 'Alım Gideri',
  InventoryWriteOff: 'Fire/Zayi Gideri',
};

export const GLPostingMapSection = () => {
  const { t } = useTranslation();
  const query = useGLPostingMapQuery();
  const rows = query.data?.data ?? [];

  return (
    <div className="space-y-3">
      <p className="text-xs text-slate-500 dark:text-slate-400">
        {t('Accounting.glPostingMap.help', {
          defaultValue:
            'Otomatik muhasebe fişleri (satış, tahsilat, tedarikçi faturası/ödemesi, mal kabul, SMM) oluşturulurken her rolün hangi muhasebe hesabına işleneceğini buradan belirleyin. Boş bırakılan satırlar varsayılan TDHP koduyla çalışır; eşleşen hesap bulunamazsa o fiş sessizce atlanır ve ana işlem akışı bozulmaz.',
        })}
      </p>

      <div className="overflow-x-auto rounded-lg border border-slate-200 dark:border-slate-800">
        <table className="w-full text-sm">
          <thead className="bg-slate-50 text-[10px] font-semibold uppercase text-slate-600 dark:bg-slate-800/50 dark:text-slate-300">
            <tr>
              <th className="px-3 py-2 text-left">
                {t('Accounting.glPostingMap.columns.role', { defaultValue: 'Rol' })}
              </th>
              <th className="w-28 px-3 py-2 text-left">
                {t('Accounting.glPostingMap.columns.default', { defaultValue: 'Varsayılan' })}
              </th>
              <th className="w-32 px-3 py-2 text-left">
                {t('Accounting.glPostingMap.columns.accountCode', { defaultValue: 'Hesap Kodu' })}
              </th>
              <th className="px-3 py-2 text-left">
                {t('Accounting.glPostingMap.columns.account', { defaultValue: 'Hesap' })}
              </th>
              <th className="w-20 px-3 py-2 text-center">
                {t('Accounting.glPostingMap.columns.status', { defaultValue: 'Durum' })}
              </th>
              <th className="px-3 py-2" />
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
            {rows.map((row) => (
              <MapRow key={row.key} row={row} />
            ))}
            {rows.length === 0 && (
              <tr>
                <td
                  colSpan={6}
                  className={`px-3 py-4 text-center text-xs ${query.isError ? 'text-danger-600 dark:text-danger-400' : 'text-slate-500'}`}
                >
                  {query.isPending
                    ? t('Accounting.glPostingMap.loading', { defaultValue: 'Yükleniyor…' })
                    : query.isError
                      ? t('Accounting.glPostingMap.loadError', {
                          defaultValue: 'Eşleştirmeler yüklenemedi.',
                        })
                      : t('Accounting.glPostingMap.empty', {
                          defaultValue: 'Eşleştirme bulunamadı.',
                        })}
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
};

const MapRow = ({ row }: { row: GLPostingMapping }) => {
  const { t } = useTranslation();
  const configure = useConfigureGLPostingMapping();
  const [code, setCode] = useState(row.effectiveCode);

  const dirty = code.trim() !== (row.effectiveCode ?? '');

  const save = async () => {
    if (!code.trim()) {
      toast.error(
        t('Accounting.glPostingMap.codeRequired', { defaultValue: 'Hesap kodu zorunludur.' }),
      );
      return;
    }
    try {
      await configure.mutateAsync({ key: row.postingKey, accountCode: code.trim() });
      toast.success(
        t('Accounting.glPostingMap.saved', { defaultValue: 'Hesap eşleştirmesi kaydedildi.' }),
      );
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <tr className="hover:bg-slate-50/40 dark:hover:bg-slate-800/30">
      <td className="px-3 py-2">
        <div className="font-medium text-slate-800 dark:text-slate-100">
          {t(`Accounting.glPostingMap.roles.${row.postingKey}`, {
            defaultValue: KEY_DEFAULTS[row.postingKey] ?? row.key,
          })}
        </div>
        {row.overrideCode && (
          <span className="text-[10px] text-primary-600 dark:text-primary-400">
            {t('Accounting.glPostingMap.customized', { defaultValue: 'özelleştirildi' })}
          </span>
        )}
      </td>
      <td className="px-3 py-2 font-mono text-xs text-slate-500 dark:text-slate-400">
        {row.defaultCode ?? '—'}
      </td>
      <td className="px-3 py-2">
        <AccountPicker value={code} onChange={setCode} />
      </td>
      <td className="px-3 py-2 text-xs text-slate-600 dark:text-slate-300">
        {row.accountName ?? <span className="text-slate-400">—</span>}
      </td>
      <td className="px-3 py-2 text-center">
        {row.resolves ? (
          <CheckCircle2
            size={15}
            className="mx-auto text-success-500"
            aria-label={t('Accounting.glPostingMap.resolved', { defaultValue: 'çözümlendi' })}
          />
        ) : (
          <AlertTriangle
            size={15}
            className="mx-auto text-warning-500"
            aria-label={t('Accounting.glPostingMap.unresolved', {
              defaultValue: 'hesap bulunamadı',
            })}
          />
        )}
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
