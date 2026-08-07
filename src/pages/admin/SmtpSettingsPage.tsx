import { Navigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Mail } from 'lucide-react';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { useIsTenantAdmin } from '@/shared/lib/auth/useIsTenantAdmin';
import { SmtpSettingsForm } from '@/features/admin/smtp/ui/SmtpSettingsForm';
import { SmtpTestSendCard } from '@/features/admin/smtp/ui/SmtpTestSendCard';

export const SmtpSettingsPage = () => {
  const { t } = useTranslation();
  const isAdmin = useIsTenantAdmin();

  if (!isAdmin) {
    return <Navigate to="/dashboard" replace />;
  }

  return (
    <main className="space-y-4 p-4">
      <PageHeader
        icon={<Mail size={20} />}
        eyebrow={t('Admin.Smtp.Eyebrow')}
        title={t('Admin.Smtp.Title')}
        subtitle={t('Admin.Smtp.Description')}
      />
      <div className="grid gap-4 lg:grid-cols-2">
        <SmtpSettingsForm />
        <SmtpTestSendCard />
      </div>
    </main>
  );
};

export default SmtpSettingsPage;
