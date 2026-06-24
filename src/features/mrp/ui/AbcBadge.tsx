import { useTranslation } from 'react-i18next';
import type { AbcClass } from '../model/mrp-planning.types';

interface Props {
  abcClass: AbcClass;
  className?: string;
}

const TONE: Record<Exclude<AbcClass, 'Unclassified'>, string> = {
  A: 'bg-danger-100 text-danger-700 dark:bg-danger-500/20 dark:text-danger-300',
  B: 'bg-warning-100 text-warning-700 dark:bg-warning-500/20 dark:text-warning-300',
  C: 'bg-slate-100 text-slate-600 dark:bg-slate-500/20 dark:text-slate-300',
};

export const AbcBadge = ({ abcClass, className = '' }: Props) => {
  const { t } = useTranslation();
  if (abcClass === 'Unclassified') return null;
  return (
    <span
      data-testid="abc-badge"
      data-abc-class={abcClass}
      className={`inline-flex items-center rounded-full px-2 py-0.5 text-[11px] font-semibold ${TONE[abcClass]} ${className}`}
    >
      {t(`Mrp.Workbench.Abc.${abcClass}`)}
    </span>
  );
};
