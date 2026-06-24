import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Archive, Building2, Edit3, Search, Undo2 } from 'lucide-react';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { ListPageTemplate } from '@/shared/ui/PageTemplate/PageTemplate';
import { Input } from '@/shared/ui/Input/Input';
import { Checkbox } from '@/shared/ui/Checkbox/Checkbox';
import { Badge } from '@/shared/ui/Badge/Badge';
import {
  useArchivePlatformTenant,
  usePlatformTenantsQuery,
  useRestorePlatformTenant,
} from '@/features/platform/usePlatformTenants';

export function PlatformTenantsListPage() {
  const { t } = useTranslation();
  const [search, setSearch] = useState('');
  const [includeArchived, setIncludeArchived] = useState(false);
  const [page] = useState(1);
  const query = usePlatformTenantsQuery(search || undefined, page, 20, includeArchived);
  const archive = useArchivePlatformTenant();
  const restore = useRestorePlatformTenant();

  const items = query.data?.data?.items ?? [];

  return (
    <ListPageTemplate
      header={
        <PageHeader
          icon={<Building2 size={20} />}
          title={t('Platform.Tenants.Title')}
          subtitle={t('Platform.Tenants.Subtitle')}
        />
      }
      toolbar={
        <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
          <Input
            type="search"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder={t('Platform.Tenants.SearchPlaceholder')}
            leftIcon={<Search size={14} />}
            className="w-full sm:w-72"
          />
          <Checkbox
            checked={includeArchived}
            onChange={(e) => setIncludeArchived(e.target.checked)}
            label={t('Platform.Tenants.IncludeArchived')}
          />
        </div>
      }
    >
      <div className="overflow-x-auto rounded border border-slate-200 dark:border-slate-700">
        <table className="min-w-full divide-y divide-slate-200 text-sm dark:divide-slate-700">
          <thead className="bg-slate-50 dark:bg-slate-800/60">
            <tr>
              <th className="px-3 py-2 text-left font-semibold text-slate-700 dark:text-slate-200">
                {t('Platform.Tenants.Fields.Name')}
              </th>
              <th className="px-3 py-2 text-left font-semibold text-slate-700 dark:text-slate-200">
                {t('Platform.Tenants.Fields.Slug')}
              </th>
              <th className="px-3 py-2 text-left font-semibold text-slate-700 dark:text-slate-200">
                {t('Platform.Tenants.Fields.Dpo')}
              </th>
              <th className="px-3 py-2 text-left font-semibold text-slate-700 dark:text-slate-200">
                {t('Platform.Tenants.Fields.Status')}
              </th>
              <th className="px-3 py-2" />
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100 bg-white dark:divide-slate-800 dark:bg-slate-900">
            {items.map((tenant) => (
              <tr key={tenant.id}>
                <td className="px-3 py-2 text-slate-800 dark:text-slate-100">{tenant.name}</td>
                <td className="px-3 py-2 text-slate-600 dark:text-slate-300">{tenant.slug}</td>
                <td className="px-3 py-2 text-slate-600 dark:text-slate-300">
                  {tenant.dpoContactEmail ?? '—'}
                </td>
                <td className="px-3 py-2">
                  {tenant.isArchived ? (
                    <Badge variant="neutral">{t('Platform.Tenants.StatusArchived')}</Badge>
                  ) : (
                    <Badge variant="success">{t('Platform.Tenants.StatusActive')}</Badge>
                  )}
                </td>
                <td className="px-3 py-2 text-right">
                  <div className="flex justify-end gap-2">
                    <Link
                      to={`/dashboard/platform/tenants/${tenant.id}`}
                      className="inline-flex items-center gap-1 rounded bg-primary-50 px-2 py-1 text-xs text-primary-700 hover:bg-primary-100 dark:bg-primary-900/40 dark:text-primary-200"
                    >
                      <Edit3 size={12} />
                      {t('Common.Edit')}
                    </Link>
                    {tenant.isArchived ? (
                      <button
                        type="button"
                        onClick={() => restore.mutate(tenant.id)}
                        className="inline-flex items-center gap-1 rounded bg-success-50 px-2 py-1 text-xs text-success-700 hover:bg-success-100 dark:bg-success-900/40 dark:text-success-200"
                      >
                        <Undo2 size={12} />
                        {t('Platform.Tenants.Restore')}
                      </button>
                    ) : (
                      <button
                        type="button"
                        onClick={() => archive.mutate(tenant.id)}
                        className="inline-flex items-center gap-1 rounded bg-danger-50 px-2 py-1 text-xs text-danger-700 hover:bg-danger-100 dark:bg-danger-900/40 dark:text-danger-200"
                      >
                        <Archive size={12} />
                        {t('Platform.Tenants.Archive')}
                      </button>
                    )}
                  </div>
                </td>
              </tr>
            ))}
            {items.length === 0 && !query.isLoading && (
              <tr>
                <td
                  colSpan={5}
                  className="px-3 py-6 text-center text-sm text-slate-500 dark:text-slate-400"
                >
                  {t('Platform.Tenants.Empty')}
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </ListPageTemplate>
  );
}

export default PlatformTenantsListPage;
