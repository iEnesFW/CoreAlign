import { useTranslation } from 'react-i18next';
import { Badge, type BadgeVariant } from '@/shared/ui/Badge/Badge';
import type { SubscriptionOrderStatus } from '../model/billing.types';

const TONE_MAP: Record<SubscriptionOrderStatus, BadgeVariant> = {
  Draft: 'warning',
  PendingPayment: 'warning',
  Paid: 'success',
  Failed: 'error',
  Cancelled: 'neutral',
  Expired: 'neutral',
};

interface Props {
  status: SubscriptionOrderStatus;
  className?: string;
}

export const SubscriptionStatusBadge = ({ status, className }: Props) => {
  const { t } = useTranslation();
  return (
    <Badge variant={TONE_MAP[status]} className={className} pill>
      {t(`billing.order.status.${status}`)}
    </Badge>
  );
};
