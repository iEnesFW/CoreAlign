import { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Archive, Building2, Save, Undo2 } from 'lucide-react';
import {
  useArchivePlatformTenant,
  usePlatformTenantQuery,
  useRestorePlatformTenant,
  useUpdatePlatformTenant,
} from '@/features/platform/usePlatformTenants';
import { DetailPageTemplate } from '@/shared/ui/PageTemplate/PageTemplate';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';

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
    <DetailPageTemplate
      header={
        <PageHeader
          icon={<Building2 size={20} />}
          title={tenant.name}
          subtitle={tenant.slug}
          crumbs={[
            { label: t('Platform.Tenants.Title'), to: '/dashboard/platform/tenants' },
            { label: tenant.name },
          ]}
          actions={
            <>
              {tenant.isArchived ? (
                <Button
                  type="button"
                  variant="primary"
                  size="sm"
                  onClick={() => id && restore.mutate(id)}
                >
                  <Undo2 size={14} /> {t('Platform.Tenants.Restore')}
                </Button>
              ) : (
                <Button
                  type="button"
                  variant="danger"
                  size="sm"
                  onClick={() =>
                    id &&
                    archive.mutate(id, { onSuccess: () => navigate('/dashboard/platform/tenants') })
                  }
                >
                  <Archive size={14} /> {t('Platform.Tenants.Archive')}
                </Button>
              )}
              <Button
                type="submit"
                form="platform-tenant-detail-form"
                size="sm"
                isLoading={update.isPending}
              >
                <Save size={14} /> {t('Common.Save')}
              </Button>
            </>
          }
        />
      }
    >
      <form
        id="platform-tenant-detail-form"
        className="grid gap-3 rounded-xl border border-slate-200 bg-white p-4 dark:border-slate-700 dark:bg-slate-900 sm:grid-cols-2"
        onSubmit={(e) => {
          e.preventDefault();
          if (!id) return;
          update.mutate({ id, ...form });
        }}
      >
        <Input
          label={t('Platform.Tenants.Fields.Name')}
          value={form.name}
          onChange={(e) => setForm({ name: e.target.value })}
        />
        <Input
          label={t('Platform.Tenants.Fields.Slug')}
          value={form.slug}
          onChange={(e) => setForm({ slug: e.target.value })}
        />
        <Input
          label={t('Platform.Tenants.Fields.DpoName')}
          value={form.dpoContactName}
          onChange={(e) => setForm({ dpoContactName: e.target.value })}
        />
        <Input
          type="email"
          label={t('Platform.Tenants.Fields.DpoEmail')}
          value={form.dpoContactEmail}
          onChange={(e) => setForm({ dpoContactEmail: e.target.value })}
        />
      </form>
    </DetailPageTemplate>
  );
}

export default PlatformTenantDetailPage;
