import { Navigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Plug } from 'lucide-react';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { QueryError } from '@/shared/ui/QueryError/QueryError';
import { EmptyState } from '@/shared/ui/EmptyState/EmptyState';
import { useIsTenantAdmin } from '@/features/billing/hooks/useIsTenantAdmin';
import { ProvidersCategoryTabs } from '@/features/admin/providers/ui/ProvidersCategoryTabs';
import { useProvidersListQuery } from '@/features/admin/providers/hooks/useProvidersAdmin';

export const ProvidersAdminPage = () => {
  const { t } = useTranslation();
  const isAdmin = useIsTenantAdmin();
  const providersQuery = useProvidersListQuery();

  if (!isAdmin) {
    return <Navigate to="/dashboard" replace />;
  }

  const providers = providersQuery.data ?? [];

  return (
    <main className="space-y-4 p-4">
      <PageHeader
        icon={<Plug size={20} />}
        eyebrow={t('Admin.Providers.Eyebrow')}
        title={t('Admin.Providers.Title')}
        subtitle={t('Admin.Providers.Description')}
      />

      {providersQuery.isError ? (
        <QueryError
          description={t('Admin.Providers.LoadFailed')}
          onRetry={() => providersQuery.refetch()}
        />
      ) : providersQuery.isLoading ? (
        <EmptyState title={t('common.loading')} variant="plain" />
      ) : (
        <ProvidersCategoryTabs providers={providers} />
      )}
    </main>
  );
};

export default ProvidersAdminPage;
