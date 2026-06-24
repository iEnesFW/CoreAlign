import { useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate, useParams } from 'react-router-dom';
import { ArrowLeft, Printer } from 'lucide-react';
import { formatCurrency } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { useAuthStore } from '@/shared/lib/store/authStore';
import { usePayslipQuery } from '@/features/hr/hooks/usePayrollRuns';

const PAYSLIP_CURRENCY = 'TRY';

const periodLabel = (year: number, month: number, locale: string) => {
  try {
    return new Intl.DateTimeFormat(locale, { month: 'long', year: 'numeric' }).format(
      new Date(year, month - 1, 1),
    );
  } catch {
    return `${month}/${year}`;
  }
};

export const PayslipPrintView = () => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const query = usePayslipQuery(id ?? null);
  const slip = query.data?.data;
  const tenantName = useAuthStore((s) => s.user?.tenantName ?? '');

  useEffect(() => {
    document.body.classList.add('bg-white');
    return () => document.body.classList.remove('bg-white');
  }, []);

  if (query.isPending || !slip) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-white text-sm text-slate-500">
        {t('common.loading', { defaultValue: 'Yükleniyor…' })}
      </div>
    );
  }

  const money = (v: number) => formatCurrency(v, locale, PAYSLIP_CURRENCY);

  const totalDeductions =
    slip.sgkEmployee +
    slip.unemploymentEmployee +
    slip.incomeTaxNet +
    slip.stampTax +
    slip.otherDeductionsTotal;

  const componentLabel = (componentType: string) =>
    t(`Payroll.componentType.${componentType}`, { defaultValue: componentType });
  const deductionLabel = (deductionType: string) =>
    t(`Payroll.deductionType.${deductionType}`, { defaultValue: deductionType });

  return (
    <div className="min-h-screen bg-white text-slate-900">
      <div className="no-print sticky top-0 z-10 border-b border-slate-200 bg-white px-4 py-2 print:hidden">
        <div className="mx-auto flex max-w-3xl items-center justify-between">
          <button
            type="button"
            onClick={() => navigate(-1)}
            className="inline-flex items-center gap-1 rounded px-2 py-1 text-sm text-slate-700 hover:bg-slate-100"
          >
            <ArrowLeft size={16} />
            {t('common.back', { defaultValue: 'Geri' })}
          </button>
          <button
            type="button"
            onClick={() => window.print()}
            className="inline-flex items-center gap-1 rounded bg-primary-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-primary-700"
          >
            <Printer size={14} />
            {t('Payroll.payslipPrint.button', { defaultValue: 'Yazdır' })}
          </button>
        </div>
      </div>

      <div className="mx-auto max-w-3xl px-8 py-10 print:px-0 print:py-0">
        <header className="flex items-start justify-between border-b border-slate-200 pb-6">
          <div>
            <div className="text-xs font-semibold uppercase tracking-wider text-slate-500">
              {t('Payroll.payslipPrint.employer', { defaultValue: 'İşveren' })}
            </div>
            <div className="mt-1 text-2xl font-bold text-slate-900">{tenantName}</div>
          </div>
          <div className="text-right">
            <div className="text-2xl font-bold tracking-tight text-slate-900">
              {t('Payroll.payslipPrint.heading', { defaultValue: 'Maaş Bordrosu' })}
            </div>
            <div className="mt-1 text-sm text-slate-600">
              {periodLabel(slip.periodYear, slip.periodMonth, locale)}
            </div>
            <div className="font-mono text-xs text-slate-500">{slip.payslipNumber}</div>
          </div>
        </header>

        <section className="mt-6 grid grid-cols-2 gap-6 text-sm">
          <div>
            <div className="text-[10px] font-semibold uppercase tracking-wider text-slate-500">
              {t('Payroll.payslipPrint.employee', { defaultValue: 'Personel' })}
            </div>
            <div className="mt-1 text-base font-semibold text-slate-900">
              {slip.employeeFullName}
            </div>
            <div className="font-mono text-xs text-slate-500">{slip.employeeNumber}</div>
            {slip.nationalIdMasked && (
              <div className="font-mono text-xs text-slate-500">{slip.nationalIdMasked}</div>
            )}
          </div>
          <div className="text-right">
            <PrintRow
              label={t('Payroll.payslipPrint.daysWorked', { defaultValue: 'Çalışılan Gün' })}
              value={String(slip.daysWorked)}
            />
          </div>
        </section>

        <section className="mt-8 grid grid-cols-1 gap-6 sm:grid-cols-2">
          <div className="rounded-lg border border-slate-200">
            <div className="border-b border-slate-200 bg-slate-50 px-3 py-2 text-xs font-semibold uppercase tracking-wider text-slate-600">
              {t('Payroll.payslipPrint.earnings', { defaultValue: 'Kazançlar' })}
            </div>
            <div className="divide-y divide-slate-100 text-sm">
              {slip.earningLines.map((line) => (
                <PrintLine
                  key={line.id}
                  label={componentLabel(line.componentType)}
                  value={money(line.amount)}
                />
              ))}
              <PrintLine
                label={t('Payroll.payslipPrint.gross', { defaultValue: 'Brüt Toplam' })}
                value={money(slip.grossEarnings)}
                bold
              />
            </div>
          </div>

          <div className="rounded-lg border border-slate-200">
            <div className="border-b border-slate-200 bg-slate-50 px-3 py-2 text-xs font-semibold uppercase tracking-wider text-slate-600">
              {t('Payroll.payslipPrint.deductions', { defaultValue: 'Kesintiler' })}
            </div>
            <div className="divide-y divide-slate-100 text-sm">
              <PrintLine
                label={t('Payroll.payslipPrint.sgkEmployee', { defaultValue: 'SGK İşçi Payı' })}
                value={money(slip.sgkEmployee)}
              />
              <PrintLine
                label={t('Payroll.payslipPrint.unemploymentEmployee', {
                  defaultValue: 'İşsizlik İşçi Payı',
                })}
                value={money(slip.unemploymentEmployee)}
              />
              <PrintLine
                label={t('Payroll.payslipPrint.incomeTax', { defaultValue: 'Gelir Vergisi' })}
                value={money(slip.incomeTaxNet)}
              />
              <PrintLine
                label={t('Payroll.payslipPrint.stampTax', { defaultValue: 'Damga Vergisi' })}
                value={money(slip.stampTax)}
              />
              {slip.otherDeductionsTotal > 0 && (
                <PrintLine
                  label={t('Payroll.payslipPrint.otherDeductions', {
                    defaultValue: 'Diğer Kesintiler',
                  })}
                  value={money(slip.otherDeductionsTotal)}
                />
              )}
              <PrintLine
                label={t('Payroll.payslipPrint.totalDeductions', {
                  defaultValue: 'Toplam Kesinti',
                })}
                value={money(totalDeductions)}
                bold
              />
            </div>
            {slip.minWageIncomeTaxExemptionApplied > 0 && (
              <div className="border-t border-slate-100 px-3 py-2 text-[11px] text-slate-500">
                {t('Payroll.payslipPrint.minWageExemptionNote', {
                  defaultValue:
                    'Gelir vergisi, {{amount}} asgari ücret istisnası düşülerek hesaplanmıştır.',
                  amount: money(slip.minWageIncomeTaxExemptionApplied),
                })}
              </div>
            )}
          </div>
        </section>

        <section className="mt-6 flex items-center justify-between rounded-lg border-2 border-slate-900 px-4 py-3">
          <span className="text-sm font-semibold uppercase tracking-wider text-slate-700">
            {t('Payroll.payslipPrint.netPay', { defaultValue: 'Net Ödenecek' })}
          </span>
          <span className="text-2xl font-bold text-slate-900">{money(slip.netPay)}</span>
        </section>

        {slip.deductionLines.length > 0 && (
          <section className="mt-6 rounded-lg border border-slate-200">
            <div className="border-b border-slate-200 bg-slate-50 px-3 py-2 text-xs font-semibold uppercase tracking-wider text-slate-600">
              {t('Payroll.payslipPrint.otherDeductionLines', {
                defaultValue: 'Diğer Kesinti Kalemleri',
              })}
            </div>
            <div className="divide-y divide-slate-100 text-sm">
              {slip.deductionLines.map((line) => (
                <PrintLine
                  key={line.id}
                  label={deductionLabel(line.deductionType)}
                  value={money(line.amount)}
                />
              ))}
            </div>
          </section>
        )}

        <section className="mt-6 rounded-lg border border-slate-200">
          <div className="border-b border-slate-200 bg-slate-50 px-3 py-2 text-xs font-semibold uppercase tracking-wider text-slate-600">
            {t('Payroll.payslipPrint.employerCostTitle', { defaultValue: 'İşveren Maliyeti' })}
          </div>
          <div className="divide-y divide-slate-100 text-sm">
            <PrintLine
              label={t('Payroll.payslipPrint.sgkEmployer', { defaultValue: 'SGK İşveren Payı' })}
              value={money(slip.sgkEmployer)}
            />
            <PrintLine
              label={t('Payroll.payslipPrint.unemploymentEmployer', {
                defaultValue: 'İşsizlik İşveren Payı',
              })}
              value={money(slip.unemploymentEmployer)}
            />
            <PrintLine
              label={t('Payroll.payslipPrint.employerCost', {
                defaultValue: 'Toplam İşveren Maliyeti',
              })}
              value={money(slip.employerCost)}
              bold
            />
          </div>
        </section>

        <footer className="mt-12 border-t border-slate-200 pt-4 text-center text-[10px] text-slate-500">
          {t('Payroll.payslipPrint.footer', {
            defaultValue: 'Bu bordro elektronik olarak oluşturulmuştur.',
          })}
        </footer>
      </div>
    </div>
  );
};

const PrintRow = ({ label, value }: { label: string; value: string }) => (
  <div className="flex justify-end gap-3 text-sm">
    <span className="text-[10px] font-semibold uppercase tracking-wider text-slate-500">
      {label}
    </span>
    <span className="font-medium text-slate-900">{value}</span>
  </div>
);

const PrintLine = ({ label, value, bold }: { label: string; value: string; bold?: boolean }) => (
  <div className="flex items-center justify-between px-3 py-2">
    <span className={bold ? 'font-semibold text-slate-800' : 'text-slate-600'}>{label}</span>
    <span className={`font-mono ${bold ? 'font-bold text-slate-900' : 'text-slate-800'}`}>
      {value}
    </span>
  </div>
);

export default PayslipPrintView;
