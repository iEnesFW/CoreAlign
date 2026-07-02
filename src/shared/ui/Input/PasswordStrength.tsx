import { useTranslation } from 'react-i18next';
import { cn } from '@/shared/lib/cn';

/**
 * PasswordStrength — live requirement meter driven by the SAME rules as
 * `registerSchema` (length + upper + lower + digit + special). Purely visual;
 * validation still happens through zod on submit.
 */
const RULES: { test: (v: string) => boolean; key: string; fallback: string }[] = [
  { test: (v) => v.length >= 8, key: 'auth.password.rules.length', fallback: 'En az 8 karakter' },
  { test: (v) => /[A-Z]/.test(v), key: 'auth.password.rules.upper', fallback: 'Büyük harf' },
  { test: (v) => /[a-z]/.test(v), key: 'auth.password.rules.lower', fallback: 'Küçük harf' },
  { test: (v) => /[0-9]/.test(v), key: 'auth.password.rules.digit', fallback: 'Rakam' },
  {
    test: (v) => /[^a-zA-Z0-9]/.test(v),
    key: 'auth.password.rules.special',
    fallback: 'Özel karakter',
  },
];

const BARS = [
  'bg-danger-500',
  'bg-danger-500',
  'bg-warning-500',
  'bg-warning-500',
  'bg-success-500',
];
const LABELS = [
  { key: 'auth.password.weak', fallback: 'Zayıf' },
  { key: 'auth.password.fair', fallback: 'Orta' },
  { key: 'auth.password.good', fallback: 'İyi' },
  { key: 'auth.password.strong', fallback: 'Güçlü' },
];

export const PasswordStrength = ({ value }: { value: string }) => {
  const { t } = useTranslation();
  if (!value) return null;

  const score = RULES.reduce((n, r) => n + (r.test(value) ? 1 : 0), 0);
  const pct = (score / RULES.length) * 100;
  const labelIdx = score <= 1 ? 0 : score === 2 ? 1 : score === 3 || score === 4 ? 2 : 3;

  return (
    <div className="mt-2 flex flex-col gap-2">
      <div className="flex items-center gap-2">
        <div className="h-1.5 flex-1 overflow-hidden rounded-full bg-slate-200 dark:bg-white/10">
          <div
            className={cn(
              'h-full rounded-full transition-all duration-300',
              BARS[Math.max(0, score - 1)],
            )}
            style={{ width: `${pct}%` }}
          />
        </div>
        <span className="w-12 text-right text-[11px] font-semibold text-slate-500 dark:text-slate-400">
          {t(LABELS[labelIdx].key, { defaultValue: LABELS[labelIdx].fallback })}
        </span>
      </div>
      <div className="flex flex-wrap gap-x-3 gap-y-1">
        {RULES.map((r) => {
          const ok = r.test(value);
          return (
            <span
              key={r.key}
              className={cn(
                'flex items-center gap-1 text-[11px] transition-colors',
                ok
                  ? 'text-success-600 dark:text-success-400'
                  : 'text-slate-400 dark:text-slate-500',
              )}
            >
              <span
                className={cn(
                  'grid h-3 w-3 place-items-center rounded-full text-[8px]',
                  ok ? 'bg-success-500 text-white' : 'bg-slate-200 dark:bg-white/10',
                )}
              >
                {ok ? '✓' : ''}
              </span>
              {t(r.key, { defaultValue: r.fallback })}
            </span>
          );
        })}
      </div>
    </div>
  );
};
