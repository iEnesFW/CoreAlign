import { useTranslation } from 'react-i18next';
import { Badge, type BadgeVariant } from '@/shared/ui/Badge/Badge';
import type { PayrollRunStatus } from '../model/enums';

const STATUS_VARIANT: Record<PayrollRunStatus, BadgeVariant> = {
  Draft: 'neutral',
  Calculated: 'info',
  Approved: 'primary',
  Posted: 'warning',
  Paid: 'success',
};

const STATUS_FALLBACK: Record<PayrollRunStatus, string> = {
  Draft: 'Taslak',
  Calculated: 'Hesaplandı',
  Approved: 'Onaylandı',
  Posted: 'Muhasebeleşti',
  Paid: 'Ödendi',
};

export const RunStatusBadge = ({ status }: { status: PayrollRunStatus }) => {
  const { t } = useTranslation();
  return (
    <Badge variant={STATUS_VARIANT[status]}>
      {t(`Payroll.runStatus.${status}`, { defaultValue: STATUS_FALLBACK[status] })}
    </Badge>
  );
};
