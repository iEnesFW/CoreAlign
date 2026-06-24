import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useTestSmtpMutation } from '../hooks/useSmtpAdmin';

export const SmtpTestSendCard = () => {
  const { t } = useTranslation();
  const [recipient, setRecipient] = useState('');
  const testMutation = useTestSmtpMutation();

  const onSend = async () => {
    try {
      const result = await testMutation.mutateAsync(recipient.trim());
      if (result.success) {
        toast.success(result.message ?? t('Admin.Smtp.Test.Success'));
      } else {
        toast.error(result.message ?? t('Admin.Smtp.Test.Failed'));
      }
    } catch (err) {
      toastApiError(err, t('Admin.Smtp.Test.Failed'));
    }
  };

  const disabled = recipient.trim().length === 0 || testMutation.isPending;

  return (
    <div className="space-y-3 rounded-lg border border-slate-200 bg-white p-4 dark:border-slate-700 dark:bg-slate-900">
      <div>
        <p className="text-sm font-medium text-slate-900 dark:text-slate-100">
          {t('Admin.Smtp.Test.Title')}
        </p>
        <p className="text-[11px] text-slate-500 dark:text-slate-400">
          {t('Admin.Smtp.Test.Hint')}
        </p>
      </div>
      <div className="flex flex-col gap-2 sm:flex-row">
        <Input
          type="email"
          value={recipient}
          onChange={(e) => setRecipient(e.target.value)}
          placeholder={t('Admin.Smtp.Test.Recipient')}
          className="flex-1"
        />
        <Button
          type="button"
          variant="secondary"
          onClick={onSend}
          isLoading={testMutation.isPending}
          disabled={disabled}
        >
          {t('Admin.Smtp.Test.Send')}
        </Button>
      </div>
    </div>
  );
};
