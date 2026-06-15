import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useCategoriesQuery, useTaxRatesQuery } from '@/features/master-data/hooks/useMasterData';
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

const inputCls =
  'w-full rounded border border-slate-300 bg-white px-2 py-1 text-xs text-slate-900 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100';

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
    <div className="fixed inset-0 z-40 flex items-center justify-center bg-slate-900/40 p-4">
      <div className="w-full max-w-lg rounded-lg bg-white p-4 shadow-xl dark:bg-slate-900">
        <h3 className="text-sm font-semibold text-slate-800 dark:text-slate-100">
          {rule ? t('Settings.TaxRules.EditTitle') : t('Settings.TaxRules.NewTitle')}
        </h3>
        <form onSubmit={submit} className="mt-3 grid grid-cols-1 gap-2 text-xs md:grid-cols-2">
          <Field label={t('Settings.TaxRules.Code')}>
            <input
              required
              maxLength={32}
              value={state.code}
              disabled={Boolean(rule)}
              onChange={(e) => setState({ ...state, code: e.target.value })}
              className={inputCls}
            />
          </Field>
          <Field label={t('Settings.TaxRules.Name')}>
            <input
              required
              maxLength={150}
              value={state.name}
              onChange={(e) => setState({ ...state, name: e.target.value })}
              className={inputCls}
            />
          </Field>
          <Field label={t('Settings.TaxRules.Scope')}>
            <select
              value={state.scope}
              onChange={(e) => setState({ ...state, scope: e.target.value as TaxRuleScope })}
              className={inputCls}
            >
              {SCOPES.map((s) => (
                <option key={s} value={s}>
                  {t(`Settings.TaxRules.Scopes.${s}`)}
                </option>
              ))}
            </select>
          </Field>
          <Field label={t('Settings.TaxRules.Rate')}>
            <input
              required
              type="number"
              inputMode="decimal"
              value={state.ratePercent}
              onChange={(e) => setState({ ...state, ratePercent: e.target.value })}
              className={inputCls}
            />
          </Field>
          {needsRegion && (
            <Field label={t('Settings.TaxRules.RegionCode')}>
              <input
                maxLength={32}
                value={state.regionCode}
                onChange={(e) => setState({ ...state, regionCode: e.target.value })}
                className={inputCls}
              />
            </Field>
          )}
          {needsClass && (
            <>
              <Field label={t('Settings.TaxRules.ProductClass')}>
                <input
                  maxLength={64}
                  value={state.productClass}
                  onChange={(e) => setState({ ...state, productClass: e.target.value })}
                  className={inputCls}
                />
              </Field>
              <Field label={t('Settings.TaxRules.ProductCategory')}>
                <select
                  value={state.productCategoryId}
                  onChange={(e) => setState({ ...state, productCategoryId: e.target.value })}
                  className={inputCls}
                >
                  <option value="">—</option>
                  {(categories.data?.data ?? []).map((c) => (
                    <option key={c.id} value={c.id}>
                      {c.name}
                    </option>
                  ))}
                </select>
              </Field>
            </>
          )}
          {state.scope === 'Product' && (
            <Field label={t('Settings.TaxRules.Product')}>
              <input
                type="text"
                value={state.productId}
                onChange={(e) => setState({ ...state, productId: e.target.value })}
                className={inputCls}
                placeholder="UUID"
              />
            </Field>
          )}
          <Field label={t('Settings.TaxRules.Fallback')}>
            <select
              value={state.fallbackTaxRateId}
              onChange={(e) => setState({ ...state, fallbackTaxRateId: e.target.value })}
              className={inputCls}
            >
              <option value="">—</option>
              {(rates.data?.data ?? []).map((r) => (
                <option key={r.id} value={r.id}>
                  {r.name} ({r.ratePercent}%)
                </option>
              ))}
            </select>
          </Field>
          <Field label={t('Settings.TaxRules.Priority')}>
            <input
              type="number"
              value={state.priority}
              onChange={(e) => setState({ ...state, priority: e.target.value })}
              className={inputCls}
            />
          </Field>
          <Field label={t('Settings.TaxRules.ValidFrom')}>
            <input
              type="date"
              value={state.validFromUtc}
              onChange={(e) => setState({ ...state, validFromUtc: e.target.value })}
              className={inputCls}
            />
          </Field>
          <Field label={t('Settings.TaxRules.ValidUntil')}>
            <input
              type="date"
              value={state.validUntilUtc}
              onChange={(e) => setState({ ...state, validUntilUtc: e.target.value })}
              className={inputCls}
            />
          </Field>
          <Field label={t('Settings.TaxRules.Status')}>
            <label className="inline-flex items-center gap-2 text-xs text-slate-700 dark:text-slate-200">
              <input
                type="checkbox"
                checked={state.isActive}
                onChange={(e) => setState({ ...state, isActive: e.target.checked })}
                className="h-4 w-4 rounded border-slate-300 text-indigo-600 focus:ring-indigo-500 dark:border-slate-700 dark:bg-slate-900"
              />
              {t(state.isActive ? 'Common.Active' : 'Common.Inactive')}
            </label>
          </Field>
          <div className="md:col-span-2">
            <label className="block text-[11px] font-medium text-slate-600 dark:text-slate-300">
              {t('Settings.TaxRules.Description')}
            </label>
            <textarea
              maxLength={500}
              rows={2}
              value={state.description}
              onChange={(e) => setState({ ...state, description: e.target.value })}
              className={`mt-0.5 ${inputCls}`}
            />
          </div>
          <div className="mt-3 flex justify-end gap-2 md:col-span-2">
            <button
              type="button"
              onClick={onClose}
              className="rounded border border-slate-300 px-3 py-1 text-xs text-slate-600 hover:bg-slate-50 dark:border-slate-700 dark:text-slate-300 dark:hover:bg-slate-800"
            >
              {t('Common.Cancel')}
            </button>
            <button
              type="submit"
              disabled={saving}
              className="rounded bg-indigo-600 px-3 py-1 text-xs font-medium text-white hover:bg-indigo-700 disabled:opacity-50 dark:bg-indigo-500 dark:hover:bg-indigo-600"
            >
              {t('Common.Save')}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

const Field = ({ label, children }: { label: string; children: React.ReactNode }) => (
  <div>
    <label className="block text-[11px] font-medium text-slate-600 dark:text-slate-300">
      {label}
    </label>
    <div className="mt-0.5">{children}</div>
  </div>
);
