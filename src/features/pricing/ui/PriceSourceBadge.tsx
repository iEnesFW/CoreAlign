import { Sparkles, Tag, TrendingDown, User, Wand } from 'lucide-react';
import type { PriceSource } from '../model/pricing.types';

const SOURCE_META: Record<PriceSource, { tone: string; icon: React.ReactNode }> = {
  ProductListPrice: {
    tone: 'bg-slate-100 text-slate-700 dark:bg-slate-700/40 dark:text-slate-300',
    icon: <Tag size={10} />,
  },
  PriceList: {
    tone: 'bg-primary-100 text-primary-700 dark:bg-primary-500/20 dark:text-primary-300',
    icon: <TrendingDown size={10} />,
  },
  CustomerProductPrice: {
    tone: 'bg-success-100 text-success-700 dark:bg-success-500/20 dark:text-success-300',
    icon: <User size={10} />,
  },
  Promotion: {
    tone: 'bg-warning-100 text-warning-800 dark:bg-warning-500/20 dark:text-warning-300',
    icon: <Sparkles size={10} />,
  },
  ManualOverride: {
    tone: 'bg-violet-100 text-violet-700 dark:bg-violet-500/20 dark:text-violet-300',
    icon: <Wand size={10} />,
  },
};

interface Props {
  source: PriceSource;
  label?: string;
}

export const PriceSourceBadge = ({ source, label }: Props) => {
  const meta = SOURCE_META[source];
  return (
    <span
      className={`inline-flex items-center gap-1 rounded px-1.5 py-0.5 text-[10px] font-semibold ${meta.tone}`}
    >
      {meta.icon}
      {label ?? source}
    </span>
  );
};
