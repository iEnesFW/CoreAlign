import { Database } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useConfirm } from '@/shared/ui/ConfirmDialog/useConfirm';
import {
  usePaymentTermsQuery,
  useSeedStandardUoms,
  useTaxRatesQuery,
  useUomsQuery,
} from '@/shared/master-data/hooks/useMasterData';

const Badge = ({ active }: { active: boolean }) => {
  const { t } = useTranslation();
  return (
    <span
      className={`rounded px-1.5 py-0.5 text-[10px] font-semibold ${
        active
          ? 'bg-success-100 text-success-700 dark:bg-success-500/20 dark:text-success-300'
          : 'bg-slate-200 text-slate-600 dark:bg-slate-700 dark:text-slate-300'
      }`}
    >
      {active
        ? t('MasterData.StatusActive', { defaultValue: 'Aktif' })
        : t('MasterData.StatusInactive', { defaultValue: 'Pasif' })}
    </span>
  );
};

export const MasterDataSection = () => {
  const { t } = useTranslation();
  const confirm = useConfirm();
  const taxRates = useTaxRatesQuery();
  const paymentTerms = usePaymentTermsQuery();
  const uoms = useUomsQuery();
  const seedUoms = useSeedStandardUoms();

  const seedStandardUoms = async () => {
    const ok = await confirm({
      title: t('MasterData.SeedUomsTitle', { defaultValue: 'Standart Birimleri Yükle' }),
      message: t('MasterData.SeedUomsMessage', {
        defaultValue:
          'Metre, kilogram, litre, adet gibi tüm standart ölçü birimleri eklenecek. Mevcut birimler korunur. Devam edilsin mi?',
      }),
      confirmLabel: t('MasterData.SeedUomsConfirm', { defaultValue: 'Yükle' }),
    });
    if (!ok) return;
    try {
      const result = await seedUoms.mutateAsync();
      toast.success(
        t('MasterData.SeedUomsSuccess', {
          count: result.data ?? 0,
          defaultValue: '{{count}} ölçü birimi eklendi.',
        }),
      );
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <div className="space-y-6">
      <p className="text-xs text-slate-500 dark:text-slate-400">
        {t('MasterData.Intro', {
          defaultValue:
            'Vergi oranları, ödeme vadeleri ve ölçü birimleri. Bu kayıtlar fatura, sipariş ve fiyat hesaplamalarında kullanılır.',
        })}
      </p>

      <section>
        <h3 className="mb-2 text-sm font-semibold text-slate-900 dark:text-slate-100">
          {t('MasterData.TaxRatesHeading', { defaultValue: 'Vergi Oranları (KDV / Tevkifat)' })}
        </h3>
        <div className="overflow-hidden rounded-lg border border-slate-200 dark:border-slate-800">
          <table className="w-full text-sm">
            <thead className="bg-slate-50 text-[10px] font-semibold uppercase text-slate-600 dark:bg-slate-800/50 dark:text-slate-300">
              <tr>
                <th className="px-3 py-2 text-left">
                  {t('MasterData.ColumnCode', { defaultValue: 'Kod' })}
                </th>
                <th className="px-3 py-2 text-left">
                  {t('MasterData.ColumnName', { defaultValue: 'İsim' })}
                </th>
                <th className="px-3 py-2 text-right">
                  {t('MasterData.ColumnRatePercent', { defaultValue: 'Oran %' })}
                </th>
                <th className="px-3 py-2 text-center">
                  {t('MasterData.ColumnWithholding', { defaultValue: 'Tevkifat' })}
                </th>
                <th className="px-3 py-2 text-center">
                  {t('MasterData.ColumnStatus', { defaultValue: 'Durum' })}
                </th>
              </tr>
            </thead>
            <tbody>
              {(taxRates.data?.data ?? []).map((r) => (
                <tr key={r.id} className="border-t border-slate-100 dark:border-slate-800">
                  <td className="px-3 py-2 font-mono text-xs">{r.code}</td>
                  <td className="px-3 py-2 text-xs">{r.name}</td>
                  <td className="px-3 py-2 text-right font-mono text-xs">{r.ratePercent}</td>
                  <td className="px-3 py-2 text-center text-xs">
                    {r.isWithholding ? t('MasterData.Yes', { defaultValue: 'Evet' }) : '—'}
                  </td>
                  <td className="px-3 py-2 text-center">
                    <Badge active={r.isActive} />
                  </td>
                </tr>
              ))}
              {(taxRates.data?.data ?? []).length === 0 && !taxRates.isPending && (
                <tr>
                  <td colSpan={5} className="px-3 py-4 text-center text-xs text-slate-500">
                    {t('MasterData.TaxRatesEmpty', {
                      defaultValue: 'Henüz vergi oranı tanımlanmadı.',
                    })}
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </section>

      <section>
        <h3 className="mb-2 text-sm font-semibold text-slate-900 dark:text-slate-100">
          {t('MasterData.PaymentTermsHeading', { defaultValue: 'Ödeme Vadeleri' })}
        </h3>
        <div className="overflow-hidden rounded-lg border border-slate-200 dark:border-slate-800">
          <table className="w-full text-sm">
            <thead className="bg-slate-50 text-[10px] font-semibold uppercase text-slate-600 dark:bg-slate-800/50 dark:text-slate-300">
              <tr>
                <th className="px-3 py-2 text-left">
                  {t('MasterData.ColumnCode', { defaultValue: 'Kod' })}
                </th>
                <th className="px-3 py-2 text-left">
                  {t('MasterData.ColumnName', { defaultValue: 'İsim' })}
                </th>
                <th className="px-3 py-2 text-right">
                  {t('MasterData.ColumnNetDays', { defaultValue: 'Net Gün' })}
                </th>
                <th className="px-3 py-2 text-center">
                  {t('MasterData.ColumnEndOfMonth', { defaultValue: 'Ay Sonu' })}
                </th>
                <th className="px-3 py-2 text-center">
                  {t('MasterData.ColumnStatus', { defaultValue: 'Durum' })}
                </th>
              </tr>
            </thead>
            <tbody>
              {(paymentTerms.data?.data ?? []).map((p) => (
                <tr key={p.id} className="border-t border-slate-100 dark:border-slate-800">
                  <td className="px-3 py-2 font-mono text-xs">{p.code}</td>
                  <td className="px-3 py-2 text-xs">{p.name}</td>
                  <td className="px-3 py-2 text-right font-mono text-xs">{p.netDays}</td>
                  <td className="px-3 py-2 text-center text-xs">
                    {p.endOfMonth ? t('MasterData.Yes', { defaultValue: 'Evet' }) : '—'}
                  </td>
                  <td className="px-3 py-2 text-center">
                    <Badge active={p.isActive} />
                  </td>
                </tr>
              ))}
              {(paymentTerms.data?.data ?? []).length === 0 && !paymentTerms.isPending && (
                <tr>
                  <td colSpan={5} className="px-3 py-4 text-center text-xs text-slate-500">
                    {t('MasterData.PaymentTermsEmpty', {
                      defaultValue: 'Henüz ödeme vadesi tanımlanmadı.',
                    })}
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </section>

      <section>
        <div className="mb-2 flex items-center justify-between gap-2">
          <h3 className="text-sm font-semibold text-slate-900 dark:text-slate-100">
            {t('MasterData.UomsHeading', { defaultValue: 'Ölçü Birimleri' })}
          </h3>
          <button
            type="button"
            onClick={seedStandardUoms}
            disabled={seedUoms.isPending}
            className="inline-flex items-center gap-1.5 rounded border border-primary-200 bg-primary-50 px-2.5 py-1.5 text-xs font-semibold text-primary-700 hover:bg-primary-100 disabled:opacity-50 dark:border-primary-500/30 dark:bg-primary-500/10 dark:text-primary-300 dark:hover:bg-primary-500/20"
          >
            <Database size={12} />
            {t('MasterData.SeedUomsButton', { defaultValue: 'Standart birimleri yükle' })}
          </button>
        </div>
        <div className="overflow-hidden rounded-lg border border-slate-200 dark:border-slate-800">
          <table className="w-full text-sm">
            <thead className="bg-slate-50 text-[10px] font-semibold uppercase text-slate-600 dark:bg-slate-800/50 dark:text-slate-300">
              <tr>
                <th className="px-3 py-2 text-left">
                  {t('MasterData.ColumnCode', { defaultValue: 'Kod' })}
                </th>
                <th className="px-3 py-2 text-left">
                  {t('MasterData.ColumnName', { defaultValue: 'İsim' })}
                </th>
                <th className="px-3 py-2 text-left">
                  {t('MasterData.ColumnSymbol', { defaultValue: 'Sembol' })}
                </th>
                <th className="px-3 py-2 text-center">
                  {t('MasterData.ColumnBaseUnit', { defaultValue: 'Ana Birim' })}
                </th>
                <th className="px-3 py-2 text-center">
                  {t('MasterData.ColumnStatus', { defaultValue: 'Durum' })}
                </th>
              </tr>
            </thead>
            <tbody>
              {(uoms.data?.data ?? []).map((u) => (
                <tr key={u.id} className="border-t border-slate-100 dark:border-slate-800">
                  <td className="px-3 py-2 font-mono text-xs">{u.code}</td>
                  <td className="px-3 py-2 text-xs">{u.name}</td>
                  <td className="px-3 py-2 text-xs">{u.symbol ?? '—'}</td>
                  <td className="px-3 py-2 text-center text-xs">
                    {u.isBase ? t('MasterData.Yes', { defaultValue: 'Evet' }) : '—'}
                  </td>
                  <td className="px-3 py-2 text-center">
                    <Badge active={u.isActive} />
                  </td>
                </tr>
              ))}
              {(uoms.data?.data ?? []).length === 0 && !uoms.isPending && (
                <tr>
                  <td colSpan={5} className="px-3 py-4 text-center text-xs text-slate-500">
                    {t('MasterData.UomsEmpty', { defaultValue: 'Henüz ölçü birimi tanımlanmadı.' })}
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </section>
    </div>
  );
};
