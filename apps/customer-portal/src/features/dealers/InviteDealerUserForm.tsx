import { useState, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Button } from '@/shared/ui/Button';
import { Input } from '@/shared/ui/Input';
import { Modal } from '@/shared/ui/Modal';
import { Spinner } from '@/shared/ui/Spinner';
import { useInviteDealerUser } from './hooks';

interface InviteDealerUserFormProps {
  open: boolean;
  onClose: () => void;
  dealerAccountId: string;
  dealerName: string;
}

export const InviteDealerUserForm = ({
  open,
  onClose,
  dealerAccountId,
  dealerName,
}: InviteDealerUserFormProps) => {
  const { t } = useTranslation();
  const inviteUser = useInviteDealerUser();
  const [email, setEmail] = useState('');
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');

  const onSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (inviteUser.isPending) return;
    inviteUser.mutate(
      {
        dealerAccountId,
        email: email.trim(),
        firstName: firstName.trim() || undefined,
        lastName: lastName.trim() || undefined,
        role: 'DealerStaff',
      },
      {
        onSuccess: () => {
          toast.success(t('dealers.form.createdSuccess'));
          setEmail('');
          setFirstName('');
          setLastName('');
          onClose();
        },
        onError: (caught) => {
          const err = caught as { normalizedMessage?: string; status?: number };
          if (err.status !== 401 && err.status !== 403) {
            toast.error(err.normalizedMessage ?? t('errors.unknown'));
          }
        },
      },
    );
  };

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={t('dealers.inviteDealerUser')}
      description={dealerName}
      footer={
        <>
          <Button type="button" variant="ghost" onClick={onClose} disabled={inviteUser.isPending}>
            {t('common.cancel')}
          </Button>
          <Button type="submit" form="invite-dealer-user-form" disabled={inviteUser.isPending}>
            {inviteUser.isPending ? <Spinner size={16} className="text-white" /> : null}
            {inviteUser.isPending ? t('dealers.form.submitting') : t('dealers.form.submit')}
          </Button>
        </>
      }
    >
      <form id="invite-dealer-user-form" onSubmit={onSubmit} className="flex flex-col gap-4">
        <Input
          label={t('dealers.form.ownerEmail')}
          type="email"
          value={email}
          onChange={(event) => setEmail(event.target.value)}
          required
          disabled={inviteUser.isPending}
        />
        <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
          <Input
            label={t('dealers.form.ownerFirstName')}
            value={firstName}
            onChange={(event) => setFirstName(event.target.value)}
            disabled={inviteUser.isPending}
          />
          <Input
            label={t('dealers.form.ownerLastName')}
            value={lastName}
            onChange={(event) => setLastName(event.target.value)}
            disabled={inviteUser.isPending}
          />
        </div>
      </form>
    </Modal>
  );
};
