import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Hash } from 'lucide-react';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { ListPageTemplate } from '@/shared/ui/PageTemplate/PageTemplate';
import { useDocumentNumberGapsQuery } from '@/features/reports/hooks/useReportQueries';

const MISSING_PREVIEW = 20;

export const DocumentNumberGapPage = () => {
  const { t } = useTranslation();
  const [year, setYear] = useState(() => new Date().getFullYear());

  const query = useDocumentNumberGapsQuery({ year });
  const data = query.data?.data;
  const rows = data?.rows ?? [];

  return (
    <ListPageTemplate
      header={
        <PageHeader
          icon={<Hash size={20} />}
          title={t('DocumentNumberGap.title', { defaultValue: 'Belge Numarası Boşlukları' })}
          subtitle={t('DocumentNumberGap.subtitle', {
            defaultValue: 'Sıra numaralı belgelerde atlanan/eksik numaraları tespit et.',
          })}
        />
      }
      toolbar={
        <div className="flex flex-wrap items-center gap-3">
          <label className="flex items-center gap-2 text-xs text-slate-600 dark:text-slate-300">
            {t('DocumentNumberGap.year', { defaultValue: 'Yıl' })}
            <input
              type="number"
              value={year}
              min={2000}
              max={2100}
              onChange={(e) => setYear(Number(e.target.value) || year)}
              className="w-24 rounded-md border border-slate-300 bg-white px-2 py-1 text-sm text-slate-800 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
            />
          </label>
          <span className="ml-auto text-[11px] text-slate-500 dark:text-slate-400">
            {t('DocumentNumberGap.totalGap', {
              defaultValue: 'Toplam boşluk: {{count}}',
              count: data?.totalGap ?? 0,
            })}
          </span>
        </div>
      }
    >
      {query.isPending ? (
        <div className="px-3 py-8 text-center text-sm text-slate-500">
          {t('common.loading', { defaultValue: 'Yükleniyor…' })}
        </div>
      ) : rows.length === 0 ? (
        <div className="rounded-lg border border-slate-200 px-3 py-10 text-center text-sm text-slate-500 dark:border-slate-800">
          {t('DocumentNumberGap.empty', {
            defaultValue: 'Bu yıl için sıra numaralı belge bulunamadı.',
          })}
        </div>
      ) : (
        <div className="overflow-x-auto rounded-lg border border-slate-200 dark:border-slate-800">
          <table className="w-full text-sm">
            <thead className="bg-slate-50 text-left text-xs uppercase tracking-wide text-slate-500 dark:bg-slate-900/50 dark:text-slate-400">
              <tr>
                <th className="px-3 py-2">
                  {t('DocumentNumberGap.cols.type', { defaultValue: 'Belge' })}
                </th>
                <th className="px-3 py-2">
                  {t('DocumentNumberGap.cols.prefix', { defaultValue: 'Önek' })}
                </th>
                <th className="px-3 py-2 text-right">
                  {t('DocumentNumberGap.cols.expected', { defaultValue: 'Beklenen' })}
                </th>
                <th className="px-3 py-2 text-right">
                  {t('DocumentNumberGap.cols.used', { defaultValue: 'Kullanılan' })}
                </th>
                <th className="px-3 py-2 text-right">
                  {t('DocumentNumberGap.cols.max', { defaultValue: 'En Yüksek' })}
                </th>
                <th className="px-3 py-2 text-right">
                  {t('DocumentNumberGap.cols.gap', { defaultValue: 'Boşluk' })}
                </th>
                <th className="px-3 py-2">
                  {t('DocumentNumberGap.cols.missing', { defaultValue: 'Eksik Numaralar' })}
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
              {rows.map((r) => (
                <tr key={r.documentType} className="text-slate-700 dark:text-slate-200">
                  <td className="px-3 py-2 font-medium">
                    {t(`DocumentNumberGap.types.${r.documentType}` as const, {
                      defaultValue: r.documentType,
                    })}
                  </td>
                  <td className="px-3 py-2 font-mono text-xs text-slate-500">{r.prefix}</td>
                  <td className="px-3 py-2 text-right tabular-nums">{r.expected}</td>
                  <td className="px-3 py-2 text-right tabular-nums">{r.usedCount}</td>
                  <td className="px-3 py-2 text-right tabular-nums">{r.maxUsed}</td>
                  <td className="px-3 py-2 text-right">
                    {r.gapCount > 0 ? (
                      <span className="inline-flex items-center rounded bg-danger-100 px-1.5 py-0.5 text-xs font-semibold text-danger-700 dark:bg-danger-500/20 dark:text-danger-300">
                        {r.gapCount}
                      </span>
                    ) : (
                      <span className="inline-flex items-center rounded bg-success-100 px-1.5 py-0.5 text-xs font-medium text-success-700 dark:bg-success-500/20 dark:text-success-300">
                        {t('DocumentNumberGap.sequential', { defaultValue: 'Kesintisiz' })}
                      </span>
                    )}
                  </td>
                  <td className="px-3 py-2">
                    {r.missingNumbers.length === 0 ? (
                      <span className="text-xs text-slate-400">—</span>
                    ) : (
                      <div className="flex flex-wrap gap-1">
                        {r.missingNumbers.slice(0, MISSING_PREVIEW).map((n) => (
                          <span
                            key={n}
                            className="rounded bg-warning-50 px-1.5 py-0.5 font-mono text-[11px] text-warning-800 dark:bg-warning-500/10 dark:text-warning-300"
                          >
                            {r.prefix}-{r.year}-{n}
                          </span>
                        ))}
                        {r.missingNumbers.length > MISSING_PREVIEW && (
                          <span className="text-[11px] text-slate-500">
                            {t('DocumentNumberGap.moreMissing', {
                              defaultValue: '+{{count}} daha',
                              count: r.missingNumbers.length - MISSING_PREVIEW,
                            })}
                          </span>
                        )}
                      </div>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </ListPageTemplate>
  );
};

export default DocumentNumberGapPage;
