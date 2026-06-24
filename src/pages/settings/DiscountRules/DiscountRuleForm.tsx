import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Percent } from 'lucide-react';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { Select } from '@/shared/ui/Select/Select';
import { Textarea } from '@/shared/ui/Textarea/Textarea';
import { toastApiError } from '@/shared/lib/mutationToast';
import {
  useCategoriesQuery,
  useCustomerGroupsQuery,
} from '@/shared/master-data/hooks/useMasterData';
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
    <Modal
      open
      title={rule ? t('Settings.DiscountRules.EditTitle') : t('Settings.DiscountRules.NewTitle')}
      icon={<Percent size={18} />}
      onClose={onClose}
      size="lg"
      footer={
        <>
          <Button variant="ghost" type="button" onClick={onClose}>
            {t('Common.Cancel')}
          </Button>
          <Button type="submit" form="discount-rule-form" isLoading={saving}>
            {t('Common.Save')}
          </Button>
        </>
      }
    >
      <form
        id="discount-rule-form"
        onSubmit={submit}
        className="grid grid-cols-1 gap-3 md:grid-cols-2"
      >
        <Input
          label={t('Settings.DiscountRules.Code')}
          required
          maxLength={32}
          value={state.code}
          disabled={Boolean(rule)}
          onChange={(e) => setState({ ...state, code: e.target.value })}
        />
        <Input
          label={t('Settings.DiscountRules.Name')}
          required
          maxLength={150}
          value={state.name}
          onChange={(e) => setState({ ...state, name: e.target.value })}
        />
        <Select
          label={t('Settings.DiscountRules.Scope')}
          value={state.scope}
          onChange={(e) => setState({ ...state, scope: e.target.value as DiscountRuleScope })}
        >
          {SCOPES.map((s) => (
            <option key={s} value={s}>
              {t(`Settings.DiscountRules.Scopes.${s}`)}
            </option>
          ))}
        </Select>
        <Select
          label={t('Settings.DiscountRules.ValueType')}
          value={state.valueType}
          onChange={(e) => setState({ ...state, valueType: e.target.value as DiscountValueType })}
        >
          {VALUE_TYPES.map((v) => (
            <option key={v} value={v}>
              {t(`Settings.DiscountRules.ValueTypes.${v}`)}
            </option>
          ))}
        </Select>
        <Input
          label={t('Settings.DiscountRules.Value')}
          required
          type="number"
          inputMode="decimal"
          value={state.value}
          onChange={(e) => setState({ ...state, value: e.target.value })}
        />
        <Input
          label={t('Settings.DiscountRules.MinQuantity')}
          type="number"
          inputMode="decimal"
          value={state.minQuantity}
          onChange={(e) => setState({ ...state, minQuantity: e.target.value })}
        />
        {state.scope === 'CustomerGroup' && (
          <Select
            label={t('Settings.DiscountRules.CustomerGroup')}
            value={state.customerGroupId}
            onChange={(e) => setState({ ...state, customerGroupId: e.target.value })}
          >
            <option value="">—</option>
            {(groups.data?.data ?? []).map((g) => (
              <option key={g.id} value={g.id}>
                {g.name}
              </option>
            ))}
          </Select>
        )}
        {state.scope === 'ProductCategory' && (
          <Select
            label={t('Settings.DiscountRules.ProductCategory')}
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
        )}
        {state.scope === 'Product' && (
          <Input
            label={t('Settings.DiscountRules.Product')}
            type="text"
            value={state.productId}
            onChange={(e) => setState({ ...state, productId: e.target.value })}
            placeholder="UUID"
          />
        )}
        <Input
          label={t('Settings.DiscountRules.Priority')}
          type="number"
          value={state.priority}
          onChange={(e) => setState({ ...state, priority: e.target.value })}
        />
        <Input
          label={t('Settings.DiscountRules.ValidFrom')}
          type="date"
          value={state.validFromUtc}
          onChange={(e) => setState({ ...state, validFromUtc: e.target.value })}
        />
        <Input
          label={t('Settings.DiscountRules.ValidUntil')}
          type="date"
          value={state.validUntilUtc}
          onChange={(e) => setState({ ...state, validUntilUtc: e.target.value })}
        />
        <div className="flex flex-col gap-1.5">
          <span className="text-sm font-medium text-slate-700 dark:text-slate-300">
            {t('Settings.DiscountRules.Status')}
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
        <Textarea
          className="md:col-span-2"
          label={t('Settings.DiscountRules.Description')}
          maxLength={500}
          rows={2}
          value={state.description}
          onChange={(e) => setState({ ...state, description: e.target.value })}
        />
      </form>
    </Modal>
  );
};
