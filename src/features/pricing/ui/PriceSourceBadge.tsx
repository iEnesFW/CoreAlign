import { Sparkles, Tag, TrendingDown, User, Wand } from 'lucide-react';
import type { PriceSource } from '../model/pricing.types';

const SOURCE_META: Record<PriceSource, { tone: string; icon: React.ReactNode }> = {
  ProductListPrice: {
    tone: 'bg-slate-100 text-slate-700 dark:bg-slate-700/40 dark:text-slate-300',
    icon: <Tag size={10} />,
  },
  PriceList: {
    tone: 'bg-indigo-100 text-indigo-700 dark:bg-indigo-500/20 dark:text-indigo-300',
    icon: <TrendingDown size={10} />,
  },
  CustomerProductPrice: {
    tone: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/20 dark:text-emerald-300',
    icon: <User size={10} />,
  },
  Promotion: {
    tone: 'bg-amber-100 text-amber-800 dark:bg-amber-500/20 dark:text-amber-300',
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
