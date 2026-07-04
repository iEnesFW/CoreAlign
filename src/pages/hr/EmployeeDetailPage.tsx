import { useState } from 'react';
import { useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  Coins,
  Contact,
  LogIn,
  LogOut,
  MinusCircle,
  Pencil,
  UserCog,
  UserRound,
} from 'lucide-react';
import { DetailPageTemplate } from '@/shared/ui/PageTemplate/PageTemplate';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { Button } from '@/shared/ui/Button/Button';
import { Badge, type BadgeVariant } from '@/shared/ui/Badge/Badge';
import { formatCurrency } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { useAuthStore } from '@/shared/lib/store/authStore';
import { useEmployeeQuery } from '@/features/hr/hooks/useEmployees';
import { downloadVCard } from '@/features/hr/lib/employeeVCard';
import { ChangeBaseSalaryModal } from '@/features/hr/ui/ChangeBaseSalaryModal';
import { EmployeeFormModal } from '@/features/hr/ui/EmployeeFormModal';
import { SalaryComponentEditor } from '@/features/hr/ui/SalaryComponentEditor';
import { DeductionEditor } from '@/features/hr/ui/DeductionEditor';
import type { EmploymentStatus } from '@/features/hr/model/enums';
import type { EmployeeDeduction, SalaryComponent } from '@/features/hr/model/employee.types';
import { Field } from './employeeDetail/EmployeeParts';
import { EmployeeInfoBar } from './employeeDetail/EmployeeInfoBar';
import { SalaryComponentsTable } from './employeeDetail/SalaryComponentsTable';
import { DeductionsTable } from './employeeDetail/DeductionsTable';
import { useEmployeeActions } from './employeeDetail/useEmployeeActions';

type Tab = 'overview' | 'components' | 'deductions';

const STATUS_VARIANT: Record<EmploymentStatus, BadgeVariant> = {
  Active: 'success',
  OnLeave: 'warning',
  Terminated: 'neutral',
};

export const EmployeeDetailPage = () => {
  const { id } = useParams<{ id: string }>();
  const { t } = useTranslation();
  const locale = useFormatLocale();

  const [tab, setTab] = useState<Tab>('overview');
  const [showEdit, setShowEdit] = useState(false);
  const [showChangeSalary, setShowChangeSalary] = useState(false);
  const orgName = useAuthStore((s) => s.user?.tenantName ?? '');
  const [componentModal, setComponentModal] = useState<{
    component: SalaryComponent | null;
  } | null>(null);
  const [deductionModal, setDeductionModal] = useState<{
    deduction: EmployeeDeduction | null;
  } | null>(null);

  const query = useEmployeeQuery(id ?? null);
  const {
    terminate,
    goOnLeave,
    returnFromLeave,
    componentTypeLabel,
    deductionTypeLabel,
    runTerminate,
    runLeave,
    runReturn,
    deleteComponent,
    deleteDeduction,
  } = useEmployeeActions(id ?? '');

  const e = query.data?.data;

  if (query.isPending) {
    return (
      <div className="p-6 text-sm text-slate-500">
        {t('common.loading', { defaultValue: 'Yükleniyor…' })}
      </div>
    );
  }
  if (!e) {
    return (
      <div className="p-6 text-sm text-slate-500">
        {t('Payroll.employeeDetail.notFound', { defaultValue: 'Personel bulunamadı.' })}
      </div>
    );
  }

  const statusLabel = t(`Payroll.employmentStatus.${e.status}`, { defaultValue: e.status });
  const yesNo = (value: boolean) =>
    value ? t('Common.Yes', { defaultValue: 'Evet' }) : t('Common.No', { defaultValue: 'Hayır' });

  const tabs: { id: Tab; label: string; icon: typeof UserRound }[] = [
    {
      id: 'overview',
      label: t('Payroll.employeeDetail.tabs.overview', { defaultValue: 'Genel' }),
      icon: UserRound,
    },
    {
      id: 'components',
      label: t('Payroll.employeeDetail.tabs.components', { defaultValue: 'Maaş Bileşenleri' }),
      icon: Coins,
    },
    {
      id: 'deductions',
      label: t('Payroll.employeeDetail.tabs.deductions', { defaultValue: 'Kesintiler' }),
      icon: MinusCircle,
    },
  ];

  return (
    <DetailPageTemplate
      header={
        <PageHeader
          icon={<UserCog size={20} />}
          title={e.fullName}
          subtitle={e.title ?? undefined}
          crumbs={[
            {
              label: t('Payroll.employeeDetail.backToList', { defaultValue: 'Personel' }),
              to: '/dashboard/hr/employees',
            },
            { label: e.fullName },
          ]}
          actions={
            <div className="flex flex-wrap items-center gap-2">
              <Button variant="secondary" size="sm" onClick={() => downloadVCard(e, orgName)}>
                <Contact size={14} />
                {t('Payroll.employeeDetail.downloadVCard', { defaultValue: 'vCard İndir' })}
              </Button>
              <Button variant="secondary" size="sm" onClick={() => setShowEdit(true)}>
                <Pencil size={14} />
                {t('common.edit', { defaultValue: 'Düzenle' })}
              </Button>
              {e.status === 'Active' && (
                <>
                  <Button variant="outline" size="sm" onClick={() => setShowChangeSalary(true)}>
                    <Coins size={14} />
                    {t('Payroll.changeSalary.title', { defaultValue: 'Maaş Değiştir' })}
                  </Button>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={runLeave}
                    isLoading={goOnLeave.isPending}
                  >
                    <LogOut size={14} />
                    {t('Payroll.employeeDetail.putOnLeave', { defaultValue: 'İzne Çıkar' })}
                  </Button>
                  <Button
                    variant="danger"
                    size="sm"
                    onClick={() => runTerminate(e.fullName)}
                    isLoading={terminate.isPending}
                  >
                    {t('Payroll.employeeDetail.terminate', { defaultValue: 'İşten Çıkar' })}
                  </Button>
                </>
              )}
              {e.status === 'OnLeave' && (
                <Button
                  variant="outline"
                  size="sm"
                  onClick={runReturn}
                  isLoading={returnFromLeave.isPending}
                >
                  <LogIn size={14} />
                  {t('Payroll.employeeDetail.returnFromLeave', { defaultValue: 'İşe Döndür' })}
                </Button>
              )}
            </div>
          }
          trailing={
            <div className="flex flex-col items-stretch gap-2 sm:items-end">
              <Badge variant={STATUS_VARIANT[e.status]}>{statusLabel}</Badge>
              <div className="text-right">
                <div className="text-xs text-slate-500">
                  {t('Payroll.employeeDetail.baseSalary', { defaultValue: 'Brüt Maaş' })}
                </div>
                <div className="text-xl font-bold text-slate-900 dark:text-slate-100">
                  {formatCurrency(e.baseSalaryGross, locale, e.salaryCurrency)}
                </div>
              </div>
            </div>
          }
        />
      }
    >
      <EmployeeInfoBar employee={e} locale={locale} />

      <div className="flex gap-1 border-b border-slate-200 dark:border-slate-800">
        {tabs.map((tb) => {
          const Icon = tb.icon;
          const active = tab === tb.id;
          return (
            <button
              key={tb.id}
              type="button"
              onClick={() => setTab(tb.id)}
              className={`inline-flex items-center gap-1.5 border-b-2 px-3 py-2 text-xs font-medium transition ${
                active
                  ? 'border-primary-600 text-primary-700 dark:border-primary-400 dark:text-primary-300'
                  : 'border-transparent text-slate-500 hover:text-slate-700 dark:hover:text-slate-300'
              }`}
            >
              <Icon size={12} />
              {tb.label}
            </button>
          );
        })}
      </div>

      <div className="rounded-lg border border-slate-200 bg-white p-4 dark:border-slate-800 dark:bg-slate-900">
        {tab === 'overview' && (
          <dl className="grid grid-cols-1 gap-3 text-sm sm:grid-cols-2">
            <Field
              label={t('Payroll.employeeDetail.fields.currency', { defaultValue: 'Para Birimi' })}
            >
              {e.salaryCurrency}
            </Field>
            <Field
              label={t('Payroll.employeeDetail.fields.employmentType', {
                defaultValue: 'Çalışma Türü',
              })}
            >
              {t(`Payroll.employmentType.${e.employmentType}`, { defaultValue: e.employmentType })}
            </Field>
            <Field
              label={t('Payroll.employeeDetail.fields.salaryBasis', {
                defaultValue: 'Maaş Esası',
              })}
            >
              {t(`Payroll.salaryBasis.${e.salaryBasis}`, { defaultValue: e.salaryBasis })}
            </Field>
            <Field label={t('Payroll.employeeDetail.fields.bankName', { defaultValue: 'Banka' })}>
              {e.bankName ?? '—'}
            </Field>
            <Field
              label={t('Payroll.employeeDetail.fields.componentsCount', {
                defaultValue: 'Bileşen Sayısı',
              })}
            >
              {e.salaryComponents.length}
            </Field>
            {e.sgkRegistrationNo && (
              <Field
                label={t('Payroll.employeeDetail.fields.sgkRegistrationNo', {
                  defaultValue: 'SGK Sicil No',
                })}
              >
                <span className="font-mono">{e.sgkRegistrationNo}</span>
              </Field>
            )}
            <Field
              label={t('Payroll.employeeDetail.fields.sgkIncentiveEligible', {
                defaultValue: 'SGK Teşvik',
              })}
            >
              {yesNo(e.isSgkIncentiveEligible)}
            </Field>
            <Field
              label={t('Payroll.employeeDetail.fields.disabilityDegree', {
                defaultValue: 'Engellilik Derecesi',
              })}
            >
              {t(`Payroll.disabilityDegree.${e.disabilityDegree}`, {
                defaultValue: e.disabilityDegree,
              })}
            </Field>
            <Field
              label={t('Payroll.employeeDetail.fields.retiredWorking', {
                defaultValue: 'Emekli Çalışan',
              })}
            >
              {yesNo(e.isRetiredWorking)}
            </Field>
            <Field
              label={t('Payroll.employeeDetail.fields.sgkExempt', { defaultValue: 'SGK Muaf' })}
            >
              {yesNo(e.sgkExempt)}
            </Field>
            <Field
              label={t('Payroll.employeeDetail.fields.dependentCount', {
                defaultValue: 'Bakmakla Yükümlü',
              })}
            >
              {e.dependentCount}
            </Field>
            <Field
              label={t('Payroll.employeeDetail.fields.spouseEmployed', {
                defaultValue: 'Eş Çalışıyor',
              })}
            >
              {yesNo(e.spouseEmployed)}
            </Field>
            {e.status === 'Terminated' && e.terminationReason && (
              <Field
                full
                label={t('Payroll.employeeDetail.fields.terminationReason', {
                  defaultValue: 'Çıkış Nedeni',
                })}
              >
                {e.terminationReason}
              </Field>
            )}
          </dl>
        )}

        {tab === 'components' && (
          <SalaryComponentsTable
            components={e.salaryComponents}
            currency={e.salaryCurrency}
            locale={locale}
            typeLabel={componentTypeLabel}
            onAdd={() => setComponentModal({ component: null })}
            onEdit={(c) => setComponentModal({ component: c })}
            onDelete={deleteComponent}
          />
        )}

        {tab === 'deductions' && (
          <DeductionsTable
            deductions={e.deductions}
            currency={e.salaryCurrency}
            locale={locale}
            typeLabel={deductionTypeLabel}
            onAdd={() => setDeductionModal({ deduction: null })}
            onEdit={(d) => setDeductionModal({ deduction: d })}
            onDelete={deleteDeduction}
          />
        )}
      </div>

      {showEdit && <EmployeeFormModal employee={e} onClose={() => setShowEdit(false)} />}
      {showChangeSalary && (
        <ChangeBaseSalaryModal employee={e} onClose={() => setShowChangeSalary(false)} />
      )}
      {componentModal && (
        <SalaryComponentEditor
          employeeId={e.id}
          component={componentModal.component}
          onClose={() => setComponentModal(null)}
        />
      )}
      {deductionModal && (
        <DeductionEditor
          employeeId={e.id}
          deduction={deductionModal.deduction}
          onClose={() => setDeductionModal(null)}
        />
      )}
    </DetailPageTemplate>
  );
};

export default EmployeeDetailPage;
