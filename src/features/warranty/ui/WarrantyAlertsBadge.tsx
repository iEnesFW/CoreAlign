import { useTranslation } from 'react-i18next';
import { Bell } from 'lucide-react';
import { useExpiringWarrantiesQuery } from '../hooks/useWarrantyContracts';

interface Props {
  withinDays?: number;
  onClick?: () => void;
}

export const WarrantyAlertsBadge = ({ withinDays = 30, onClick }: Props) => {
  const { t } = useTranslation();
  const { data, isLoading } = useExpiringWarrantiesQuery(withinDays);
  const count = data?.data?.length ?? 0;
  if (isLoading || count === 0) return null;

  return (
    <button
      type="button"
      onClick={onClick}
      className="relative inline-flex items-center gap-1 rounded-full bg-warning-100 px-3 py-1 text-xs font-medium text-warning-800 hover:bg-warning-200 dark:bg-warning-500/20 dark:text-warning-200"
      aria-label={t('Warranty.Alerts.BadgeAria', {
        defaultValue: 'Expiring warranties',
      })}
    >
      <Bell className="h-3.5 w-3.5" />
      <span>
        {t('Warranty.Alerts.ExpiringCount', {
          defaultValue: '{{count}} expiring',
          count,
        })}
      </span>
    </button>
  );
};

export default WarrantyAlertsBadge;
