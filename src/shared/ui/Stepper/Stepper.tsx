import { Check } from 'lucide-react';

import type { StepperStep } from './Stepper.types';

interface StepperProps {
  steps: StepperStep[];
  current: string;
  onStepClick?: (id: string) => void;
}

export const Stepper = ({ steps, current, onStepClick }: StepperProps) => {
  const currentIndex = Math.max(
    0,
    steps.findIndex((s) => s.id === current),
  );

  return (
    <ol className="flex items-center gap-2" aria-label="progress">
      {steps.map((step, index) => {
        const done = index < currentIndex;
        const active = index === currentIndex;
        // Forward navigation belongs to the CTA, which is where validation lives; letting a click
        // skip ahead would land the user on a step whose preconditions were never checked.
        const clickable = Boolean(onStepClick) && index < currentIndex;

        const circle = done
          ? 'border-success-500 bg-success-500 text-white'
          : active
            ? 'border-primary-500 bg-gradient-to-br from-primary-500 to-purple-600 text-white shadow-sm shadow-primary-500/30'
            : 'border-slate-300 bg-white text-slate-500 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-400';

        return (
          <li key={step.id} className="flex items-center gap-2">
            <button
              type="button"
              onClick={clickable ? () => onStepClick?.(step.id) : undefined}
              disabled={!clickable}
              aria-current={active ? 'step' : undefined}
              className={`flex items-center gap-2 rounded-full py-1 pr-3 pl-1 text-sm transition ${
                clickable
                  ? 'cursor-pointer hover:bg-slate-100 dark:hover:bg-slate-800'
                  : 'cursor-default'
              }`}
            >
              <span
                className={`flex h-7 w-7 items-center justify-center rounded-full border text-xs font-semibold transition ${circle}`}
              >
                {done ? <Check size={14} aria-hidden="true" /> : index + 1}
              </span>
              <span
                className={
                  active
                    ? 'font-semibold text-slate-900 dark:text-slate-100'
                    : 'text-slate-500 dark:text-slate-400'
                }
              >
                {step.label}
              </span>
            </button>
            {index < steps.length - 1 && (
              <span
                aria-hidden="true"
                className={`h-px w-6 sm:w-10 ${
                  done ? 'bg-success-400' : 'bg-slate-200 dark:bg-slate-700'
                }`}
              />
            )}
          </li>
        );
      })}
    </ol>
  );
};
