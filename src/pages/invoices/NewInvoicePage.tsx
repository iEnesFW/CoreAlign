import { useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { FileText } from 'lucide-react';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { CreateStandaloneInvoiceModal } from './components/CreateStandaloneInvoiceModal';

export const NewInvoicePage = () => {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const createdRef = useRef(false);

  return (
    <div className="space-y-4">
      <PageHeader
        icon={<FileText size={20} />}
        title={t('invoices.standalone.title')}
        subtitle={t('invoices.standalone.subtitle')}
        crumbs={[
          { label: t('invoices.title'), to: '/dashboard/invoices' },
          { label: t('invoices.standalone.title') },
        ]}
      />
      <div className="mx-auto w-full max-w-4xl">
        <CreateStandaloneInvoiceModal
          open
          presentation="page"
          onCreated={(id) => {
            createdRef.current = true;
            navigate(`/dashboard/invoices?focus=${id}`);
          }}
          onClose={() => {
            if (!createdRef.current) navigate('/dashboard/invoices');
          }}
        />
      </div>
    </div>
  );
};
