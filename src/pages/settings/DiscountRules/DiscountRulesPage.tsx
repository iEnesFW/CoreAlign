import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import type { TFunction } from 'i18next';
import { Pencil, Percent, Plus, Power, Trash2 } from 'lucide-react';
import { toast } from 'sonner';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useConfirm } from '@/shared/ui/ConfirmDialog/useConfirm';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { ListPageTemplate } from '@/shared/ui/PageTemplate/PageTemplate';
import { Button } from '@/shared/ui/Button/Button';
import { Badge } from '@/shared/ui/Badge/Badge';
import {
  useCreateDiscountRule,
  useDeleteDiscountRule,
  useDiscountRulesQuery,
  useUpdateDiscountRule,
} from '@/features/pricing/hooks/usePricingRulesQueries';
import type {
  DiscountRule,
  DiscountRuleScope,
  DiscountValueType,
} from '@/features/pricing/model/pricingRules.types';
import {
  useCustomerGroupsQuery,
  useCategoriesQuery,
} from '@/shared/master-data/hooks/useMasterData';
import { DiscountRuleForm } from './DiscountRuleForm';

export const DiscountRulesPage = () => {
  const { t } = useTranslation();
  const confirm = useConfirm();
  const rulesQ = useDiscountRulesQuery();
  const groupsQ = useCustomerGroupsQuery();
  const categoriesQ = useCategoriesQuery();
  const createMut = useCreateDiscountRule();
  const updateMut = useUpdateDiscountRule();
  const deleteMut = useDeleteDiscountRule();
  const [editing, setEditing] = useState<DiscountRule | 'new' | null>(null);

  const rules = rulesQ.data?.data ?? [];
  const groupNameById = useMemo(() => {
    const map = new Map<string, string>();
    for (const g of groupsQ.data?.data ?? []) map.set(g.id, g.name);
    return map;
  }, [groupsQ.data]);
  const categoryNameById = useMemo(() => {
    const map = new Map<string, string>();
    for (const c of categoriesQ.data?.data ?? []) map.set(c.id, c.name);
    return map;
  }, [categoriesQ.data]);

  const handleDelete = async (rule: DiscountRule) => {
    const ok = await confirm({
      title: t('Settings.DiscountRules.DeleteTitle'),
      message: t('Settings.DiscountRules.DeleteMessage', { code: rule.code }),
      confirmLabel: t('Common.Delete'),
    });
    if (!ok) return;
    try {
      await deleteMut.mutateAsync(rule.id);
      toast.success(t('Settings.DiscountRules.Deleted'));
    } catch (err) {
      toastApiError(err);
    }
  };

  const handleToggleActive = async (rule: DiscountRule) => {
    try {
      await updateMut.mutateAsync({
        id: rule.id,
        name: rule.name,
        scope: rule.scope,
        valueType: rule.valueType,
        value: rule.value,
        customerGroupId: rule.customerGroupId,
        productCategoryId: rule.productCategoryId,
        productId: rule.productId,
        validFromUtc: rule.validFromUtc,
        validUntilUtc: rule.validUntilUtc,
        minQuantity: rule.minQuantity,
        priority: rule.priority,
        isActive: !rule.isActive,
        description: rule.description,
      });
      toast.success(
        t(
          rule.isActive ? 'Settings.DiscountRules.Deactivated' : 'Settings.DiscountRules.Activated',
        ),
      );
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <ListPageTemplate
      header={
        <PageHeader
          icon={<Percent size={20} />}
          title={t('Settings.DiscountRules.Title')}
          subtitle={t('Settings.DiscountRules.Subtitle')}
          actions={
            <Button size="sm" onClick={() => setEditing('new')}>
              <Plus size={14} />
              {t('Settings.DiscountRules.New')}
            </Button>
          }
        />
      }
    >
      <div className="overflow-x-auto rounded border border-slate-200 dark:border-slate-700">
        <table className="min-w-full text-xs">
          <thead className="bg-slate-50 text-left text-slate-500 dark:bg-slate-800 dark:text-slate-400">
            <tr>
              <th className="px-2 py-1.5 font-medium">{t('Settings.DiscountRules.Code')}</th>
              <th className="px-2 py-1.5 font-medium">{t('Settings.DiscountRules.Name')}</th>
              <th className="px-2 py-1.5 font-medium">{t('Settings.DiscountRules.Scope')}</th>
              <th className="px-2 py-1.5 font-medium">{t('Settings.DiscountRules.Target')}</th>
              <th className="px-2 py-1.5 text-right font-medium">
                {t('Settings.DiscountRules.Value')}
              </th>
              <th className="px-2 py-1.5 text-right font-medium">
                {t('Settings.DiscountRules.Priority')}
              </th>
              <th className="px-2 py-1.5 font-medium">{t('Settings.DiscountRules.Validity')}</th>
              <th className="px-2 py-1.5 font-medium">{t('Settings.DiscountRules.Status')}</th>
              <th className="px-2 py-1.5"></th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
            {rules.map((rule) => (
              <tr key={rule.id} className="bg-white dark:bg-slate-900">
                <td className="px-2 py-1.5 font-mono text-slate-700 dark:text-slate-200">
                  {rule.code}
                </td>
                <td className="px-2 py-1.5 text-slate-700 dark:text-slate-200">{rule.name}</td>
                <td className="px-2 py-1.5 text-slate-600 dark:text-slate-300">
                  {t(`Settings.DiscountRules.Scopes.${rule.scope}`)}
                </td>
                <td className="px-2 py-1.5 text-slate-600 dark:text-slate-300">
                  {scopeTarget(rule, groupNameById, categoryNameById)}
                </td>
                <td className="px-2 py-1.5 text-right text-slate-800 dark:text-slate-100">
                  {rule.valueType === 'Percent' ? `${rule.value}%` : rule.value}
                </td>
                <td className="px-2 py-1.5 text-right text-slate-600 dark:text-slate-300">
                  {rule.priority}
                </td>
                <td className="px-2 py-1.5 text-slate-500 dark:text-slate-400">
                  {validityLabel(rule)}
                </td>
                <td className="px-2 py-1.5">
                  <StatusBadge active={rule.isActive} t={t} />
                </td>
                <td className="px-2 py-1.5 text-right whitespace-nowrap">
                  <button
                    type="button"
                    onClick={() => handleToggleActive(rule)}
                    className={`rounded p-1 ${
                      rule.isActive
                        ? 'text-warning-600 hover:bg-warning-50 dark:hover:bg-warning-900/30'
                        : 'text-success-600 hover:bg-success-50 dark:hover:bg-success-900/30'
                    }`}
                    aria-label={t(rule.isActive ? 'Common.Deactivate' : 'Common.Activate')}
                  >
                    <Power size={12} />
                  </button>
                  <button
                    type="button"
                    onClick={() => setEditing(rule)}
                    className="rounded p-1 text-slate-500 hover:bg-slate-100 dark:hover:bg-slate-800"
                    aria-label={t('Common.Edit')}
                  >
                    <Pencil size={12} />
                  </button>
                  <button
                    type="button"
                    onClick={() => handleDelete(rule)}
                    className="rounded p-1 text-danger-500 hover:bg-danger-50 dark:hover:bg-danger-900/30"
                    aria-label={t('Common.Delete')}
                  >
                    <Trash2 size={12} />
                  </button>
                </td>
              </tr>
            ))}
            {rules.length === 0 && (
              <tr>
                <td
                  colSpan={9}
                  className="px-2 py-4 text-center text-slate-400 dark:text-slate-500"
                >
                  {t('Settings.DiscountRules.Empty')}
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {editing && (
        <DiscountRuleForm
          rule={editing === 'new' ? null : editing}
          onClose={() => setEditing(null)}
          onSubmit={async (input) => {
            if (editing === 'new') {
              await createMut.mutateAsync(input);
              toast.success(t('Settings.DiscountRules.Created'));
            } else {
              await updateMut.mutateAsync({
                id: editing.id,
                name: input.name,
                scope: input.scope,
                valueType: input.valueType,
                value: input.value,
                customerGroupId: input.customerGroupId ?? null,
                productCategoryId: input.productCategoryId ?? null,
                productId: input.productId ?? null,
                validFromUtc: input.validFromUtc ?? null,
                validUntilUtc: input.validUntilUtc ?? null,
                minQuantity: input.minQuantity ?? null,
                priority: input.priority ?? 0,
                isActive: input.isActive ?? editing.isActive,
                description: input.description ?? null,
              });
              toast.success(t('Settings.DiscountRules.Updated'));
            }
            setEditing(null);
          }}
        />
      )}
    </ListPageTemplate>
  );
};

const StatusBadge = ({ active, t }: { active: boolean; t: TFunction }) => (
  <Badge variant={active ? 'success' : 'neutral'}>
    {t(active ? 'Common.Active' : 'Common.Inactive')}
  </Badge>
);

const validityLabel = (rule: DiscountRule): string => {
  const fmt = (utc: string) => utc.slice(0, 10);
  if (rule.validFromUtc && rule.validUntilUtc)
    return `${fmt(rule.validFromUtc)} → ${fmt(rule.validUntilUtc)}`;
  if (rule.validFromUtc) return `≥ ${fmt(rule.validFromUtc)}`;
  if (rule.validUntilUtc) return `≤ ${fmt(rule.validUntilUtc)}`;
  return '—';
};

const scopeTarget = (
  rule: DiscountRule,
  groups: Map<string, string>,
  categories: Map<string, string>,
): string => {
  const targets: string[] = [];
  if (rule.customerGroupId) targets.push(groups.get(rule.customerGroupId) ?? rule.customerGroupId);
  if (rule.productCategoryId)
    targets.push(categories.get(rule.productCategoryId) ?? rule.productCategoryId);
  if (rule.productId) targets.push(rule.productId);
  return targets.length > 0 ? targets.join(' · ') : '—';
};

export type { DiscountRuleScope, DiscountValueType };
export default DiscountRulesPage;
