import { useRef, useState } from 'react';

interface Props {
  initial: string;
  ariaLabel: string;
  disabled?: boolean;
  onCommit: (value: string) => void;
  onCancel: () => void;
}

export function InlineTextEditor({ initial, ariaLabel, disabled, onCommit, onCancel }: Props) {
  const [value, setValue] = useState(initial);
  const doneRef = useRef(false);

  const finish = (commit: boolean) => {
    if (doneRef.current) return;
    doneRef.current = true;
    if (commit) onCommit(value);
    else onCancel();
  };

  return (
    <input
      ref={(el) => el?.focus()}
      value={value}
      onChange={(e) => setValue(e.target.value)}
      onKeyDown={(e) => {
        if (e.key === 'Enter') finish(true);
        if (e.key === 'Escape') finish(false);
      }}
      onBlur={() => finish(true)}
      disabled={disabled}
      aria-label={ariaLabel}
      className="w-full min-w-[160px] rounded-md border border-primary-300 bg-white px-2 py-1 text-xs font-semibold text-slate-900 focus:outline-none focus:ring-2 focus:ring-primary-500/30 disabled:opacity-60 dark:border-primary-500/40 dark:bg-slate-900 dark:text-slate-100"
    />
  );
}
