import { Plus } from 'lucide-react';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Button } from '@/shared/ui/Button';
import { PageHeader } from '@/shared/ui/PageHeader';
import { Spinner } from '@/shared/ui/Spinner';
import { useDashboard, usePortalDealers } from '@/features/portal/hooks';
import { DealerCard } from '@/features/dealers/DealerCard';
import { InviteDealerForm } from '@/features/dealers/InviteDealerForm';

export const DealersPage = () => {
  const { t } = useTranslation();
  const dealersQuery = usePortalDealers();
  const dashboardQuery = useDashboard();
  const [inviteOpen, setInviteOpen] = useState(false);

  const customerId = dashboardQuery.data?.customerId;

  return (
    <div className="space-y-6">
      <PageHeader
        title={t('dealers.title')}
        subtitle={t('dealers.subtitle')}
        action={
          <Button onClick={() => setInviteOpen(true)} disabled={!customerId}>
            <Plus size={16} />
            {t('dealers.inviteDealer')}
          </Button>
        }
      />

      {dealersQuery.isLoading ? (
        <div className="flex items-center gap-2 text-sm text-slate-500">
          <Spinner /> {t('common.loading')}
        </div>
      ) : (dealersQuery.data?.length ?? 0) === 0 ? (
        <div className="rounded-2xl border border-dashed border-slate-200 bg-white px-6 py-12 text-center text-sm text-slate-500 dark:border-slate-700 dark:bg-slate-900">
          {t('dealers.empty')}
        </div>
      ) : (
        <div className="grid grid-cols-1 gap-5 xl:grid-cols-2">
          {dealersQuery.data!.map((dealer) => (
            <DealerCard key={dealer.id} dealer={dealer} customerId={customerId ?? ''} />
          ))}
        </div>
      )}

      {customerId ? (
        <InviteDealerForm
          open={inviteOpen}
          onClose={() => setInviteOpen(false)}
          customerId={customerId}
        />
      ) : null}
    </div>
  );
};
