import { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Archive, Save, Undo2 } from 'lucide-react';
import {
  useArchivePlatformTenant,
  usePlatformTenantQuery,
  useRestorePlatformTenant,
  useUpdatePlatformTenant,
} from '@/features/platform/usePlatformTenants';

interface FormState {
  name: string;
  slug: string;
  dpoContactName: string;
  dpoContactEmail: string;
}

export function PlatformTenantDetailPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const detail = usePlatformTenantQuery(id);
  const update = useUpdatePlatformTenant();
  const archive = useArchivePlatformTenant();
  const restore = useRestorePlatformTenant();

  const [overrides, setOverrides] = useState<Partial<FormState>>({});
  const tenant = detail.data?.data;

  if (!tenant) {
    return <p className="p-4 text-sm text-slate-500">{t('Common.Loading')}</p>;
  }

  const form: FormState = {
    name: overrides.name ?? tenant.name,
    slug: overrides.slug ?? tenant.slug,
    dpoContactName: overrides.dpoContactName ?? tenant.dpoContactName ?? '',
    dpoContactEmail: overrides.dpoContactEmail ?? tenant.dpoContactEmail ?? '',
  };

  const setForm = (patch: Partial<FormState>) => setOverrides((prev) => ({ ...prev, ...patch }));

  return (
    <section className="space-y-4 p-4">
      <header className="space-y-1">
        <h1 className="text-xl font-semibold text-slate-900 dark:text-slate-100">{tenant.name}</h1>
        <p className="text-sm text-slate-500 dark:text-slate-400">{tenant.slug}</p>
      </header>

      <form
        className="grid gap-3 rounded border border-slate-200 bg-white p-4 dark:border-slate-700 dark:bg-slate-900 sm:grid-cols-2"
        onSubmit={(e) => {
          e.preventDefault();
          if (!id) return;
          update.mutate({ id, ...form });
        }}
      >
        <label className="text-sm">
          <span className="block text-slate-600 dark:text-slate-300">
            {t('Platform.Tenants.Fields.Name')}
          </span>
          <input
            value={form.name}
            onChange={(e) => setForm({ name: e.target.value })}
            className="mt-1 w-full rounded border border-slate-200 bg-white p-1.5 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100"
          />
        </label>
        <label className="text-sm">
          <span className="block text-slate-600 dark:text-slate-300">
            {t('Platform.Tenants.Fields.Slug')}
          </span>
          <input
            value={form.slug}
            onChange={(e) => setForm({ slug: e.target.value })}
            className="mt-1 w-full rounded border border-slate-200 bg-white p-1.5 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100"
          />
        </label>
        <label className="text-sm">
          <span className="block text-slate-600 dark:text-slate-300">
            {t('Platform.Tenants.Fields.DpoName')}
          </span>
          <input
            value={form.dpoContactName}
            onChange={(e) => setForm({ dpoContactName: e.target.value })}
            className="mt-1 w-full rounded border border-slate-200 bg-white p-1.5 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100"
          />
        </label>
        <label className="text-sm">
          <span className="block text-slate-600 dark:text-slate-300">
            {t('Platform.Tenants.Fields.DpoEmail')}
          </span>
          <input
            type="email"
            value={form.dpoContactEmail}
            onChange={(e) => setForm({ dpoContactEmail: e.target.value })}
            className="mt-1 w-full rounded border border-slate-200 bg-white p-1.5 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100"
          />
        </label>
        <div className="col-span-full flex flex-wrap items-center justify-between gap-2">
          <div className="flex gap-2">
            {tenant.isArchived ? (
              <button
                type="button"
                onClick={() => id && restore.mutate(id)}
                className="inline-flex items-center gap-1 rounded bg-emerald-600 px-3 py-1.5 text-sm text-white hover:bg-emerald-700"
              >
                <Undo2 size={14} /> {t('Platform.Tenants.Restore')}
              </button>
            ) : (
              <button
                type="button"
                onClick={() =>
                  id &&
                  archive.mutate(id, { onSuccess: () => navigate('/dashboard/platform/tenants') })
                }
                className="inline-flex items-center gap-1 rounded bg-rose-600 px-3 py-1.5 text-sm text-white hover:bg-rose-700"
              >
                <Archive size={14} /> {t('Platform.Tenants.Archive')}
              </button>
            )}
          </div>
          <button
            type="submit"
            className="inline-flex items-center gap-1 rounded bg-indigo-600 px-3 py-1.5 text-sm text-white hover:bg-indigo-700 disabled:opacity-60"
            disabled={update.isPending}
          >
            <Save size={14} /> {t('Common.Save')}
          </button>
        </div>
      </form>
    </section>
  );
}

export default PlatformTenantDetailPage;
