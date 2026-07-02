import React, { forwardRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Eye, EyeOff } from 'lucide-react';
import { Input } from './Input';

type BaseProps = Omit<React.ComponentProps<typeof Input>, 'type' | 'rightSlot'>;

/**
 * PasswordInput — Input with a built-in show/hide toggle in the right slot.
 * Forwards the ref to the underlying <input>, so it drops into react-hook-form
 * (`{...register('password')}`) and controlled forms (`value`/`onChange`) alike.
 */
export const PasswordInput = forwardRef<HTMLInputElement, BaseProps>((props, ref) => {
  const { t } = useTranslation();
  const [show, setShow] = useState(false);
  return (
    <Input
      ref={ref}
      type={show ? 'text' : 'password'}
      rightSlot={
        <button
          type="button"
          tabIndex={-1}
          onClick={() => setShow((v) => !v)}
          aria-label={t(show ? 'auth.login.hidePassword' : 'auth.login.showPassword', {
            defaultValue: show ? 'Şifreyi gizle' : 'Şifreyi göster',
          })}
          className="grid h-8 w-8 place-items-center rounded-md text-slate-400 transition-colors hover:bg-slate-100 hover:text-slate-600 dark:hover:bg-white/5 dark:hover:text-slate-200"
        >
          {show ? <EyeOff size={18} /> : <Eye size={18} />}
        </button>
      }
      {...props}
    />
  );
});

PasswordInput.displayName = 'PasswordInput';
