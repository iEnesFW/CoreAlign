import { useTranslation } from 'react-i18next';
import { ArrowDownLeft, ArrowUpRight, BookOpen, CheckCircle2, CircleDot } from 'lucide-react';
import { Badge } from '@/shared/ui/Badge/Badge';
import type { Invoice } from '@/features/invoices/model/invoice.types';
import { type GlEntry, fmtCurrency } from './ledgerModel';

export const GlPostingCard = ({
  invoice,
  entries,
  totalDebit,
  totalCredit,
  locale,
}: {
  invoice: Invoice;
  entries: GlEntry[];
  totalDebit: number;
  totalCredit: number;
  locale: string;
}) => {
  const { t } = useTranslation();
  const balanced = Math.abs(totalDebit - totalCredit) < 0.01;
  return (
    <section className="rounded-lg border border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-900">
      <header className="flex items-center justify-between gap-2 border-b border-slate-100 px-3 py-2 text-[10px] font-semibold uppercase tracking-wider text-slate-500 dark:border-slate-800 dark:text-slate-400">
        <span className="inline-flex items-center gap-1.5">
          <BookOpen size={12} />
          {t('invoices.ledger.glTitle')}
        </span>
        <Badge variant={invoice.isPostedToLedger ? 'success' : 'neutral'} pill>
          {invoice.isPostedToLedger ? t('invoices.ledger.posted') : t('invoices.ledger.notPosted')}
        </Badge>
      </header>
      <table className="w-full text-left text-[11px]">
        <thead className="bg-slate-50 text-[10px] uppercase tracking-wider text-slate-500 dark:bg-slate-900/40 dark:text-slate-400">
          <tr>
            <th className="px-3 py-1.5">{t('invoices.ledger.account')}</th>
            <th className="px-3 py-1.5">{t('invoices.ledger.description')}</th>
            <th className="px-3 py-1.5 text-right">{t('invoices.ledger.debit')}</th>
            <th className="px-3 py-1.5 text-right">{t('invoices.ledger.credit')}</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
          {entries.map((e, i) => (
            <tr key={`${e.account}-${i}`}>
              <td className="px-3 py-1.5 font-mono text-[11px] text-slate-900 dark:text-slate-100">
                {e.account}
              </td>
              <td className="px-3 py-1.5 text-slate-700 dark:text-slate-300">{e.description}</td>
              <td className="px-3 py-1.5 text-right tabular-nums text-danger-600 dark:text-danger-400">
                {e.debit > 0 ? (
                  <>
                    <ArrowUpRight size={9} className="mr-1 inline" />
                    {fmtCurrency(e.debit, invoice.currency, locale)}
                  </>
                ) : (
                  '—'
                )}
              </td>
              <td className="px-3 py-1.5 text-right tabular-nums text-success-600 dark:text-success-400">
                {e.credit > 0 ? (
                  <>
                    <ArrowDownLeft size={9} className="mr-1 inline" />
                    {fmtCurrency(e.credit, invoice.currency, locale)}
                  </>
                ) : (
                  '—'
                )}
              </td>
            </tr>
          ))}
        </tbody>
        <tfoot className="bg-slate-50 dark:bg-slate-800/40">
          <tr>
            <td
              colSpan={2}
              className="px-3 py-2 text-right text-[10px] font-semibold uppercase text-slate-500 dark:text-slate-400"
            >
              {t('invoices.ledger.total')}
            </td>
            <td className="px-3 py-2 text-right font-bold tabular-nums text-slate-900 dark:text-slate-100">
              {fmtCurrency(totalDebit, invoice.currency, locale)}
            </td>
            <td className="px-3 py-2 text-right font-bold tabular-nums text-slate-900 dark:text-slate-100">
              {fmtCurrency(totalCredit, invoice.currency, locale)}
            </td>
          </tr>
          <tr>
            <td
              colSpan={4}
              className="border-t border-slate-200 px-3 py-1.5 text-right text-[10px] dark:border-slate-800"
            >
              {balanced ? (
                <span className="inline-flex items-center gap-1 text-success-600 dark:text-success-400">
                  <CheckCircle2 size={10} /> {t('invoices.ledger.balanced')}
                </span>
              ) : (
                <span className="inline-flex items-center gap-1 text-danger-600 dark:text-danger-400">
                  <CircleDot size={10} />
                  {t('invoices.ledger.unbalanced')}:{' '}
                  {fmtCurrency(totalDebit - totalCredit, invoice.currency, locale)}
                </span>
              )}
            </td>
          </tr>
        </tfoot>
      </table>
    </section>
  );
};
