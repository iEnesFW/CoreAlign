import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Archive, Edit3, Search, Undo2 } from 'lucide-react';
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
    <section className="space-y-4 p-4">
      <header className="space-y-1">
        <h1 className="text-xl font-semibold text-slate-900 dark:text-slate-100">
          {t('Platform.Tenants.Title')}
        </h1>
        <p className="text-sm text-slate-500 dark:text-slate-400">
          {t('Platform.Tenants.Subtitle')}
        </p>
      </header>

      <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
        <label className="relative flex-1">
          <Search size={14} className="absolute left-2 top-1/2 -translate-y-1/2 text-slate-400" />
          <input
            type="search"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder={t('Platform.Tenants.SearchPlaceholder')}
            className="w-full rounded border border-slate-200 bg-white py-1.5 pl-7 pr-3 text-sm dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
          />
        </label>
        <label className="flex items-center gap-2 text-sm text-slate-700 dark:text-slate-200">
          <input
            type="checkbox"
            checked={includeArchived}
            onChange={(e) => setIncludeArchived(e.target.checked)}
          />
          {t('Platform.Tenants.IncludeArchived')}
        </label>
      </div>

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
                    <span className="rounded bg-slate-100 px-2 py-0.5 text-xs text-slate-700 dark:bg-slate-700 dark:text-slate-200">
                      {t('Platform.Tenants.StatusArchived')}
                    </span>
                  ) : (
                    <span className="rounded bg-emerald-100 px-2 py-0.5 text-xs text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-300">
                      {t('Platform.Tenants.StatusActive')}
                    </span>
                  )}
                </td>
                <td className="px-3 py-2 text-right">
                  <div className="flex justify-end gap-2">
                    <Link
                      to={`/dashboard/platform/tenants/${tenant.id}`}
                      className="inline-flex items-center gap-1 rounded bg-indigo-50 px-2 py-1 text-xs text-indigo-700 hover:bg-indigo-100 dark:bg-indigo-900/40 dark:text-indigo-200"
                    >
                      <Edit3 size={12} />
                      {t('Common.Edit')}
                    </Link>
                    {tenant.isArchived ? (
                      <button
                        type="button"
                        onClick={() => restore.mutate(tenant.id)}
                        className="inline-flex items-center gap-1 rounded bg-emerald-50 px-2 py-1 text-xs text-emerald-700 hover:bg-emerald-100 dark:bg-emerald-900/40 dark:text-emerald-200"
                      >
                        <Undo2 size={12} />
                        {t('Platform.Tenants.Restore')}
                      </button>
                    ) : (
                      <button
                        type="button"
                        onClick={() => archive.mutate(tenant.id)}
                        className="inline-flex items-center gap-1 rounded bg-rose-50 px-2 py-1 text-xs text-rose-700 hover:bg-rose-100 dark:bg-rose-900/40 dark:text-rose-200"
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
    </section>
  );
}

export default PlatformTenantsListPage;
