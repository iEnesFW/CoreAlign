import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toastApiError } from '@/shared/lib/mutationToast';
import {
  useCategoriesQuery,
  useCustomerGroupsQuery,
} from '@/features/master-data/hooks/useMasterData';
import type {
  DiscountRule,
  DiscountRuleInput,
  DiscountRuleScope,
  DiscountValueType,
} from '@/features/pricing/model/pricingRules.types';

interface Props {
  rule: DiscountRule | null;
  onClose: () => void;
  onSubmit: (input: DiscountRuleInput) => Promise<void>;
}

const SCOPES: DiscountRuleScope[] = ['Global', 'CustomerGroup', 'ProductCategory', 'Product'];
const VALUE_TYPES: DiscountValueType[] = ['Percent', 'FixedAmount'];

interface State {
  code: string;
  name: string;
  scope: DiscountRuleScope;
  valueType: DiscountValueType;
  value: string;
  customerGroupId: string;
  productCategoryId: string;
  productId: string;
  validFromUtc: string;
  validUntilUtc: string;
  minQuantity: string;
  priority: string;
  isActive: boolean;
  description: string;
}

const buildState = (rule: DiscountRule | null): State => ({
  code: rule?.code ?? '',
  name: rule?.name ?? '',
  scope: rule?.scope ?? 'Global',
  valueType: rule?.valueType ?? 'Percent',
  value: rule === null ? '' : String(rule.value),
  customerGroupId: rule?.customerGroupId ?? '',
  productCategoryId: rule?.productCategoryId ?? '',
  productId: rule?.productId ?? '',
  validFromUtc: rule?.validFromUtc?.slice(0, 10) ?? '',
  validUntilUtc: rule?.validUntilUtc?.slice(0, 10) ?? '',
  minQuantity:
    rule?.minQuantity === null || rule?.minQuantity === undefined ? '' : String(rule.minQuantity),
  priority: String(rule?.priority ?? 0),
  isActive: rule?.isActive ?? true,
  description: rule?.description ?? '',
});

export const DiscountRuleForm = ({ rule, onClose, onSubmit }: Props) => {
  const { t } = useTranslation();
  const groups = useCustomerGroupsQuery();
  const categories = useCategoriesQuery();
  const [state, setState] = useState<State>(() => buildState(rule));
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    setState(buildState(rule));
  }, [rule]);

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    try {
      const value = Number(state.value);
      if (!Number.isFinite(value)) {
        throw new Error(t('Settings.DiscountRules.Errors.InvalidValue'));
      }
      await onSubmit({
        code: state.code.trim(),
        name: state.name.trim(),
        scope: state.scope,
        valueType: state.valueType,
        value,
        customerGroupId: state.scope === 'CustomerGroup' ? state.customerGroupId || null : null,
        productCategoryId:
          state.scope === 'ProductCategory' ? state.productCategoryId || null : null,
        productId: state.scope === 'Product' ? state.productId || null : null,
        validFromUtc: state.validFromUtc ? new Date(state.validFromUtc).toISOString() : null,
        validUntilUtc: state.validUntilUtc ? new Date(state.validUntilUtc).toISOString() : null,
        minQuantity: state.minQuantity ? Number(state.minQuantity) : null,
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
          {rule ? t('Settings.DiscountRules.EditTitle') : t('Settings.DiscountRules.NewTitle')}
        </h3>
        <form onSubmit={submit} className="mt-3 grid grid-cols-1 gap-2 text-xs md:grid-cols-2">
          <Field label={t('Settings.DiscountRules.Code')}>
            <input
              required
              maxLength={32}
              value={state.code}
              disabled={Boolean(rule)}
              onChange={(e) => setState({ ...state, code: e.target.value })}
              className="w-full rounded border border-slate-300 bg-white px-2 py-1 text-xs text-slate-900 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
            />
          </Field>
          <Field label={t('Settings.DiscountRules.Name')}>
            <input
              required
              maxLength={150}
              value={state.name}
              onChange={(e) => setState({ ...state, name: e.target.value })}
              className="w-full rounded border border-slate-300 bg-white px-2 py-1 text-xs text-slate-900 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
            />
          </Field>
          <Field label={t('Settings.DiscountRules.Scope')}>
            <select
              value={state.scope}
              onChange={(e) => setState({ ...state, scope: e.target.value as DiscountRuleScope })}
              className="w-full rounded border border-slate-300 bg-white px-2 py-1 text-xs text-slate-900 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
            >
              {SCOPES.map((s) => (
                <option key={s} value={s}>
                  {t(`Settings.DiscountRules.Scopes.${s}`)}
                </option>
              ))}
            </select>
          </Field>
          <Field label={t('Settings.DiscountRules.ValueType')}>
            <select
              value={state.valueType}
              onChange={(e) =>
                setState({ ...state, valueType: e.target.value as DiscountValueType })
              }
              className="w-full rounded border border-slate-300 bg-white px-2 py-1 text-xs text-slate-900 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
            >
              {VALUE_TYPES.map((v) => (
                <option key={v} value={v}>
                  {t(`Settings.DiscountRules.ValueTypes.${v}`)}
                </option>
              ))}
            </select>
          </Field>
          <Field label={t('Settings.DiscountRules.Value')}>
            <input
              required
              type="number"
              inputMode="decimal"
              value={state.value}
              onChange={(e) => setState({ ...state, value: e.target.value })}
              className="w-full rounded border border-slate-300 bg-white px-2 py-1 text-xs text-slate-900 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
            />
          </Field>
          <Field label={t('Settings.DiscountRules.MinQuantity')}>
            <input
              type="number"
              inputMode="decimal"
              value={state.minQuantity}
              onChange={(e) => setState({ ...state, minQuantity: e.target.value })}
              className="w-full rounded border border-slate-300 bg-white px-2 py-1 text-xs text-slate-900 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
            />
          </Field>
          {state.scope === 'CustomerGroup' && (
            <Field label={t('Settings.DiscountRules.CustomerGroup')}>
              <select
                value={state.customerGroupId}
                onChange={(e) => setState({ ...state, customerGroupId: e.target.value })}
                className="w-full rounded border border-slate-300 bg-white px-2 py-1 text-xs text-slate-900 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
              >
                <option value="">—</option>
                {(groups.data?.data ?? []).map((g) => (
                  <option key={g.id} value={g.id}>
                    {g.name}
                  </option>
                ))}
              </select>
            </Field>
          )}
          {state.scope === 'ProductCategory' && (
            <Field label={t('Settings.DiscountRules.ProductCategory')}>
              <select
                value={state.productCategoryId}
                onChange={(e) => setState({ ...state, productCategoryId: e.target.value })}
                className="w-full rounded border border-slate-300 bg-white px-2 py-1 text-xs text-slate-900 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
              >
                <option value="">—</option>
                {(categories.data?.data ?? []).map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.name}
                  </option>
                ))}
              </select>
            </Field>
          )}
          {state.scope === 'Product' && (
            <Field label={t('Settings.DiscountRules.Product')}>
              <input
                type="text"
                value={state.productId}
                onChange={(e) => setState({ ...state, productId: e.target.value })}
                className="w-full rounded border border-slate-300 bg-white px-2 py-1 text-xs text-slate-900 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
                placeholder="UUID"
              />
            </Field>
          )}
          <Field label={t('Settings.DiscountRules.Priority')}>
            <input
              type="number"
              value={state.priority}
              onChange={(e) => setState({ ...state, priority: e.target.value })}
              className="w-full rounded border border-slate-300 bg-white px-2 py-1 text-xs text-slate-900 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
            />
          </Field>
          <Field label={t('Settings.DiscountRules.ValidFrom')}>
            <input
              type="date"
              value={state.validFromUtc}
              onChange={(e) => setState({ ...state, validFromUtc: e.target.value })}
              className="w-full rounded border border-slate-300 bg-white px-2 py-1 text-xs text-slate-900 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
            />
          </Field>
          <Field label={t('Settings.DiscountRules.ValidUntil')}>
            <input
              type="date"
              value={state.validUntilUtc}
              onChange={(e) => setState({ ...state, validUntilUtc: e.target.value })}
              className="w-full rounded border border-slate-300 bg-white px-2 py-1 text-xs text-slate-900 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
            />
          </Field>
          <Field label={t('Settings.DiscountRules.Status')}>
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
              {t('Settings.DiscountRules.Description')}
            </label>
            <textarea
              maxLength={500}
              rows={2}
              value={state.description}
              onChange={(e) => setState({ ...state, description: e.target.value })}
              className="mt-0.5 w-full rounded border border-slate-300 bg-white px-2 py-1 text-xs text-slate-900 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
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
