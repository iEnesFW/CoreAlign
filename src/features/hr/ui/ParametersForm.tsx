import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Plus, SlidersHorizontal, Trash2 } from 'lucide-react';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { toastApiError } from '@/shared/lib/mutationToast';
import {
  useCreatePayrollParameters,
  useUpdatePayrollParameters,
} from '../hooks/usePayrollParameters';
import type { PayrollParameters, PayrollTaxBracket } from '../model/parameters.types';

interface Props {
  parameters: PayrollParameters | null;
  onClose: () => void;
}

interface BracketState {
  key: string;
  upperBound: string;
  ratePercent: string;
}

const todayIso = () => new Date().toISOString().slice(0, 10);

const fieldClass =
  'mt-1 w-full rounded border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100';
const labelClass = 'block text-xs font-medium text-slate-700 dark:text-slate-300';

const toBracketState = (b: PayrollTaxBracket): BracketState => ({
  key: crypto.randomUUID(),
  upperBound: b.upperBound === null ? '' : String(b.upperBound),
  ratePercent: String(b.ratePercent),
});

const newBracket = (): BracketState => ({
  key: crypto.randomUUID(),
  upperBound: '',
  ratePercent: '',
});

export const ParametersForm = ({ parameters, onClose }: Props) => {
  const { t } = useTranslation();
  const isEdit = parameters !== null;

  const createMutation = useCreatePayrollParameters();
  const updateMutation = useUpdatePayrollParameters();

  const [description, setDescription] = useState(parameters?.description ?? '');
  const [effectiveYear, setEffectiveYear] = useState(
    String(parameters?.effectiveYear ?? new Date().getFullYear()),
  );
  const [effectiveFrom, setEffectiveFrom] = useState(
    parameters?.effectiveFrom?.slice(0, 10) ?? todayIso(),
  );
  const [effectiveTo, setEffectiveTo] = useState(parameters?.effectiveTo?.slice(0, 10) ?? '');
  const [isActive, setIsActive] = useState(parameters?.isActive ?? true);
  const [grossMinimumWage, setGrossMinimumWage] = useState(
    parameters ? String(parameters.grossMinimumWage) : '',
  );
  const [minWageExemptionEnabled, setMinWageExemptionEnabled] = useState(
    parameters?.minWageExemptionEnabled ?? true,
  );
  const [sgkEmployeeRate, setSgkEmployeeRate] = useState(
    parameters ? String(parameters.sgkEmployeeRate) : '14',
  );
  const [sgkEmployerRate, setSgkEmployerRate] = useState(
    parameters ? String(parameters.sgkEmployerRate) : '20.5',
  );
  const [sgkEmployer5PointIncentiveRate, setSgkEmployer5PointIncentiveRate] = useState(
    parameters ? String(parameters.sgkEmployer5PointIncentiveRate) : '5',
  );
  const [unemploymentEmployeeRate, setUnemploymentEmployeeRate] = useState(
    parameters ? String(parameters.unemploymentEmployeeRate) : '1',
  );
  const [unemploymentEmployerRate, setUnemploymentEmployerRate] = useState(
    parameters ? String(parameters.unemploymentEmployerRate) : '2',
  );
  const [stampTaxRate, setStampTaxRate] = useState(
    parameters ? String(parameters.stampTaxRate) : '0.759',
  );
  const [sgkFloorMonthly, setSgkFloorMonthly] = useState(
    parameters ? String(parameters.sgkFloorMonthly) : '',
  );
  const [sgkCeilingMultiplier, setSgkCeilingMultiplier] = useState(
    parameters ? String(parameters.sgkCeilingMultiplier) : '7.5',
  );
  const [sgkCeilingMonthly, setSgkCeilingMonthly] = useState(
    parameters ? String(parameters.sgkCeilingMonthly) : '',
  );
  const [disability1Amount, setDisability1Amount] = useState(
    parameters ? String(parameters.disability1Amount) : '0',
  );
  const [disability2Amount, setDisability2Amount] = useState(
    parameters ? String(parameters.disability2Amount) : '0',
  );
  const [disability3Amount, setDisability3Amount] = useState(
    parameters ? String(parameters.disability3Amount) : '0',
  );
  const [brackets, setBrackets] = useState<BracketState[]>(
    parameters && parameters.taxBrackets.length > 0
      ? parameters.taxBrackets.map(toBracketState)
      : [newBracket()],
  );

  const pending = createMutation.isPending || updateMutation.isPending;

  const updateBracket = (key: string, patch: Partial<BracketState>) =>
    setBrackets((prev) => prev.map((b) => (b.key === key ? { ...b, ...patch } : b)));
  const addBracket = () => setBrackets((prev) => [...prev, newBracket()]);
  const removeBracket = (key: string) =>
    setBrackets((prev) => (prev.length === 1 ? prev : prev.filter((b) => b.key !== key)));

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!(Number(grossMinimumWage) > 0)) {
      toast.error(
        t('Payroll.parametersForm.minWageRequired', {
          defaultValue: 'Geçerli bir asgari ücret girin.',
        }),
      );
      return;
    }

    const sharedRates = {
      sgkEmployeeRate: Number(sgkEmployeeRate) || 0,
      sgkEmployerRate: Number(sgkEmployerRate) || 0,
      sgkEmployer5PointIncentiveRate: Number(sgkEmployer5PointIncentiveRate) || 0,
      unemploymentEmployeeRate: Number(unemploymentEmployeeRate) || 0,
      unemploymentEmployerRate: Number(unemploymentEmployerRate) || 0,
      sgkFloorMonthly: Number(sgkFloorMonthly) || 0,
      sgkCeilingMultiplier: Number(sgkCeilingMultiplier) || 0,
      sgkCeilingMonthly: Number(sgkCeilingMonthly) || 0,
      stampTaxRate: Number(stampTaxRate) || 0,
      grossMinimumWage: Number(grossMinimumWage),
      disability1Amount: Number(disability1Amount) || 0,
      disability2Amount: Number(disability2Amount) || 0,
      disability3Amount: Number(disability3Amount) || 0,
      minWageExemptionEnabled,
      effectiveTo: effectiveTo || null,
      description: description.trim() || null,
    };

    try {
      if (isEdit && parameters) {
        await updateMutation.mutateAsync({
          id: parameters.id,
          ...sharedRates,
          isActive,
          effectiveFrom,
        });
        toast.success(
          t('Payroll.parametersForm.updated', { defaultValue: 'Parametreler güncellendi.' }),
        );
      } else {
        const taxBrackets = brackets
          .filter((b) => b.ratePercent !== '')
          .map((b, index) => ({
            ratePercent: Number(b.ratePercent) || 0,
            sortOrder: index,
            upperBound: b.upperBound === '' ? null : Number(b.upperBound),
          }));
        await createMutation.mutateAsync({
          effectiveYear: Number(effectiveYear) || new Date().getFullYear(),
          effectiveFrom,
          ...sharedRates,
          taxBrackets,
        });
        toast.success(
          t('Payroll.parametersForm.created', { defaultValue: 'Parametre seti oluşturuldu.' }),
        );
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
      icon={<SlidersHorizontal size={16} />}
      title={
        isEdit
          ? t('Payroll.parametersForm.editTitle', {
              defaultValue: 'Bordro Parametrelerini Düzenle',
            })
          : t('Payroll.parametersForm.newTitle', { defaultValue: 'Yeni Parametre Seti' })
      }
      footer={
        <>
          <Button variant="outline" size="sm" type="button" onClick={onClose}>
            {t('common.cancel', { defaultValue: 'İptal' })}
          </Button>
          <Button size="sm" type="submit" form="parameters-form" isLoading={pending}>
            {t('common.save', { defaultValue: 'Kaydet' })}
          </Button>
        </>
      }
    >
      <form id="parameters-form" onSubmit={submit} className="space-y-4">
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
          <div className="sm:col-span-2">
            <label className={labelClass}>
              {t('Payroll.parametersForm.description', { defaultValue: 'Açıklama' })}
            </label>
            <input
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              className={fieldClass}
            />
          </div>
          <div>
            <label className={labelClass}>
              {t('Payroll.parametersForm.effectiveYear', { defaultValue: 'Geçerlilik Yılı' })}
            </label>
            <input
              type="number"
              min={2000}
              max={2100}
              value={effectiveYear}
              onChange={(e) => setEffectiveYear(e.target.value)}
              className={`${fieldClass} text-right`}
              disabled={isEdit}
            />
          </div>
          <div>
            <label className={labelClass}>
              {t('Payroll.parametersForm.effectiveFrom', { defaultValue: 'Geçerlilik Başlangıcı' })}
            </label>
            <input
              type="date"
              value={effectiveFrom}
              onChange={(e) => setEffectiveFrom(e.target.value)}
              className={fieldClass}
            />
          </div>
          <div>
            <label className={labelClass}>
              {t('Payroll.parametersForm.effectiveTo', { defaultValue: 'Geçerlilik Bitişi' })}
            </label>
            <input
              type="date"
              value={effectiveTo}
              onChange={(e) => setEffectiveTo(e.target.value)}
              className={fieldClass}
            />
          </div>
          <div>
            <label className={labelClass}>
              {t('Payroll.parametersForm.minimumWage', { defaultValue: 'Asgari Ücret (Brüt)' })} *
            </label>
            <input
              type="number"
              min={0}
              step="any"
              value={grossMinimumWage}
              onChange={(e) => setGrossMinimumWage(e.target.value)}
              className={`${fieldClass} text-right`}
            />
          </div>
        </div>

        <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
          <Rate
            label={t('Payroll.parametersForm.sgkEmployee', { defaultValue: 'SGK İşçi %' })}
            value={sgkEmployeeRate}
            onChange={setSgkEmployeeRate}
          />
          <Rate
            label={t('Payroll.parametersForm.sgkEmployer', { defaultValue: 'SGK İşveren %' })}
            value={sgkEmployerRate}
            onChange={setSgkEmployerRate}
          />
          <Rate
            label={t('Payroll.parametersForm.sgkIncentive', {
              defaultValue: 'SGK 5 Puan Teşvik %',
            })}
            value={sgkEmployer5PointIncentiveRate}
            onChange={setSgkEmployer5PointIncentiveRate}
          />
          <Rate
            label={t('Payroll.parametersForm.unemploymentEmployee', {
              defaultValue: 'İşsizlik İşçi %',
            })}
            value={unemploymentEmployeeRate}
            onChange={setUnemploymentEmployeeRate}
          />
          <Rate
            label={t('Payroll.parametersForm.unemploymentEmployer', {
              defaultValue: 'İşsizlik İşveren %',
            })}
            value={unemploymentEmployerRate}
            onChange={setUnemploymentEmployerRate}
          />
          <Rate
            label={t('Payroll.parametersForm.stampTax', { defaultValue: 'Damga Vergisi %' })}
            value={stampTaxRate}
            onChange={setStampTaxRate}
          />
        </div>

        <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
          <Rate
            label={t('Payroll.parametersForm.sgkFloorMonthly', {
              defaultValue: 'SGK Taban Matrahı',
            })}
            value={sgkFloorMonthly}
            onChange={setSgkFloorMonthly}
          />
          <Rate
            label={t('Payroll.parametersForm.sgkCeilingMultiplier', {
              defaultValue: 'SGK Tavan Katsayısı',
            })}
            value={sgkCeilingMultiplier}
            onChange={setSgkCeilingMultiplier}
          />
          <Rate
            label={t('Payroll.parametersForm.sgkCeilingMonthly', {
              defaultValue: 'SGK Tavan Matrahı',
            })}
            value={sgkCeilingMonthly}
            onChange={setSgkCeilingMonthly}
          />
          <Rate
            label={t('Payroll.parametersForm.disability1', {
              defaultValue: 'Engellilik 1. Derece',
            })}
            value={disability1Amount}
            onChange={setDisability1Amount}
          />
          <Rate
            label={t('Payroll.parametersForm.disability2', {
              defaultValue: 'Engellilik 2. Derece',
            })}
            value={disability2Amount}
            onChange={setDisability2Amount}
          />
          <Rate
            label={t('Payroll.parametersForm.disability3', {
              defaultValue: 'Engellilik 3. Derece',
            })}
            value={disability3Amount}
            onChange={setDisability3Amount}
          />
        </div>

        <div className="flex flex-wrap gap-4 text-xs text-slate-700 dark:text-slate-300">
          <label className="inline-flex items-center gap-1.5">
            <input
              type="checkbox"
              checked={minWageExemptionEnabled}
              onChange={(e) => setMinWageExemptionEnabled(e.target.checked)}
            />
            {t('Payroll.parametersForm.minWageExemption', {
              defaultValue: 'Asgari ücret gelir/damga vergisi istisnası uygula',
            })}
          </label>
          {isEdit && (
            <label className="inline-flex items-center gap-1.5">
              <input
                type="checkbox"
                checked={isActive}
                onChange={(e) => setIsActive(e.target.checked)}
              />
              {t('Payroll.parametersForm.isActive', { defaultValue: 'Aktif' })}
            </label>
          )}
        </div>

        {!isEdit && (
          <div>
            <div className="mb-1.5 flex items-center justify-between">
              <span className="text-xs font-semibold text-slate-700 dark:text-slate-300">
                {t('Payroll.parametersForm.brackets', { defaultValue: 'Gelir Vergisi Dilimleri' })}
              </span>
              <button
                type="button"
                onClick={addBracket}
                className="inline-flex items-center gap-1 rounded border border-slate-200 bg-white px-2 py-1 text-[11px] font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
              >
                <Plus size={11} />
                {t('Payroll.parametersForm.addBracket', { defaultValue: 'Dilim ekle' })}
              </button>
            </div>
            <div className="overflow-hidden rounded-lg border border-slate-200 dark:border-slate-800">
              <table className="w-full text-sm">
                <thead className="bg-slate-50 text-[10px] font-semibold uppercase text-slate-600 dark:bg-slate-800/50 dark:text-slate-300">
                  <tr>
                    <th className="px-2 py-1.5 text-right">
                      {t('Payroll.parametersForm.upperBound', {
                        defaultValue: 'Üst Sınır (boş = sınırsız)',
                      })}
                    </th>
                    <th className="w-28 px-2 py-1.5 text-right">
                      {t('Payroll.parametersForm.ratePercent', { defaultValue: 'Oran %' })}
                    </th>
                    <th className="w-8 px-2 py-1.5" />
                  </tr>
                </thead>
                <tbody>
                  {brackets.map((b) => (
                    <tr key={b.key} className="border-t border-slate-100 dark:border-slate-800">
                      <td className="px-2 py-1.5">
                        <input
                          type="number"
                          min={0}
                          step="any"
                          value={b.upperBound}
                          onChange={(e) => updateBracket(b.key, { upperBound: e.target.value })}
                          placeholder="∞"
                          className="w-full rounded border border-slate-200 bg-white px-2 py-1.5 text-right text-sm dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
                        />
                      </td>
                      <td className="px-2 py-1.5">
                        <input
                          type="number"
                          min={0}
                          max={100}
                          step="any"
                          value={b.ratePercent}
                          onChange={(e) => updateBracket(b.key, { ratePercent: e.target.value })}
                          className="w-full rounded border border-slate-200 bg-white px-2 py-1.5 text-right text-sm dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
                        />
                      </td>
                      <td className="px-2 py-1.5 text-center">
                        <button
                          type="button"
                          onClick={() => removeBracket(b.key)}
                          disabled={brackets.length === 1}
                          className="rounded p-1 text-slate-400 hover:bg-danger-50 hover:text-danger-700 disabled:opacity-30 dark:hover:bg-danger-500/10"
                          aria-label={t('common.delete', { defaultValue: 'Sil' })}
                        >
                          <Trash2 size={13} />
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        )}
      </form>
    </Modal>
  );
};

const Rate = ({
  label,
  value,
  onChange,
}: {
  label: string;
  value: string;
  onChange: (v: string) => void;
}) => (
  <div>
    <label className={labelClass}>{label}</label>
    <input
      type="number"
      min={0}
      step="any"
      value={value}
      onChange={(e) => onChange(e.target.value)}
      className={`${fieldClass} text-right`}
    />
  </div>
);
