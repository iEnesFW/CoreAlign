import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  ArrowRight,
  CheckCircle2,
  ChevronLeft,
  ChevronRight,
  Lock,
  RotateCcw,
  Sparkles,
  TrendingDown,
  TrendingUp,
} from 'lucide-react';
import { toast } from 'sonner';
import { toastApiError } from '@/shared/lib/mutationToast';
import { formatNumber } from '@/shared/lib/format';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { DetailPageTemplate } from '@/shared/ui/PageTemplate/PageTemplate';
import { Button } from '@/shared/ui/Button/Button';
import { Badge } from '@/shared/ui/Badge/Badge';
import { useConfirm } from '@/shared/ui/ConfirmDialog/useConfirm';
import { useDecimalPlaces } from '@/features/settings/hooks/useSettingsQueries';
import {
  useCloseFiscalYear,
  useOpenNextFiscalYear,
  useReverseFiscalYearClose,
} from '@/features/accounting/hooks/useFiscalYearCloseQueries';
import type { YearEndEntry } from '@/features/accounting/model/fiscalYearClose.types';
import type { JournalEntry, JournalLine } from '@/features/accounting/model/journalEntry.types';

const currentYear = () => new Date().getFullYear();

const PROFIT_CODE = '590';
const LOSS_CODE = '591';
const RESULT_CODE = '690';

export const YearEndClosePage = () => {
  const { t, i18n } = useTranslation();
  const locale = i18n.language;
  const decimals = useDecimalPlaces();
  const fmt = (n: number) => formatNumber(n, locale, decimals);
  const confirm = useConfirm();

  const [year, setYear] = useState(currentYear() - 1);
  const [closeResult, setCloseResult] = useState<YearEndEntry | null>(null);
  const [openingResult, setOpeningResult] = useState<YearEndEntry | null>(null);

  const closeMutation = useCloseFiscalYear();
  const openMutation = useOpenNextFiscalYear();
  const reverseMutation = useReverseFiscalYearClose();

  const resetForYear = (next: number) => {
    setYear(next);
    setCloseResult(null);
    setOpeningResult(null);
  };

  const isClosed = closeResult !== null;
  const alreadyExisted = closeResult?.alreadyExisted ?? false;
  const netResult = closeResult?.netResult ?? 0;
  const isProfit = netResult >= 0;
  const closingEntry = closeResult?.entry ?? null;

  const runClose = async () => {
    const ok = await confirm({
      title: t('YearEndClose.confirmTitle', { defaultValue: 'Yıl Sonu Kapanışı' }),
      message: t('YearEndClose.confirmMessage', {
        defaultValue:
          '{{year}} yılı için TDHP yıl sonu kapanış fişi oluşturulacak: 6xx gelir/gider hesapları 690’a, 690 ise 590/591 özkaynak hesabına devredilecektir. Bu işlem post edilmiş yevmiye fişi üretir.',
        year,
      }),
      confirmLabel: t('YearEndClose.confirmAction', { defaultValue: 'Kapanışı Oluştur' }),
      tone: 'danger',
    });
    if (!ok) return;
    try {
      const res = await closeMutation.mutateAsync(year);
      const data = res.data ?? null;
      setCloseResult(data);
      if (data?.alreadyExisted) {
        toast.info(
          t('YearEndClose.alreadyExistedToast', {
            defaultValue: '{{year}} zaten kapatılmış; mevcut fiş gösteriliyor.',
            year,
          }),
        );
      } else {
        toast.success(t('YearEndClose.closed', { defaultValue: '{{year}} yılı kapatıldı.', year }));
      }
    } catch (err) {
      toastApiError(err);
    }
  };

  const runOpen = async () => {
    try {
      const res = await openMutation.mutateAsync(year);
      setOpeningResult(res.data ?? null);
      toast.success(
        t('YearEndClose.opened', {
          defaultValue: '{{year}} açılış fişi oluşturuldu.',
          year: year + 1,
        }),
      );
    } catch (err) {
      toastApiError(err);
    }
  };

  const runReverse = async () => {
    const ok = await confirm({
      title: t('YearEndClose.reverseTitle', { defaultValue: 'Kapanışı Geri Al' }),
      message: t('YearEndClose.confirmReverse', {
        defaultValue:
          '{{year}} yıl sonu kapanışı ters çevrilecek (karşı kayıt). Yalnızca açılış fişi henüz oluşturulmamışsa yapın.',
        year,
      }),
      confirmLabel: t('YearEndClose.reverseAction', { defaultValue: 'Geri Al' }),
      tone: 'danger',
    });
    if (!ok) return;
    try {
      await reverseMutation.mutateAsync(year);
      setCloseResult(null);
      setOpeningResult(null);
      toast.success(
        t('YearEndClose.reversed', { defaultValue: '{{year}} kapanışı geri alındı.', year }),
      );
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <DetailPageTemplate
      header={
        <PageHeader
          icon={<Lock size={20} />}
          title={t('YearEndClose.title', { defaultValue: 'Yıl Sonu Kapanış (Kapanış Fişi)' })}
          subtitle={t('YearEndClose.subtitle', {
            defaultValue:
              'TDHP yıl sonu kapanışı: tüm 6xx gelir/gider hesapları 690 Dönem Kârı/Zararı hesabına, oradan da 590 (kâr) veya 591 (zarar) özkaynak hesabına devredilir.',
          })}
          tone="indigo"
        />
      }
    >
      <div className="flex flex-wrap items-end gap-3">
        <div>
          <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
            {t('YearEndClose.fiscalYear', { defaultValue: 'Mali Yıl' })}
          </label>
          <div className="mt-1 inline-flex items-center gap-2">
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => resetForYear(year - 1)}
              aria-label={t('YearEndClose.prevYear', { defaultValue: 'Önceki yıl' })}
            >
              <ChevronLeft size={14} />
            </Button>
            <span className="min-w-[3rem] text-center text-lg font-semibold text-slate-900 dark:text-slate-100">
              {year}
            </span>
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => resetForYear(year + 1)}
              aria-label={t('YearEndClose.nextYear', { defaultValue: 'Sonraki yıl' })}
            >
              <ChevronRight size={14} />
            </Button>
          </div>
        </div>

        <div className="ml-auto inline-flex items-center gap-2">
          {isClosed ? (
            <Badge variant="success">
              <CheckCircle2 className="mr-1 inline" size={11} />
              {t('YearEndClose.alreadyClosed', { defaultValue: '{{year}} kapatıldı', year })}
            </Badge>
          ) : (
            <Badge variant="warning">
              {t('YearEndClose.open', { defaultValue: '{{year}} açık', year })}
            </Badge>
          )}
        </div>
      </div>

      <div className="rounded-lg border border-slate-200 bg-white p-4 dark:border-slate-800 dark:bg-slate-900">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div className="max-w-xl text-sm text-slate-600 dark:text-slate-300">
            {alreadyExisted
              ? t('YearEndClose.closedHint', {
                  defaultValue:
                    'Bu mali yıl için kapanış fişi zaten oluşturulmuş. İşlem idempotenttir; tekrar çalıştırmak yeni fiş üretmez.',
                })
              : t('YearEndClose.guardHint', {
                  defaultValue:
                    'Kapanış önemli bir işlemdir ve post edilmiş bir Kapanış fişi üretir. Devam etmeden önce dönem mizanının doğru olduğundan emin olun.',
                })}
          </div>
          <div className="inline-flex flex-wrap gap-2">
            <Button
              type="button"
              variant={isClosed ? 'outline' : 'primary'}
              onClick={runClose}
              disabled={closeMutation.isPending}
            >
              <Lock size={14} />
              {closeMutation.isPending
                ? t('YearEndClose.closing', { defaultValue: 'Kapatılıyor…' })
                : isClosed
                  ? t('YearEndClose.refresh', { defaultValue: 'Kapanışı Görüntüle' })
                  : t('YearEndClose.runClose', { defaultValue: 'Yıl Sonu Kapanışını Çalıştır' })}
            </Button>
            {isClosed && (
              <>
                <Button
                  type="button"
                  variant="secondary"
                  onClick={runOpen}
                  disabled={openMutation.isPending || openingResult !== null}
                >
                  <Sparkles size={14} />
                  {openingResult !== null
                    ? t('YearEndClose.openingDone', {
                        defaultValue: '{{year}} açılışı yapıldı',
                        year: year + 1,
                      })
                    : openMutation.isPending
                      ? t('YearEndClose.opening', { defaultValue: 'Açılıyor…' })
                      : t('YearEndClose.runOpen', {
                          defaultValue: '{{year}} Açılış Fişini Oluştur',
                          year: year + 1,
                        })}
                </Button>
                <Button
                  type="button"
                  variant="ghost"
                  onClick={runReverse}
                  disabled={reverseMutation.isPending || openingResult !== null}
                >
                  <RotateCcw size={14} />
                  {reverseMutation.isPending
                    ? t('YearEndClose.reversing', { defaultValue: 'Geri alınıyor…' })
                    : t('YearEndClose.reverseClose', { defaultValue: 'Kapanışı Geri Al' })}
                </Button>
              </>
            )}
          </div>
        </div>
      </div>

      {closingEntry && (
        <>
          <div
            className={`flex items-center justify-between rounded-lg border px-4 py-3 ${
              isProfit
                ? 'border-success-200 bg-success-50 dark:border-success-500/30 dark:bg-success-500/10'
                : 'border-danger-200 bg-danger-50 dark:border-danger-500/30 dark:bg-danger-500/10'
            }`}
          >
            <div className="flex items-center gap-2">
              {isProfit ? (
                <TrendingUp className="text-success-600 dark:text-success-300" size={20} />
              ) : (
                <TrendingDown className="text-danger-600 dark:text-danger-300" size={20} />
              )}
              <div>
                <div className="text-xs font-medium uppercase tracking-wide text-slate-500">
                  {t('YearEndClose.netResult', { defaultValue: 'Dönem Sonucu' })}
                </div>
                <div
                  className={`text-lg font-bold ${
                    isProfit
                      ? 'text-success-700 dark:text-success-300'
                      : 'text-danger-700 dark:text-danger-300'
                  }`}
                >
                  {isProfit
                    ? t('YearEndClose.netProfit', {
                        defaultValue: 'Net Kâr: {{value}}',
                        value: fmt(Math.abs(netResult)),
                      })
                    : t('YearEndClose.netLoss', {
                        defaultValue: 'Net Zarar: {{value}}',
                        value: fmt(Math.abs(netResult)),
                      })}
                </div>
              </div>
            </div>
            <div className="flex items-center gap-2 text-xs font-medium text-slate-600 dark:text-slate-300">
              <span className="font-mono">{RESULT_CODE}</span>
              <ArrowRight size={13} />
              <span className="font-mono">{isProfit ? PROFIT_CODE : LOSS_CODE}</span>
              <span>
                {isProfit
                  ? t('YearEndClose.transferProfit', {
                      defaultValue: 'Dönem Net Kârı özkaynağa devredildi',
                    })
                  : t('YearEndClose.transferLoss', {
                      defaultValue: 'Dönem Net Zararı özkaynağa devredildi',
                    })}
              </span>
            </div>
          </div>

          <ClosingEntryTable
            entry={closingEntry}
            fmt={fmt}
            t={t}
            caption={t('YearEndClose.closingEntryCaption', { defaultValue: 'Kapanış Fişi' })}
          />
        </>
      )}

      {openingResult?.entry && (
        <ClosingEntryTable
          entry={openingResult.entry}
          fmt={fmt}
          t={t}
          caption={t('YearEndClose.openingEntryCaption', {
            defaultValue: 'Açılış Fişi ({{year}})',
            year: year + 1,
          })}
        />
      )}
    </DetailPageTemplate>
  );
};

const ClosingEntryTable = ({
  entry,
  fmt,
  t,
  caption,
}: {
  entry: JournalEntry;
  fmt: (n: number) => string;
  t: ReturnType<typeof useTranslation>['t'];
  caption: string;
}) => (
  <div className="overflow-hidden rounded-lg border border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-900">
    <div className="flex flex-wrap items-center justify-between gap-2 border-b border-slate-200 px-4 py-2.5 dark:border-slate-800">
      <div className="flex items-center gap-2">
        <span className="text-xs font-semibold uppercase tracking-wide text-slate-500">
          {caption}
        </span>
        <span className="font-mono text-sm font-semibold text-slate-900 dark:text-slate-100">
          {entry.number}
        </span>
        {entry.status === 'Posted' && (
          <Badge variant="success">
            {t('YearEndClose.posted', { defaultValue: 'Post edildi' })}
          </Badge>
        )}
      </div>
      {entry.description && (
        <span className="text-xs italic text-slate-500 dark:text-slate-400">
          {entry.description}
        </span>
      )}
    </div>
    <table className="w-full text-sm">
      <thead className="bg-slate-50 text-[10px] font-semibold uppercase text-slate-600 dark:bg-slate-800/50 dark:text-slate-300">
        <tr>
          <th className="px-3 py-2 text-left">{t('YearEndClose.code', { defaultValue: 'Kod' })}</th>
          <th className="px-3 py-2 text-left">
            {t('YearEndClose.accountName', { defaultValue: 'Hesap Adı' })}
          </th>
          <th className="px-3 py-2 text-right">
            {t('YearEndClose.debit', { defaultValue: 'Borç' })}
          </th>
          <th className="px-3 py-2 text-right">
            {t('YearEndClose.credit', { defaultValue: 'Alacak' })}
          </th>
        </tr>
      </thead>
      <tbody>
        {entry.lines.map((line: JournalLine) => (
          <tr
            key={line.id}
            className="border-t border-slate-100 hover:bg-slate-50 dark:border-slate-800 dark:hover:bg-slate-800/30"
          >
            <td className="px-3 py-2 font-mono text-xs text-slate-400">{line.accountCode}</td>
            <td className="px-3 py-2 text-xs text-slate-900 dark:text-slate-100">
              {line.accountName}
            </td>
            <td className="px-3 py-2 text-right font-mono text-xs">
              {line.debit ? fmt(line.debit) : '—'}
            </td>
            <td className="px-3 py-2 text-right font-mono text-xs">
              {line.credit ? fmt(line.credit) : '—'}
            </td>
          </tr>
        ))}
      </tbody>
      <tfoot className="border-t-2 border-slate-300 bg-slate-50 font-semibold dark:border-slate-700 dark:bg-slate-800/50">
        <tr>
          <td colSpan={2} className="px-3 py-2 text-right text-xs uppercase">
            {t('YearEndClose.total', { defaultValue: 'Toplam' })}
          </td>
          <td className="px-3 py-2 text-right font-mono text-xs">{fmt(entry.totalDebit)}</td>
          <td className="px-3 py-2 text-right font-mono text-xs">{fmt(entry.totalCredit)}</td>
        </tr>
      </tfoot>
    </table>
  </div>
);

export default YearEndClosePage;
