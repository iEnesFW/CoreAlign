import { useTranslation } from 'react-i18next';
import { formatDate } from '@/shared/lib/format';
import type { Employee } from '@/features/hr/model/employee.types';
import { Info } from './EmployeeParts';

export const EmployeeInfoBar = ({
  employee: e,
  locale,
}: {
  employee: Employee;
  locale: string;
}) => {
  const { t } = useTranslation();
  return (
    <div className="flex flex-wrap gap-3 rounded-xl border border-slate-200 bg-white p-4 text-xs text-slate-600 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-300">
      <Info
        label={t('Payroll.employeeDetail.fields.number', { defaultValue: 'Sicil' })}
        value={e.employeeNumber}
        mono
      />
      <Info
        label={t('Payroll.employeeDetail.fields.tc', { defaultValue: 'TC Kimlik' })}
        value={e.nationalIdMasked ?? '—'}
        mono
      />
      {e.ibanMasked && (
        <Info
          label={t('Payroll.employeeDetail.fields.iban', { defaultValue: 'IBAN' })}
          value={e.ibanMasked}
          mono
        />
      )}
      {e.department && (
        <Info
          label={t('Payroll.employeeDetail.fields.department', { defaultValue: 'Departman' })}
          value={e.department}
        />
      )}
      {e.email && (
        <Info
          label={t('Payroll.employeeDetail.fields.email', { defaultValue: 'E-posta' })}
          value={e.email}
        />
      )}
      {e.phone && (
        <Info
          label={t('Payroll.employeeDetail.fields.phone', { defaultValue: 'Telefon' })}
          value={e.phone}
        />
      )}
      <Info
        label={t('Payroll.employeeDetail.fields.hireDate', { defaultValue: 'İşe Giriş' })}
        value={formatDate(e.hireDate, locale)}
      />
      {e.terminationDate && (
        <Info
          label={t('Payroll.employeeDetail.fields.terminationDate', {
            defaultValue: 'Çıkış Tarihi',
          })}
          value={formatDate(e.terminationDate, locale)}
        />
      )}
    </div>
  );
};
