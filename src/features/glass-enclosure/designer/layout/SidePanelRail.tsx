import { ChevronLeft, ChevronRight } from 'lucide-react';

interface SidePanelRailProps {
  side: 'left' | 'right';
  label: string;
  expandLabel: string;
  onExpand: () => void;
}

export const SidePanelRail = ({ side, label, expandLabel, onExpand }: SidePanelRailProps) => (
  <button
    type="button"
    onClick={onExpand}
    title={expandLabel}
    aria-label={expandLabel}
    aria-expanded={false}
    className="flex h-full w-full flex-col items-center gap-2 bg-white py-2 text-slate-500 transition-colors hover:bg-slate-50 hover:text-slate-700 focus-visible:ring-2 focus-visible:ring-blue-500 dark:bg-slate-900 dark:text-slate-400 dark:hover:bg-slate-800 dark:hover:text-slate-200"
  >
    {side === 'left' ? <ChevronRight size={14} /> : <ChevronLeft size={14} />}
    <span className="text-[10px] font-semibold uppercase tracking-wide [writing-mode:vertical-rl]">
      {label}
    </span>
  </button>
);

export default SidePanelRail;
