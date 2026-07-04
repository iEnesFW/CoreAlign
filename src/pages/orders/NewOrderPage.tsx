import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { ShoppingCart } from 'lucide-react';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { OrderFormModal } from '@/features/orders/ui/OrderFormModal';

export const NewOrderPage = () => {
  const { t } = useTranslation();
  const navigate = useNavigate();

  return (
    <div className="space-y-4">
      <PageHeader
        icon={<ShoppingCart size={20} />}
        title={t('orders.modal.createTitle')}
        crumbs={[
          { label: t('orders.title'), to: '/dashboard/orders' },
          { label: t('orders.modal.createTitle') },
        ]}
      />
      <div className="mx-auto w-full max-w-4xl">
        <OrderFormModal
          open
          order={null}
          presentation="page"
          onClose={() => navigate('/dashboard/orders')}
        />
      </div>
    </div>
  );
};
