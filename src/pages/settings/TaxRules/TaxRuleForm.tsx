import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Percent } from 'lucide-react';
import { toastApiError } from '@/shared/lib/mutationToast';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { Select } from '@/shared/ui/Select/Select';
import { Textarea } from '@/shared/ui/Textarea/Textarea';
import { useCategoriesQuery, useTaxRatesQuery } from '@/shared/master-data/hooks/useMasterData';
import type {
  TaxRule,
  TaxRuleInput,
  TaxRuleScope,
} from '@/features/pricing/model/pricingRules.types';

interface Props {
  rule: TaxRule | null;
  onClose: () => void;
  onSubmit: (input: TaxRuleInput) => Promise<void>;
}

const SCOPES: TaxRuleScope[] = [
  'Global',
  'Region',
  'ProductClass',
  'RegionAndProductClass',
  'Product',
];

interface State {
  code: string;
  name: string;
  scope: TaxRuleScope;
  ratePercent: string;
  regionCode: string;
  productClass: string;
  productCategoryId: string;
  productId: string;
  fallbackTaxRateId: string;
  validFromUtc: string;
  validUntilUtc: string;
  priority: string;
  isActive: boolean;
  description: string;
}

const buildState = (rule: TaxRule | null): State => ({
  code: rule?.code ?? '',
  name: rule?.name ?? '',
  scope: rule?.scope ?? 'Global',
  ratePercent: rule === null ? '' : String(rule.ratePercent),
  regionCode: rule?.regionCode ?? '',
  productClass: rule?.productClass ?? '',
  productCategoryId: rule?.productCategoryId ?? '',
  productId: rule?.productId ?? '',
  fallbackTaxRateId: rule?.fallbackTaxRateId ?? '',
  validFromUtc: rule?.validFromUtc?.slice(0, 10) ?? '',
  validUntilUtc: rule?.validUntilUtc?.slice(0, 10) ?? '',
  priority: String(rule?.priority ?? 0),
  isActive: rule?.isActive ?? true,
  description: rule?.description ?? '',
});

export const TaxRuleForm = ({ rule, onClose, onSubmit }: Props) => {
  const { t } = useTranslation();
  const rates = useTaxRatesQuery();
  const categories = useCategoriesQuery();
  const [state, setState] = useState<State>(() => buildState(rule));
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    setState(buildState(rule));
  }, [rule]);

  const needsRegion = state.scope === 'Region' || state.scope === 'RegionAndProductClass';
  const needsClass = state.scope === 'ProductClass' || state.scope === 'RegionAndProductClass';

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    try {
      const rate = Number(state.ratePercent);
      if (!Number.isFinite(rate) || rate < 0 || rate > 100) {
        throw new Error(t('Settings.TaxRules.Errors.RateRange'));
      }
      await onSubmit({
        code: state.code.trim(),
        name: state.name.trim(),
        scope: state.scope,
        ratePercent: rate,
        regionCode: needsRegion ? state.regionCode.trim() || null : null,
        productClass: needsClass ? state.productClass.trim() || null : null,
        productCategoryId: needsClass ? state.productCategoryId || null : null,
        productId: state.scope === 'Product' ? state.productId || null : null,
        fallbackTaxRateId: state.fallbackTaxRateId || null,
        validFromUtc: state.validFromUtc ? new Date(state.validFromUtc).toISOString() : null,
        validUntilUtc: state.validUntilUtc ? new Date(state.validUntilUtc).toISOString() : null,
        priority: Number(state.priority) || 0,
        isActive: state.isActive,
        description: state.description.trim() || null,
      });
    } catch (err) {
      toastApiError(err);
    } finally {
      setSaving(false);
    }
  };

  return (
    <Modal
      open={true}
      title={rule ? t('Settings.TaxRules.EditTitle') : t('Settings.TaxRules.NewTitle')}
      icon={<Percent size={18} />}
      onClose={onClose}
      size="lg"
      footer={
        <>
          <Button variant="ghost" type="button" onClick={onClose}>
            {t('Common.Cancel')}
          </Button>
          <Button type="submit" form="tax-rule-form" isLoading={saving}>
            {t('Common.Save')}
          </Button>
        </>
      }
    >
      <form id="tax-rule-form" onSubmit={submit} className="grid grid-cols-1 gap-3 md:grid-cols-2">
        <Input
          label={t('Settings.TaxRules.Code')}
          required
          maxLength={32}
          value={state.code}
          disabled={Boolean(rule)}
          onChange={(e) => setState({ ...state, code: e.target.value })}
        />
        <Input
          label={t('Settings.TaxRules.Name')}
          required
          maxLength={150}
          value={state.name}
          onChange={(e) => setState({ ...state, name: e.target.value })}
        />
        <Select
          label={t('Settings.TaxRules.Scope')}
          value={state.scope}
          onChange={(e) => setState({ ...state, scope: e.target.value as TaxRuleScope })}
        >
          {SCOPES.map((s) => (
            <option key={s} value={s}>
              {t(`Settings.TaxRules.Scopes.${s}`)}
            </option>
          ))}
        </Select>
        <Input
          label={t('Settings.TaxRules.Rate')}
          required
          type="number"
          inputMode="decimal"
          value={state.ratePercent}
          onChange={(e) => setState({ ...state, ratePercent: e.target.value })}
        />
        {needsRegion && (
          <Input
            label={t('Settings.TaxRules.RegionCode')}
            maxLength={32}
            value={state.regionCode}
            onChange={(e) => setState({ ...state, regionCode: e.target.value })}
          />
        )}
        {needsClass && (
          <>
            <Input
              label={t('Settings.TaxRules.ProductClass')}
              maxLength={64}
              value={state.productClass}
              onChange={(e) => setState({ ...state, productClass: e.target.value })}
            />
            <Select
              label={t('Settings.TaxRules.ProductCategory')}
              value={state.productCategoryId}
              onChange={(e) => setState({ ...state, productCategoryId: e.target.value })}
            >
              <option value="">—</option>
              {(categories.data?.data ?? []).map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name}
                </option>
              ))}
            </Select>
          </>
        )}
        {state.scope === 'Product' && (
          <Input
            label={t('Settings.TaxRules.Product')}
            type="text"
            value={state.productId}
            onChange={(e) => setState({ ...state, productId: e.target.value })}
            placeholder="UUID"
          />
        )}
        <Select
          label={t('Settings.TaxRules.Fallback')}
          value={state.fallbackTaxRateId}
          onChange={(e) => setState({ ...state, fallbackTaxRateId: e.target.value })}
        >
          <option value="">—</option>
          {(rates.data?.data ?? []).map((r) => (
            <option key={r.id} value={r.id}>
              {r.name} ({r.ratePercent}%)
            </option>
          ))}
        </Select>
        <Input
          label={t('Settings.TaxRules.Priority')}
          type="number"
          value={state.priority}
          onChange={(e) => setState({ ...state, priority: e.target.value })}
        />
        <Input
          label={t('Settings.TaxRules.ValidFrom')}
          type="date"
          value={state.validFromUtc}
          onChange={(e) => setState({ ...state, validFromUtc: e.target.value })}
        />
        <Input
          label={t('Settings.TaxRules.ValidUntil')}
          type="date"
          value={state.validUntilUtc}
          onChange={(e) => setState({ ...state, validUntilUtc: e.target.value })}
        />
        <div className="flex flex-col gap-1.5">
          <span className="text-sm font-medium text-slate-700 dark:text-slate-200">
            {t('Settings.TaxRules.Status')}
          </span>
          <label className="inline-flex h-10 items-center gap-2 text-sm text-slate-700 dark:text-slate-200">
            <input
              type="checkbox"
              checked={state.isActive}
              onChange={(e) => setState({ ...state, isActive: e.target.checked })}
              className="h-4 w-4 rounded border-slate-300 text-primary-600 focus:ring-primary-500 dark:border-slate-700 dark:bg-slate-900"
            />
            {t(state.isActive ? 'Common.Active' : 'Common.Inactive')}
          </label>
        </div>
        <div className="md:col-span-2">
          <Textarea
            label={t('Settings.TaxRules.Description')}
            maxLength={500}
            rows={2}
            value={state.description}
            onChange={(e) => setState({ ...state, description: e.target.value })}
          />
        </div>
      </form>
    </Modal>
  );
};
