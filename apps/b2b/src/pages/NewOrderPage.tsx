import { useTranslation } from 'react-i18next';
import { PageHeader } from '@/shared/ui/PageHeader';
import { NewOrderForm } from '@/features/orders/NewOrderForm';

export const NewOrderPage = () => {
  const { t } = useTranslation();
  return (
    <div className="space-y-6">
      <PageHeader title={t('b2b.newOrder.title')} subtitle={t('b2b.newOrder.subtitle')} />
      <NewOrderForm />
    </div>
  );
};
