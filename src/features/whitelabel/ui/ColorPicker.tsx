import { useId } from 'react';

interface ColorPickerProps {
  label: string;
  value: string;
  onChange: (next: string) => void;
  hint?: string;
  disabled?: boolean;
}

const HEX_PATTERN = /^#[0-9a-fA-F]{6}$/;

export const ColorPicker = ({ label, value, onChange, hint, disabled }: ColorPickerProps) => {
  const id = useId();
  const safeValue = HEX_PATTERN.test(value) ? value : '#0EA5E9';

  const handleHexInput = (next: string) => {
    const trimmed = next.trim();
    if (!trimmed.startsWith('#')) {
      onChange(`#${trimmed.replace('#', '')}`);
      return;
    }
    onChange(trimmed);
  };

  return (
    <div className="flex flex-col gap-1">
      <label htmlFor={id} className="text-sm font-medium text-slate-700 dark:text-slate-200">
        {label}
      </label>
      <div className="flex items-center gap-2">
        <input
          id={id}
          type="color"
          value={safeValue}
          disabled={disabled}
          onChange={(e) => onChange(e.target.value.toUpperCase())}
          className="h-9 w-12 cursor-pointer rounded border border-slate-300 dark:border-slate-600"
        />
        <input
          type="text"
          value={value}
          disabled={disabled}
          onChange={(e) => handleHexInput(e.target.value)}
          placeholder="#RRGGBB"
          className="h-9 w-32 rounded border border-slate-300 px-2 text-sm uppercase dark:border-slate-600 dark:bg-slate-800 dark:text-slate-100"
          maxLength={7}
        />
      </div>
      {hint ? <span className="text-xs text-slate-500 dark:text-slate-400">{hint}</span> : null}
    </div>
  );
};
