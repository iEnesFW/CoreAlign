import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { UserPlus } from 'lucide-react';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useCreateEmployee, useUpdateEmployee } from '../hooks/useEmployees';
import type { Employee } from '../model/employee.types';
import {
  EMPLOYMENT_TYPES,
  SALARY_BASES,
  type EmploymentType,
  type SalaryBasis,
} from '../model/enums';

interface Props {
  employee: Employee | null;
  onClose: () => void;
}

const todayIso = () => new Date().toISOString().slice(0, 10);

const fieldClass =
  'mt-1 w-full rounded border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100';

const labelClass = 'block text-xs font-medium text-slate-700 dark:text-slate-300';

export const EmployeeFormModal = ({ employee, onClose }: Props) => {
  const { t } = useTranslation();
  const isEdit = employee !== null;

  const createMutation = useCreateEmployee();
  const updateMutation = useUpdateEmployee();

  const [firstName, setFirstName] = useState(employee?.firstName ?? '');
  const [lastName, setLastName] = useState(employee?.lastName ?? '');
  const [nationalId, setNationalId] = useState('');
  const [email, setEmail] = useState(employee?.email ?? '');
  const [phone, setPhone] = useState(employee?.phone ?? '');
  const [title, setTitle] = useState(employee?.title ?? '');
  const [department, setDepartment] = useState(employee?.department ?? '');
  const [iban, setIban] = useState('');
  const [bankName, setBankName] = useState(employee?.bankName ?? '');
  const [baseSalaryGross, setBaseSalaryGross] = useState(
    employee ? String(employee.baseSalaryGross) : '',
  );
  const [salaryCurrency, setSalaryCurrency] = useState(employee?.salaryCurrency ?? 'TRY');
  const [employmentType, setEmploymentType] = useState<EmploymentType>(
    employee?.employmentType ?? 'FullTime',
  );
  const [salaryBasis, setSalaryBasis] = useState<SalaryBasis>(employee?.salaryBasis ?? 'Gross');
  const [hireDate, setHireDate] = useState(employee?.hireDate?.slice(0, 10) ?? todayIso());
  const [dependentCount, setDependentCount] = useState(
    employee ? String(employee.dependentCount) : '0',
  );
  const [spouseEmployed, setSpouseEmployed] = useState(employee?.spouseEmployed ?? false);

  const pending = createMutation.isPending || updateMutation.isPending;

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!firstName.trim() || !lastName.trim()) {
      toast.error(t('Payroll.employeeForm.nameRequired', { defaultValue: 'Ad ve soyad zorunlu.' }));
      return;
    }
    try {
      if (isEdit && employee) {
        await updateMutation.mutateAsync({
          id: employee.id,
          firstName: firstName.trim(),
          lastName: lastName.trim(),
          email: email.trim() || null,
          phone: phone.trim() || null,
          department: department.trim() || null,
          title: title.trim() || null,
          iban: iban.trim() || null,
          bankName: bankName.trim() || null,
          dependentCount: Number(dependentCount) || 0,
          spouseEmployed,
        });
        toast.success(t('Payroll.employeeForm.updated', { defaultValue: 'Personel güncellendi.' }));
      } else {
        if (!nationalId.trim()) {
          toast.error(
            t('Payroll.employeeForm.tcRequired', { defaultValue: 'TC kimlik no zorunlu.' }),
          );
          return;
        }
        if (!(Number(baseSalaryGross) > 0)) {
          toast.error(
            t('Payroll.employeeForm.salaryRequired', { defaultValue: 'Geçerli bir maaş girin.' }),
          );
          return;
        }
        await createMutation.mutateAsync({
          firstName: firstName.trim(),
          lastName: lastName.trim(),
          nationalId: nationalId.trim(),
          hireDate,
          baseSalaryGross: Number(baseSalaryGross),
          employmentType,
          salaryBasis,
          salaryCurrency: salaryCurrency.toUpperCase(),
          email: email.trim() || null,
          phone: phone.trim() || null,
          department: department.trim() || null,
          title: title.trim() || null,
          iban: iban.trim() || null,
          bankName: bankName.trim() || null,
          dependentCount: Number(dependentCount) || 0,
          spouseEmployed,
        });
        toast.success(t('Payroll.employeeForm.created', { defaultValue: 'Personel oluşturuldu.' }));
      }
      onClose();
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <Modal
      open
      onClose={onClose}
      size="2xl"
      icon={<UserPlus size={16} />}
      title={
        isEdit
          ? t('Payroll.employeeForm.editTitle', { defaultValue: 'Personeli Düzenle' })
          : t('Payroll.employeeForm.newTitle', { defaultValue: 'Yeni Personel' })
      }
      footer={
        <>
          <Button variant="outline" size="sm" onClick={onClose} type="button">
            {t('common.cancel', { defaultValue: 'İptal' })}
          </Button>
          <Button size="sm" type="submit" form="employee-form" isLoading={pending}>
            {t('common.save', { defaultValue: 'Kaydet' })}
          </Button>
        </>
      }
    >
      <form id="employee-form" onSubmit={submit} className="space-y-3">
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <div>
            <label className={labelClass}>
              {t('Payroll.employeeForm.firstName', { defaultValue: 'Ad' })} *
            </label>
            <input
              value={firstName}
              onChange={(e) => setFirstName(e.target.value)}
              className={fieldClass}
            />
          </div>
          <div>
            <label className={labelClass}>
              {t('Payroll.employeeForm.lastName', { defaultValue: 'Soyad' })} *
            </label>
            <input
              value={lastName}
              onChange={(e) => setLastName(e.target.value)}
              className={fieldClass}
            />
          </div>
          {!isEdit && (
            <div>
              <label className={labelClass}>
                {t('Payroll.employeeForm.nationalId', { defaultValue: 'TC Kimlik No' })} *
              </label>
              <input
                value={nationalId}
                onChange={(e) => setNationalId(e.target.value.replace(/\D/g, '').slice(0, 11))}
                inputMode="numeric"
                maxLength={11}
                className={fieldClass}
              />
            </div>
          )}
          <div>
            <label className={labelClass}>
              {t('Payroll.employeeForm.jobTitle', { defaultValue: 'Unvan' })}
            </label>
            <input
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              className={fieldClass}
            />
          </div>
          <div>
            <label className={labelClass}>
              {t('Payroll.employeeForm.department', { defaultValue: 'Departman' })}
            </label>
            <input
              value={department}
              onChange={(e) => setDepartment(e.target.value)}
              className={fieldClass}
            />
          </div>
          <div>
            <label className={labelClass}>
              {t('Payroll.employeeForm.email', { defaultValue: 'E-posta' })}
            </label>
            <input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className={fieldClass}
            />
          </div>
          <div>
            <label className={labelClass}>
              {t('Payroll.employeeForm.phone', { defaultValue: 'Telefon' })}
            </label>
            <input
              value={phone}
              onChange={(e) => setPhone(e.target.value)}
              className={fieldClass}
            />
          </div>
          <div>
            <label className={labelClass}>
              {t('Payroll.employeeForm.iban', { defaultValue: 'IBAN' })}
            </label>
            <input
              value={iban}
              onChange={(e) => setIban(e.target.value.toUpperCase())}
              placeholder={isEdit ? (employee?.ibanMasked ?? '') : 'TR..'}
              className={`${fieldClass} font-mono`}
            />
          </div>
          <div>
            <label className={labelClass}>
              {t('Payroll.employeeForm.bankName', { defaultValue: 'Banka' })}
            </label>
            <input
              value={bankName}
              onChange={(e) => setBankName(e.target.value)}
              className={fieldClass}
            />
          </div>
          <div>
            <label className={labelClass}>
              {t('Payroll.employeeForm.dependentCount', {
                defaultValue: 'Bakmakla Yükümlü Sayısı',
              })}
            </label>
            <input
              type="number"
              min={0}
              value={dependentCount}
              onChange={(e) => setDependentCount(e.target.value)}
              className={`${fieldClass} text-right`}
            />
          </div>
          <label className="inline-flex items-center gap-1.5 self-end pb-1.5 text-xs text-slate-700 dark:text-slate-300">
            <input
              type="checkbox"
              checked={spouseEmployed}
              onChange={(e) => setSpouseEmployed(e.target.checked)}
            />
            {t('Payroll.employeeForm.spouseEmployed', { defaultValue: 'Eşi Çalışıyor' })}
          </label>
          {!isEdit && (
            <>
              <div>
                <label className={labelClass}>
                  {t('Payroll.employeeForm.baseSalary', { defaultValue: 'Brüt Maaş' })} *
                </label>
                <input
                  type="number"
                  min={0}
                  step="any"
                  value={baseSalaryGross}
                  onChange={(e) => setBaseSalaryGross(e.target.value)}
                  className={`${fieldClass} text-right`}
                />
              </div>
              <div>
                <label className={labelClass}>
                  {t('Payroll.employeeForm.currency', { defaultValue: 'Para Birimi' })}
                </label>
                <input
                  value={salaryCurrency}
                  onChange={(e) => setSalaryCurrency(e.target.value.toUpperCase())}
                  maxLength={3}
                  className={`${fieldClass} uppercase`}
                />
              </div>
              <div>
                <label className={labelClass}>
                  {t('Payroll.employeeForm.employmentType', { defaultValue: 'Çalışma Türü' })}
                </label>
                <select
                  value={employmentType}
                  onChange={(e) => setEmploymentType(e.target.value as EmploymentType)}
                  className={fieldClass}
                >
                  {EMPLOYMENT_TYPES.map((et) => (
                    <option key={et} value={et}>
                      {t(`Payroll.employmentType.${et}`, { defaultValue: et })}
                    </option>
                  ))}
                </select>
              </div>
              <div>
                <label className={labelClass}>
                  {t('Payroll.employeeForm.salaryBasis', { defaultValue: 'Maaş Esası' })}
                </label>
                <select
                  value={salaryBasis}
                  onChange={(e) => setSalaryBasis(e.target.value as SalaryBasis)}
                  className={fieldClass}
                >
                  {SALARY_BASES.map((sb) => (
                    <option key={sb} value={sb}>
                      {t(`Payroll.salaryBasis.${sb}`, { defaultValue: sb })}
                    </option>
                  ))}
                </select>
              </div>
              <div>
                <label className={labelClass}>
                  {t('Payroll.employeeForm.hireDate', { defaultValue: 'İşe Giriş Tarihi' })}
                </label>
                <input
                  type="date"
                  value={hireDate}
                  onChange={(e) => setHireDate(e.target.value)}
                  className={fieldClass}
                />
              </div>
            </>
          )}
        </div>
      </form>
    </Modal>
  );
};
