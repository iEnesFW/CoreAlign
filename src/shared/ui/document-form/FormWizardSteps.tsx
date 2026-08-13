import { ArrowRight } from 'lucide-react';

export interface FormWizardStep {
  id: number;
  label: string;
}

interface Props {
  steps: readonly FormWizardStep[];
  current: number;
  onSelect: (id: number) => void;
  ariaLabel: string;
}

export const FormWizardSteps = ({ steps, current, onSelect, ariaLabel }: Props) => {
  const currentIndex = steps.findIndex((s) => s.id === current);

  return (
    <nav className="flex items-center" aria-label={ariaLabel}>
      {steps.map((item, index) => (
        <div key={item.id} className="flex items-center">
          <button
            type="button"
            aria-current={index === currentIndex ? 'step' : undefined}
            className="flex items-center rounded-md px-1.5 py-1 transition-colors hover:bg-slate-100/80 dark:hover:bg-slate-800/50"
            onClick={() => onSelect(item.id)}
          >
            <span
              className={`flex h-6 w-6 items-center justify-center rounded-full text-[11px] font-bold transition-colors ${
                index === currentIndex
                  ? 'bg-indigo-600 text-white shadow-sm'
                  : currentIndex > index
                    ? 'bg-indigo-100 text-indigo-700 dark:bg-indigo-900/50 dark:text-indigo-300'
                    : 'bg-slate-200 text-slate-500 dark:bg-slate-800 dark:text-slate-400'
              }`}
            >
              {item.id}
            </span>
            <span
              className={`ml-1.5 whitespace-nowrap text-[11px] font-medium sm:text-xs ${
                index === currentIndex
                  ? 'text-indigo-900 dark:text-indigo-100'
                  : 'text-slate-500 dark:text-slate-400'
              }`}
            >
              {item.label}
            </span>
          </button>
          {index < steps.length - 1 && (
            <ArrowRight
              aria-hidden="true"
              className="pointer-events-none mx-1.5 h-3.5 w-3.5 shrink-0 text-slate-300 dark:text-slate-600"
            />
          )}
        </div>
      ))}
    </nav>
  );
};
