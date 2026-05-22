import { useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Info } from 'lucide-react';
import { toastApiError } from '@/shared/lib/mutationToast';
import { formatCurrency, formatNumber } from '@/shared/lib/format';
import {
  DECIMAL_PLACES_KEY,
  DEFAULT_DECIMAL_PLACES,
  NUMBER_FORMAT_CATEGORY,
  useParametersQuery,
  useUpsertParameters,
} from '../hooks/useSettingsQueries';

const OPTIONS = [0, 1, 2, 3, 4, 5, 6];
const SAMPLE = 1234567.891234;

export const NumberFormatSection = () => {
  const { i18n } = useTranslation();
  const params = useParametersQuery(NUMBER_FORMAT_CATEGORY);
  const upsert = useUpsertParameters();

  const stored = params.data?.data?.find((s) => s.key === DECIMAL_PLACES_KEY)?.value;
  const [decimals, setDecimals] = useState<number>(DEFAULT_DECIMAL_PLACES);

  // Seed the editable value from the server setting once it loads (adjust during
  // render rather than in an effect to avoid a cascading re-render).
  const seededFrom = useRef<string | null | undefined>(undefined);
  if (stored !== seededFrom.current) {
    seededFrom.current = stored;
    if (stored) {
      const n = Number.parseInt(stored, 10);
      if (!Number.isNaN(n)) setDecimals(Math.min(Math.max(n, 0), 6));
    }
  }

  const save = () => {
    upsert.mutate(
      [
        {
          category: NUMBER_FORMAT_CATEGORY,
          key: DECIMAL_PLACES_KEY,
          value: String(decimals),
          dataType: 'int',
          description: 'Tutar ve miktarların görüntülenmesinde kullanılan ondalık basamak sayısı.',
        },
      ],
      {
        onSuccess: () => toast.success('Sayı biçimi kaydedildi.'),
        onError: (err) => toastApiError(err),
      },
    );
  };

  const locale = i18n.language;

  return (
    <div className="max-w-xl space-y-4">
      <div>
        <h2 className="text-sm font-semibold text-slate-900 dark:text-slate-100">Sayı Biçimi</h2>
        <p className="mt-0.5 text-xs text-slate-500 dark:text-slate-400">
          Tutar ve miktarların ekranda kaç ondalık basamakla gösterileceğini belirleyin.
        </p>
      </div>

      <div>
        <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
          Ondalık Basamak Sayısı
        </label>
        <select
          value={decimals}
          onChange={(e) => setDecimals(Number(e.target.value))}
          className="mt-1 w-32 rounded border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800"
        >
          {OPTIONS.map((n) => (
            <option key={n} value={n}>
              {n}
            </option>
          ))}
        </select>
      </div>

      <div className="rounded border border-slate-200 bg-slate-50 p-3 text-sm dark:border-slate-700 dark:bg-slate-800/50">
        <div className="text-[11px] uppercase tracking-wider text-slate-500 dark:text-slate-400">
          Önizleme
        </div>
        <div className="mt-1 space-y-0.5 text-slate-800 dark:text-slate-200">
          <div>{formatNumber(SAMPLE, locale, decimals)}</div>
          <div>{formatCurrency(SAMPLE, locale, 'TRY', decimals)}</div>
        </div>
      </div>

      <div className="flex items-start gap-2 rounded border border-amber-200 bg-amber-50 p-3 text-xs text-amber-800 dark:border-amber-500/30 dark:bg-amber-500/10 dark:text-amber-300">
        <Info size={14} className="mt-0.5 shrink-0" />
        <span>
          Bu ayar yalnızca görüntülemeyi etkiler. Kayıtlı tutar, miktar ve sayısal değerler tam
          hassasiyetle saklanır; basamak sayısını değiştirmek mevcut sipariş ve faturaların
          verilerini değiştirmez.
        </span>
      </div>

      <button
        type="button"
        onClick={save}
        disabled={upsert.isPending}
        className="rounded bg-indigo-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-indigo-700 disabled:opacity-50"
      >
        {upsert.isPending ? 'Kaydediliyor…' : 'Kaydet'}
      </button>
    </div>
  );
};
