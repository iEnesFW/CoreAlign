import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import type { TFunction } from 'i18next';
import { Pencil, Plus, Power, Receipt, Trash2 } from 'lucide-react';
import { toast } from 'sonner';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useConfirm } from '@/shared/ui/ConfirmDialog/useConfirm';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { ListPageTemplate } from '@/shared/ui/PageTemplate/PageTemplate';
import { Button } from '@/shared/ui/Button/Button';
import { Badge } from '@/shared/ui/Badge/Badge';
import {
  useCreateTaxRule,
  useDeleteTaxRule,
  useTaxRulesQuery,
  useUpdateTaxRule,
} from '@/features/pricing/hooks/usePricingRulesQueries';
import { useCategoriesQuery, useTaxRatesQuery } from '@/shared/master-data/hooks/useMasterData';
import type { TaxRule } from '@/features/pricing/model/pricingRules.types';
import { TaxRuleForm } from './TaxRuleForm';

export const TaxRulesPage = () => {
  const { t } = useTranslation();
  const confirm = useConfirm();
  const rulesQ = useTaxRulesQuery();
  const ratesQ = useTaxRatesQuery();
  const categoriesQ = useCategoriesQuery();
  const createMut = useCreateTaxRule();
  const updateMut = useUpdateTaxRule();
  const deleteMut = useDeleteTaxRule();
  const [editing, setEditing] = useState<TaxRule | 'new' | null>(null);

  const rules = rulesQ.data?.data ?? [];
  const rateNameById = useMemo(() => {
    const map = new Map<string, string>();
    for (const r of ratesQ.data?.data ?? []) map.set(r.id, r.name);
    return map;
  }, [ratesQ.data]);
  const categoryNameById = useMemo(() => {
    const map = new Map<string, string>();
    for (const c of categoriesQ.data?.data ?? []) map.set(c.id, c.name);
    return map;
  }, [categoriesQ.data]);

  const handleDelete = async (rule: TaxRule) => {
    const ok = await confirm({
      title: t('Settings.TaxRules.DeleteTitle'),
      message: t('Settings.TaxRules.DeleteMessage', { code: rule.code }),
      confirmLabel: t('Common.Delete'),
    });
    if (!ok) return;
    try {
      await deleteMut.mutateAsync(rule.id);
      toast.success(t('Settings.TaxRules.Deleted'));
    } catch (err) {
      toastApiError(err);
    }
  };

  const handleToggleActive = async (rule: TaxRule) => {
    try {
      await updateMut.mutateAsync({
        id: rule.id,
        name: rule.name,
        scope: rule.scope,
        ratePercent: rule.ratePercent,
        regionCode: rule.regionCode,
        productClass: rule.productClass,
        productCategoryId: rule.productCategoryId,
        productId: rule.productId,
        fallbackTaxRateId: rule.fallbackTaxRateId,
        validFromUtc: rule.validFromUtc,
        validUntilUtc: rule.validUntilUtc,
        priority: rule.priority,
        isActive: !rule.isActive,
        description: rule.description,
      });
      toast.success(
        t(rule.isActive ? 'Settings.TaxRules.Deactivated' : 'Settings.TaxRules.Activated'),
      );
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <ListPageTemplate
      header={
        <PageHeader
          icon={<Receipt size={20} />}
          title={t('Settings.TaxRules.Title')}
          subtitle={t('Settings.TaxRules.Subtitle')}
          actions={
            <Button size="sm" onClick={() => setEditing('new')}>
              <Plus size={14} />
              {t('Settings.TaxRules.New')}
            </Button>
          }
        />
      }
    >
      <div className="overflow-x-auto rounded border border-slate-200 dark:border-slate-700">
        <table className="min-w-full text-xs">
          <thead className="bg-slate-50 text-left text-slate-500 dark:bg-slate-800 dark:text-slate-400">
            <tr>
              <th className="px-2 py-1.5 font-medium">{t('Settings.TaxRules.Code')}</th>
              <th className="px-2 py-1.5 font-medium">{t('Settings.TaxRules.Name')}</th>
              <th className="px-2 py-1.5 font-medium">{t('Settings.TaxRules.Scope')}</th>
              <th className="px-2 py-1.5 font-medium">{t('Settings.TaxRules.Target')}</th>
              <th className="px-2 py-1.5 text-right font-medium">{t('Settings.TaxRules.Rate')}</th>
              <th className="px-2 py-1.5 text-right font-medium">
                {t('Settings.TaxRules.Priority')}
              </th>
              <th className="px-2 py-1.5 font-medium">{t('Settings.TaxRules.Fallback')}</th>
              <th className="px-2 py-1.5 font-medium">{t('Settings.TaxRules.Status')}</th>
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
                  {t(`Settings.TaxRules.Scopes.${rule.scope}`)}
                </td>
                <td className="px-2 py-1.5 text-slate-600 dark:text-slate-300">
                  {targetLabel(rule, categoryNameById)}
                </td>
                <td className="px-2 py-1.5 text-right text-slate-800 dark:text-slate-100">
                  {rule.ratePercent}%
                </td>
                <td className="px-2 py-1.5 text-right text-slate-600 dark:text-slate-300">
                  {rule.priority}
                </td>
                <td className="px-2 py-1.5 text-slate-500 dark:text-slate-400">
                  {rule.fallbackTaxRateId
                    ? (rateNameById.get(rule.fallbackTaxRateId) ?? rule.fallbackTaxRateId)
                    : '—'}
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
                  {t('Settings.TaxRules.Empty')}
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {editing && (
        <TaxRuleForm
          rule={editing === 'new' ? null : editing}
          onClose={() => setEditing(null)}
          onSubmit={async (input) => {
            if (editing === 'new') {
              await createMut.mutateAsync(input);
              toast.success(t('Settings.TaxRules.Created'));
            } else {
              await updateMut.mutateAsync({
                id: editing.id,
                name: input.name,
                scope: input.scope,
                ratePercent: input.ratePercent,
                regionCode: input.regionCode ?? null,
                productClass: input.productClass ?? null,
                productCategoryId: input.productCategoryId ?? null,
                productId: input.productId ?? null,
                fallbackTaxRateId: input.fallbackTaxRateId ?? null,
                validFromUtc: input.validFromUtc ?? null,
                validUntilUtc: input.validUntilUtc ?? null,
                priority: input.priority ?? 0,
                isActive: input.isActive ?? editing.isActive,
                description: input.description ?? null,
              });
              toast.success(t('Settings.TaxRules.Updated'));
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

const targetLabel = (rule: TaxRule, categories: Map<string, string>): string => {
  const parts: string[] = [];
  if (rule.regionCode) parts.push(rule.regionCode);
  if (rule.productClass) parts.push(rule.productClass);
  if (rule.productCategoryId)
    parts.push(categories.get(rule.productCategoryId) ?? rule.productCategoryId);
  if (rule.productId) parts.push(rule.productId);
  return parts.length > 0 ? parts.join(' · ') : '—';
};

export default TaxRulesPage;
