import { Database } from 'lucide-react';
import { toast } from 'sonner';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useConfirm } from '@/shared/ui/ConfirmDialog/useConfirm';
import {
  usePaymentTermsQuery,
  useSeedStandardUoms,
  useTaxRatesQuery,
  useUomsQuery,
} from '@/features/master-data/hooks/useMasterData';

const Badge = ({ active }: { active: boolean }) => (
  <span
    className={`rounded px-1.5 py-0.5 text-[10px] font-semibold ${
      active
        ? 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/20 dark:text-emerald-300'
        : 'bg-slate-200 text-slate-600 dark:bg-slate-700 dark:text-slate-300'
    }`}
  >
    {active ? 'Aktif' : 'Pasif'}
  </span>
);

export const MasterDataSection = () => {
  const confirm = useConfirm();
  const taxRates = useTaxRatesQuery();
  const paymentTerms = usePaymentTermsQuery();
  const uoms = useUomsQuery();
  const seedUoms = useSeedStandardUoms();

  const seedStandardUoms = async () => {
    const ok = await confirm({
      title: 'Standart Birimleri Yükle',
      message:
        'Metre, kilogram, litre, adet gibi tüm standart ölçü birimleri eklenecek. Mevcut birimler korunur. Devam edilsin mi?',
      confirmLabel: 'Yükle',
    });
    if (!ok) return;
    try {
      const result = await seedUoms.mutateAsync();
      toast.success(`${result.data ?? 0} ölçü birimi eklendi.`);
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <div className="space-y-6">
      <p className="text-xs text-slate-500 dark:text-slate-400">
        Vergi oranları, ödeme vadeleri ve ölçü birimleri. Bu kayıtlar fatura, sipariş ve fiyat
        hesaplamalarında kullanılır.
      </p>

      <section>
        <h3 className="mb-2 text-sm font-semibold text-slate-900 dark:text-slate-100">
          Vergi Oranları (KDV / Tevkifat)
        </h3>
        <div className="overflow-hidden rounded-lg border border-slate-200 dark:border-slate-800">
          <table className="w-full text-sm">
            <thead className="bg-slate-50 text-[10px] font-semibold uppercase text-slate-600 dark:bg-slate-800/50 dark:text-slate-300">
              <tr>
                <th className="px-3 py-2 text-left">Kod</th>
                <th className="px-3 py-2 text-left">İsim</th>
                <th className="px-3 py-2 text-right">Oran %</th>
                <th className="px-3 py-2 text-center">Tevkifat</th>
                <th className="px-3 py-2 text-center">Durum</th>
              </tr>
            </thead>
            <tbody>
              {(taxRates.data?.data ?? []).map((r) => (
                <tr key={r.id} className="border-t border-slate-100 dark:border-slate-800">
                  <td className="px-3 py-2 font-mono text-xs">{r.code}</td>
                  <td className="px-3 py-2 text-xs">{r.name}</td>
                  <td className="px-3 py-2 text-right font-mono text-xs">{r.ratePercent}</td>
                  <td className="px-3 py-2 text-center text-xs">
                    {r.isWithholding ? 'Evet' : '—'}
                  </td>
                  <td className="px-3 py-2 text-center">
                    <Badge active={r.isActive} />
                  </td>
                </tr>
              ))}
              {(taxRates.data?.data ?? []).length === 0 && !taxRates.isPending && (
                <tr>
                  <td colSpan={5} className="px-3 py-4 text-center text-xs text-slate-500">
                    Henüz vergi oranı tanımlanmadı.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </section>

      <section>
        <h3 className="mb-2 text-sm font-semibold text-slate-900 dark:text-slate-100">
          Ödeme Vadeleri
        </h3>
        <div className="overflow-hidden rounded-lg border border-slate-200 dark:border-slate-800">
          <table className="w-full text-sm">
            <thead className="bg-slate-50 text-[10px] font-semibold uppercase text-slate-600 dark:bg-slate-800/50 dark:text-slate-300">
              <tr>
                <th className="px-3 py-2 text-left">Kod</th>
                <th className="px-3 py-2 text-left">İsim</th>
                <th className="px-3 py-2 text-right">Net Gün</th>
                <th className="px-3 py-2 text-center">Ay Sonu</th>
                <th className="px-3 py-2 text-center">Durum</th>
              </tr>
            </thead>
            <tbody>
              {(paymentTerms.data?.data ?? []).map((p) => (
                <tr key={p.id} className="border-t border-slate-100 dark:border-slate-800">
                  <td className="px-3 py-2 font-mono text-xs">{p.code}</td>
                  <td className="px-3 py-2 text-xs">{p.name}</td>
                  <td className="px-3 py-2 text-right font-mono text-xs">{p.netDays}</td>
                  <td className="px-3 py-2 text-center text-xs">{p.endOfMonth ? 'Evet' : '—'}</td>
                  <td className="px-3 py-2 text-center">
                    <Badge active={p.isActive} />
                  </td>
                </tr>
              ))}
              {(paymentTerms.data?.data ?? []).length === 0 && !paymentTerms.isPending && (
                <tr>
                  <td colSpan={5} className="px-3 py-4 text-center text-xs text-slate-500">
                    Henüz ödeme vadesi tanımlanmadı.
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
            Ölçü Birimleri
          </h3>
          <button
            type="button"
            onClick={seedStandardUoms}
            disabled={seedUoms.isPending}
            className="inline-flex items-center gap-1.5 rounded border border-indigo-200 bg-indigo-50 px-2.5 py-1.5 text-xs font-semibold text-indigo-700 hover:bg-indigo-100 disabled:opacity-50 dark:border-indigo-500/30 dark:bg-indigo-500/10 dark:text-indigo-300 dark:hover:bg-indigo-500/20"
          >
            <Database size={12} />
            Standart birimleri yükle
          </button>
        </div>
        <div className="overflow-hidden rounded-lg border border-slate-200 dark:border-slate-800">
          <table className="w-full text-sm">
            <thead className="bg-slate-50 text-[10px] font-semibold uppercase text-slate-600 dark:bg-slate-800/50 dark:text-slate-300">
              <tr>
                <th className="px-3 py-2 text-left">Kod</th>
                <th className="px-3 py-2 text-left">İsim</th>
                <th className="px-3 py-2 text-left">Sembol</th>
                <th className="px-3 py-2 text-center">Ana Birim</th>
                <th className="px-3 py-2 text-center">Durum</th>
              </tr>
            </thead>
            <tbody>
              {(uoms.data?.data ?? []).map((u) => (
                <tr key={u.id} className="border-t border-slate-100 dark:border-slate-800">
                  <td className="px-3 py-2 font-mono text-xs">{u.code}</td>
                  <td className="px-3 py-2 text-xs">{u.name}</td>
                  <td className="px-3 py-2 text-xs">{u.symbol ?? '—'}</td>
                  <td className="px-3 py-2 text-center text-xs">{u.isBase ? 'Evet' : '—'}</td>
                  <td className="px-3 py-2 text-center">
                    <Badge active={u.isActive} />
                  </td>
                </tr>
              ))}
              {(uoms.data?.data ?? []).length === 0 && !uoms.isPending && (
                <tr>
                  <td colSpan={5} className="px-3 py-4 text-center text-xs text-slate-500">
                    Henüz ölçü birimi tanımlanmadı.
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
