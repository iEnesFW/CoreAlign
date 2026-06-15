import { Eye, Mail, Phone, ShieldOff, ShieldCheck, UserPlus, Unlink2 } from 'lucide-react';
import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Button } from '@/shared/ui/Button';
import { Card, CardBody, CardHeader } from '@/shared/ui/Card';
import { DealerStatusBadge } from '@/shared/ui/StatusBadge';
import { Spinner } from '@/shared/ui/Spinner';
import type { DealerAccount } from '@/features/portal/types';
import {
  useDealerLinks,
  useDealerUsers,
  useUnlinkDealer,
  useUpdateDealerUserStatus,
} from './hooks';
import { InviteDealerUserForm } from './InviteDealerUserForm';
import { ProductVisibilityModal } from './ProductVisibilityModal';

interface DealerCardProps {
  dealer: DealerAccount;
  customerId: string;
}

export const DealerCard = ({ dealer, customerId }: DealerCardProps) => {
  const { t } = useTranslation();
  const usersQuery = useDealerUsers(dealer.id);
  const linksQuery = useDealerLinks(customerId);
  const updateUserStatus = useUpdateDealerUserStatus(dealer.id);
  const unlinkDealer = useUnlinkDealer();
  const [inviteOpen, setInviteOpen] = useState(false);
  const [visibilityOpen, setVisibilityOpen] = useState(false);

  const owner = useMemo(
    () => usersQuery.data?.find((u) => u.membershipRole === 'DealerOwner'),
    [usersQuery.data],
  );
  const dealerLink = useMemo(
    () =>
      linksQuery.data?.find((l) => l.dealerAccountId === dealer.id && l.customerId === customerId),
    [linksQuery.data, dealer.id, customerId],
  );

  const ownerIsSuspended = owner?.status === 'Suspended';
  const ownerIsActive = owner?.status === 'Active';

  const handleMutationError = (caught: unknown) => {
    const err = caught as { normalizedMessage?: string; status?: number };
    if (err.status !== 401 && err.status !== 403) {
      toast.error(err.normalizedMessage ?? t('errors.unknown'));
    }
  };

  const onSuspendAccess = () => {
    if (!owner) {
      toast.error(t('errors.unknown'));
      return;
    }
    updateUserStatus.mutate(
      { id: owner.id, status: 'Suspended' },
      {
        onSuccess: () => toast.success(t('dealers.form.suspendedSuccess')),
        onError: handleMutationError,
      },
    );
  };

  const onActivateAccess = () => {
    if (!owner) {
      toast.error(t('errors.unknown'));
      return;
    }
    updateUserStatus.mutate(
      { id: owner.id, status: 'Active' },
      {
        onSuccess: () => toast.success(t('dealers.form.activatedSuccess')),
        onError: handleMutationError,
      },
    );
  };

  const onUnlink = () => {
    if (!dealerLink) {
      toast.error(t('errors.unknown'));
      return;
    }
    unlinkDealer.mutate(
      { linkId: dealerLink.id },
      {
        onSuccess: () => toast.success(t('dealers.form.unlinkedSuccess')),
        onError: handleMutationError,
      },
    );
  };

  return (
    <Card className="overflow-hidden">
      <CardHeader
        title={
          <span className="flex items-center gap-2">
            {dealer.name}
            <DealerStatusBadge status={dealer.status} />
          </span>
        }
        subtitle={
          <span className="text-xs uppercase tracking-wide text-slate-400">
            {t('dealers.code')}: {dealer.code}
          </span>
        }
      />
      <CardBody className="flex flex-col gap-5">
        <div className="grid grid-cols-1 gap-3 text-sm text-slate-600 sm:grid-cols-2 dark:text-slate-300">
          {dealer.email ? (
            <span className="flex items-center gap-2">
              <Mail size={14} className="text-slate-400" />
              {dealer.email}
            </span>
          ) : null}
          {dealer.phone ? (
            <span className="flex items-center gap-2">
              <Phone size={14} className="text-slate-400" />
              {dealer.phone}
            </span>
          ) : null}
        </div>

        <div>
          <p className="mb-2 text-xs font-medium uppercase tracking-wide text-slate-500 dark:text-slate-400">
            {t('dealers.users')}
          </p>
          {usersQuery.isLoading ? (
            <div className="flex items-center gap-2 text-sm text-slate-500">
              <Spinner /> {t('common.loading')}
            </div>
          ) : (usersQuery.data?.length ?? 0) === 0 ? (
            <p className="text-sm text-slate-400">{t('common.noData')}</p>
          ) : (
            <ul className="flex flex-col gap-1.5 text-sm">
              {usersQuery.data!.map((u) => (
                <li
                  key={u.id}
                  className="flex flex-wrap items-center justify-between gap-2 rounded-lg border border-slate-100 px-3 py-2 dark:border-slate-800"
                >
                  <span className="text-slate-700 dark:text-slate-200">
                    {u.userFirstName || u.userLastName
                      ? `${u.userFirstName ?? ''} ${u.userLastName ?? ''}`.trim()
                      : u.userEmail}
                    <span className="ml-2 text-xs text-slate-400">{u.userEmail}</span>
                  </span>
                  <span className="text-xs text-slate-500">
                    {u.membershipRole} • {u.status}
                  </span>
                </li>
              ))}
            </ul>
          )}
        </div>

        <div className="flex flex-wrap gap-2">
          <Button size="sm" variant="secondary" onClick={() => setInviteOpen(true)}>
            <UserPlus size={14} />
            {t('dealers.inviteDealerUser')}
          </Button>
          {dealerLink ? (
            <Button size="sm" variant="ghost" onClick={() => setVisibilityOpen(true)}>
              <Eye size={14} />
              {t('dealers.manageVisibility')}
            </Button>
          ) : null}
          {ownerIsActive ? (
            <Button
              size="sm"
              variant="ghost"
              onClick={onSuspendAccess}
              disabled={updateUserStatus.isPending}
            >
              <ShieldOff size={14} />
              {t('dealers.suspend')}
            </Button>
          ) : null}
          {ownerIsSuspended ? (
            <Button
              size="sm"
              variant="ghost"
              onClick={onActivateAccess}
              disabled={updateUserStatus.isPending}
            >
              <ShieldCheck size={14} />
              {t('dealers.activate')}
            </Button>
          ) : null}
          {dealerLink ? (
            <Button size="sm" variant="danger" onClick={onUnlink} disabled={unlinkDealer.isPending}>
              <Unlink2 size={14} />
              {t('dealers.unlink')}
            </Button>
          ) : null}
        </div>
      </CardBody>

      <InviteDealerUserForm
        open={inviteOpen}
        onClose={() => setInviteOpen(false)}
        dealerAccountId={dealer.id}
        dealerName={dealer.name}
      />

      {dealerLink ? (
        <ProductVisibilityModal
          open={visibilityOpen}
          onClose={() => setVisibilityOpen(false)}
          linkId={dealerLink.id}
          dealerName={dealer.name}
        />
      ) : null}
    </Card>
  );
};
