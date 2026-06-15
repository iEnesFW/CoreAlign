import { useTranslation } from 'react-i18next';
import { Factory, ShoppingCart } from 'lucide-react';
import type { ProcurementType } from '../model/mrp-planning.types';

interface Props {
  type: ProcurementType;
  className?: string;
}

const TONE: Record<ProcurementType, string> = {
  Make: 'bg-violet-100 text-violet-700 dark:bg-violet-500/20 dark:text-violet-300',
  Buy: 'bg-sky-100 text-sky-700 dark:bg-sky-500/20 dark:text-sky-300',
};

export const ProcurementBadge = ({ type, className = '' }: Props) => {
  const { t } = useTranslation();
  const Icon = type === 'Make' ? Factory : ShoppingCart;
  return (
    <span
      data-testid="procurement-badge"
      data-procurement-type={type}
      className={`inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-[11px] font-semibold ${TONE[type]} ${className}`}
    >
      <Icon className="h-3 w-3" />
      {t(`Mrp.Workbench.Procurement.${type}`)}
    </span>
  );
};
