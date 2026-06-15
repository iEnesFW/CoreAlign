import { useTranslation } from 'react-i18next';
import type { AbcClass } from '../model/mrp-planning.types';

interface Props {
  abcClass: AbcClass;
  className?: string;
}

const TONE: Record<Exclude<AbcClass, 'Unclassified'>, string> = {
  A: 'bg-rose-100 text-rose-700 dark:bg-rose-500/20 dark:text-rose-300',
  B: 'bg-amber-100 text-amber-700 dark:bg-amber-500/20 dark:text-amber-300',
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
