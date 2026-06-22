import type { ReactNode } from 'react';
import { cn } from '@/shared/lib/cn';
import { useDesignerUxMode } from '@/shared/lib/persona';

export type DesignerTabKey = 'runs' | 'canvas' | 'inspector' | 'bom';

export interface DesignerTabItem {
  key: DesignerTabKey;
  icon: ReactNode;
  label: string;
  emoji?: string;
}

interface DesignerTabBarProps {
  tabs: DesignerTabItem[];
  activeKey: DesignerTabKey;
  onSelect: (key: DesignerTabKey) => void;
  orientation?: 'horizontal' | 'vertical';
  className?: string;
}

export const DesignerTabBar = ({
  tabs,
  activeKey,
  onSelect,
  orientation = 'horizontal',
  className,
}: DesignerTabBarProps) => {
  const mode = useDesignerUxMode();
  const isSimple = mode === 'Simple';
  const isVertical = orientation === 'vertical';

  return (
    <nav
      aria-label="Designer tabs"
      role="tablist"
      aria-orientation={isVertical ? 'vertical' : 'horizontal'}
      className={cn(
        'flex',
        isVertical ? 'h-full flex-col items-stretch gap-1 p-2' : 'h-16 items-stretch',
        className,
      )}
    >
      {tabs.map((tab) => {
        const active = tab.key === activeKey;
        return (
          <button
            key={tab.key}
            type="button"
            onClick={() => onSelect(tab.key)}
            role="tab"
            id={`designer-tab-${tab.key}`}
            aria-selected={active}
            aria-controls={`designer-tabpanel-${tab.key}`}
            tabIndex={active ? 0 : -1}
            className={cn(
              'group relative flex items-center justify-center transition-colors',
              isVertical
                ? cn(
                    'rounded-md',
                    isSimple ? 'h-14 w-14' : 'h-12 w-12',
                    active
                      ? 'bg-primary-600 text-white shadow-sm'
                      : 'text-slate-600 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-800',
                  )
                : cn(
                    'flex-1 flex-col gap-0.5 border-t-2',
                    active
                      ? 'border-primary-600 text-primary-600 dark:text-primary-400'
                      : 'border-transparent text-slate-500 hover:text-slate-700 dark:text-slate-400 dark:hover:text-slate-200',
                  ),
            )}
            title={tab.label}
          >
            {isSimple && !isVertical && tab.emoji ? (
              <span className="text-xl leading-none" aria-hidden>
                {tab.emoji}
              </span>
            ) : (
              <span
                className={cn(
                  'inline-flex items-center justify-center',
                  isSimple ? 'text-[20px]' : 'text-[18px]',
                )}
                aria-hidden
              >
                {tab.icon}
              </span>
            )}
            {!isVertical && (
              <span
                className={cn(
                  'leading-tight',
                  isSimple ? 'text-[11px] font-semibold' : 'text-[10px] font-medium',
                )}
              >
                {tab.label}
              </span>
            )}
            {isVertical && (
              <span
                className={cn(
                  'pointer-events-none absolute left-full top-1/2 z-50 ml-2 -translate-y-1/2 whitespace-nowrap rounded-md bg-slate-900 px-2 py-1 text-[11px] font-medium text-white opacity-0 shadow-lg transition-opacity group-hover:opacity-100 dark:bg-slate-100 dark:text-slate-900',
                )}
                role="tooltip"
              >
                {tab.label}
              </span>
            )}
          </button>
        );
      })}
    </nav>
  );
};

export default DesignerTabBar;
