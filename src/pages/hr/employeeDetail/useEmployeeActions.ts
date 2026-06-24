import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useConfirm } from '@/shared/ui/ConfirmDialog/useConfirm';
import {
  usePutEmployeeOnLeave,
  useRemoveDeduction,
  useRemoveSalaryComponent,
  useReturnFromLeave,
  useTerminateEmployee,
} from '@/features/hr/hooks/useEmployees';
import type { EmployeeDeduction, SalaryComponent } from '@/features/hr/model/employee.types';

const todayIso = () => new Date().toISOString().slice(0, 10);

export const useEmployeeActions = (employeeId: string) => {
  const { t } = useTranslation();
  const confirm = useConfirm();
  const terminate = useTerminateEmployee();
  const goOnLeave = usePutEmployeeOnLeave();
  const returnFromLeave = useReturnFromLeave();
  const removeComponent = useRemoveSalaryComponent();
  const removeDeduction = useRemoveDeduction();

  const componentTypeLabel = (c: SalaryComponent) =>
    t(`Payroll.componentType.${c.componentType}`, { defaultValue: c.componentType });
  const deductionTypeLabel = (d: EmployeeDeduction) =>
    t(`Payroll.deductionType.${d.deductionType}`, { defaultValue: d.deductionType });

  const runTerminate = async (employeeName: string) => {
    const ok = await confirm({
      title: t('Payroll.employeeDetail.terminateTitle', { defaultValue: 'İşten Çıkış' }),
      message: t('Payroll.employeeDetail.terminateConfirm', {
        defaultValue: '{{name}} işten çıkarılsın mı?',
        name: employeeName,
      }),
      confirmLabel: t('Payroll.employeeDetail.terminate', { defaultValue: 'İşten Çıkar' }),
      tone: 'danger',
    });
    if (!ok) return;
    try {
      await terminate.mutateAsync({ id: employeeId, terminationDate: todayIso() });
      toast.success(
        t('Payroll.employeeDetail.terminated', { defaultValue: 'Personel işten çıkarıldı.' }),
      );
    } catch (err) {
      toastApiError(err);
    }
  };

  const runLeave = async () => {
    try {
      await goOnLeave.mutateAsync({ id: employeeId });
      toast.success(t('Payroll.employeeDetail.leaveDone', { defaultValue: 'İzin kaydedildi.' }));
    } catch (err) {
      toastApiError(err);
    }
  };

  const runReturn = async () => {
    try {
      await returnFromLeave.mutateAsync({ id: employeeId });
      toast.success(
        t('Payroll.employeeDetail.returnDone', { defaultValue: 'İzin dönüşü kaydedildi.' }),
      );
    } catch (err) {
      toastApiError(err);
    }
  };

  const deleteComponent = async (c: SalaryComponent) => {
    const ok = await confirm({
      title: t('Payroll.employeeDetail.deleteComponentTitle', { defaultValue: 'Bileşeni Sil' }),
      message: t('Payroll.employeeDetail.deleteComponentConfirm', {
        defaultValue: '{{name}} silinsin mi?',
        name: componentTypeLabel(c),
      }),
      confirmLabel: t('common.delete', { defaultValue: 'Sil' }),
      tone: 'danger',
    });
    if (!ok) return;
    try {
      await removeComponent.mutateAsync({ id: employeeId, componentId: c.id });
      toast.success(
        t('Payroll.employeeDetail.componentDeleted', { defaultValue: 'Bileşen silindi.' }),
      );
    } catch (err) {
      toastApiError(err);
    }
  };

  const deleteDeduction = async (d: EmployeeDeduction) => {
    const ok = await confirm({
      title: t('Payroll.employeeDetail.deleteDeductionTitle', { defaultValue: 'Kesintiyi Sil' }),
      message: t('Payroll.employeeDetail.deleteDeductionConfirm', {
        defaultValue: '{{name}} silinsin mi?',
        name: deductionTypeLabel(d),
      }),
      confirmLabel: t('common.delete', { defaultValue: 'Sil' }),
      tone: 'danger',
    });
    if (!ok) return;
    try {
      await removeDeduction.mutateAsync({ id: employeeId, deductionId: d.id });
      toast.success(
        t('Payroll.employeeDetail.deductionDeleted', { defaultValue: 'Kesinti silindi.' }),
      );
    } catch (err) {
      toastApiError(err);
    }
  };

  return {
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
  };
};
