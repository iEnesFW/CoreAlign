import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { ShoppingCart } from 'lucide-react';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { OrderFormModal } from '@/features/orders/ui/OrderFormModal';

export const NewOrderPage = () => {
  const { t } = useTranslation();
  const navigate = useNavigate();

  return (
    <div className="flex h-full min-h-0 w-full flex-1 flex-col overflow-hidden py-2">
      <OrderFormModal
        open
        order={null}
        presentation="page"
        onClose={() => navigate('/dashboard/orders')}
        renderPageHeader={(stepNavigation) => (
          <PageHeader
            className="shrink-0"
            icon={<ShoppingCart size={20} />}
            title={t('orders.modal.createTitle')}
            crumbs={[
              { label: t('orders.title'), to: '/dashboard/orders' },
              { label: t('orders.modal.createTitle') },
            ]}
            bottomCenter={stepNavigation}
          />
        )}
      />
    </div>
  );
};
