import type { ReactNode } from 'react';

export interface ModalTab {
  id: string;
  label: string;
  badge?: ReactNode;
  hasError?: boolean;
}

interface Props {
  tabs: ModalTab[];
  active: string;
  onChange: (id: string) => void;
}

/** Sticky tab bar for splitting long modal forms into sections. */
export const ModalTabs = ({ tabs, active, onChange }: Props) => (
  <div className="sticky top-[49px] z-10 flex gap-1 border-b border-slate-200 bg-white px-5 dark:border-slate-800 dark:bg-slate-900">
    {tabs.map((tab) => {
      const isActive = tab.id === active;
      return (
        <button
          key={tab.id}
          type="button"
          onClick={() => onChange(tab.id)}
          className={`-mb-px inline-flex items-center gap-1.5 border-b-2 px-3 py-2 text-sm font-medium transition ${
            isActive
              ? 'border-indigo-600 text-indigo-700 dark:border-indigo-400 dark:text-indigo-300'
              : 'border-transparent text-slate-500 hover:text-slate-700 dark:hover:text-slate-300'
          }`}
        >
          {tab.label}
          {tab.badge !== undefined && (
            <span className="rounded-full bg-slate-100 px-1.5 text-[10px] font-semibold text-slate-600 dark:bg-slate-700 dark:text-slate-300">
              {tab.badge}
            </span>
          )}
          {tab.hasError && <span className="h-1.5 w-1.5 rounded-full bg-red-500" aria-hidden />}
        </button>
      );
    })}
  </div>
);
