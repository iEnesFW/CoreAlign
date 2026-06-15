import { useTranslation } from 'react-i18next';
import { PageHeader } from '@/shared/ui/PageHeader';
import { CreateOrderForm } from '@/features/orders/CreateOrderForm';

export const NewOrderPage = () => {
  const { t } = useTranslation();
  return (
    <div className="space-y-6">
      <PageHeader title={t('orders.create.title')} subtitle={t('orders.create.subtitle')} />
      <CreateOrderForm />
    </div>
  );
};
