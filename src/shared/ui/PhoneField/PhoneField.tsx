import PhoneInput, { type Country } from 'react-phone-number-input';
import 'react-phone-number-input/style.css';
import './PhoneField.css';

interface Props {
  label?: string;
  value: string;
  onChange: (value: string) => void;
  error?: string;
  disabled?: boolean;
  defaultCountry?: Country;
  placeholder?: string;
}

const labelCls = 'mb-1 block text-xs font-medium text-slate-700 dark:text-slate-300';

export const PhoneField = ({
  label,
  value,
  onChange,
  error,
  disabled,
  defaultCountry = 'TR',
  placeholder,
}: Props) => (
  <div>
    {label && <label className={labelCls}>{label}</label>}
    <PhoneInput
      international
      defaultCountry={defaultCountry}
      value={value || undefined}
      onChange={(v) => onChange(v ?? '')}
      disabled={disabled}
      placeholder={placeholder}
      className="ca-phone-input"
    />
    {error && <span className="mt-1 block text-xs text-danger-500">{error}</span>}
  </div>
);
