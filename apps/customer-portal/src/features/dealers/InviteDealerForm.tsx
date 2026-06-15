import { useMutation } from '@tanstack/react-query';
import { useState, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Button } from '@/shared/ui/Button';
import { Input } from '@/shared/ui/Input';
import { Modal } from '@/shared/ui/Modal';
import { Spinner } from '@/shared/ui/Spinner';
import { useCreateDealerAccount, useInviteDealerUser } from './hooks';

interface InviteDealerFormProps {
  open: boolean;
  onClose: () => void;
  customerId: string;
}

export const InviteDealerForm = ({ open, onClose, customerId }: InviteDealerFormProps) => {
  const { t } = useTranslation();
  const createDealer = useCreateDealerAccount();
  const inviteUser = useInviteDealerUser();

  const [name, setName] = useState('');
  const [code, setCode] = useState('');
  const [email, setEmail] = useState('');
  const [phone, setPhone] = useState('');
  const [ownerEmail, setOwnerEmail] = useState('');
  const [ownerFirstName, setOwnerFirstName] = useState('');
  const [ownerLastName, setOwnerLastName] = useState('');

  const reset = () => {
    setName('');
    setCode('');
    setEmail('');
    setPhone('');
    setOwnerEmail('');
    setOwnerFirstName('');
    setOwnerLastName('');
  };

  const submitMutation = useMutation({
    mutationFn: async () => {
      const dealer = await createDealer.mutateAsync({
        code: code.trim(),
        name: name.trim(),
        primaryCustomerId: customerId,
        email: email.trim() || undefined,
        phone: phone.trim() || undefined,
      });
      await inviteUser.mutateAsync({
        dealerAccountId: dealer.id,
        email: ownerEmail.trim(),
        firstName: ownerFirstName.trim() || undefined,
        lastName: ownerLastName.trim() || undefined,
        role: 'DealerOwner',
      });
    },
    onSuccess: () => {
      toast.success(t('dealers.form.createdSuccess'));
      reset();
      onClose();
    },
    onError: (caught: unknown) => {
      const err = caught as { normalizedMessage?: string; status?: number };
      if (err.status !== 401 && err.status !== 403) {
        toast.error(err.normalizedMessage ?? t('errors.unknown'));
      }
    },
  });

  const submitting = createDealer.isPending || inviteUser.isPending || submitMutation.isPending;

  const onSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (submitting) return;
    submitMutation.mutate();
  };

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={t('dealers.inviteDealer')}
      size="lg"
      footer={
        <>
          <Button type="button" variant="ghost" onClick={onClose} disabled={submitting}>
            {t('common.cancel')}
          </Button>
          <Button type="submit" form="invite-dealer-form" disabled={submitting}>
            {submitting ? <Spinner size={16} className="text-white" /> : null}
            {submitting ? t('dealers.form.submitting') : t('dealers.form.submit')}
          </Button>
        </>
      }
    >
      <form
        id="invite-dealer-form"
        onSubmit={onSubmit}
        className="grid grid-cols-1 gap-4 md:grid-cols-2"
      >
        <Input
          label={t('dealers.form.name')}
          value={name}
          onChange={(event) => setName(event.target.value)}
          required
          disabled={submitting}
        />
        <Input
          label={t('dealers.form.code')}
          value={code}
          onChange={(event) => setCode(event.target.value)}
          required
          disabled={submitting}
        />
        <Input
          label={t('dealers.form.email')}
          type="email"
          value={email}
          onChange={(event) => setEmail(event.target.value)}
          disabled={submitting}
        />
        <Input
          label={t('dealers.form.phone')}
          value={phone}
          onChange={(event) => setPhone(event.target.value)}
          disabled={submitting}
        />
        <div className="md:col-span-2 mt-2 border-t border-slate-100 pt-4 dark:border-slate-800">
          <p className="mb-3 text-sm font-medium text-slate-700 dark:text-slate-200">
            {t('dealers.inviteDealerUser')}
          </p>
        </div>
        <Input
          label={t('dealers.form.ownerEmail')}
          type="email"
          value={ownerEmail}
          onChange={(event) => setOwnerEmail(event.target.value)}
          required
          disabled={submitting}
        />
        <div />
        <Input
          label={t('dealers.form.ownerFirstName')}
          value={ownerFirstName}
          onChange={(event) => setOwnerFirstName(event.target.value)}
          disabled={submitting}
        />
        <Input
          label={t('dealers.form.ownerLastName')}
          value={ownerLastName}
          onChange={(event) => setOwnerLastName(event.target.value)}
          disabled={submitting}
        />
      </form>
    </Modal>
  );
};
