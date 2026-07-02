import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ShieldCheck, Lock, Activity, ChevronRight } from 'lucide-react';
import { Logo } from '@/shared/ui/Logo/Logo';
import { AuthBackdrop } from '@/shared/ui/Background/AuthBackdrop';

interface AuthShowcaseProps {
  theme: 'light' | 'dark';
}

type Rot = { title: string; subtitle: string };
type Stat = { value: string; label: string };

/**
 * AuthShowcase — the brand panel on the left of the auth split-screen.
 * Hidden below `lg`. Decorative layers (gradient / aurora / grid / scrim) are
 * inline-styled per theme; text uses the app's dark: tokens. Headings inherit
 * Sora from the `.ca-marketing` ancestor set by AuthLayout.
 */
export const AuthShowcase = ({ theme }: AuthShowcaseProps) => {
  const { t } = useTranslation();
  const dark = theme === 'dark';

  const rotator = (t('auth.showcase.rotator', { returnObjects: true }) as Rot[] | string) ?? [];
  const rot: Rot[] = Array.isArray(rotator) ? rotator : [];
  const pipeline =
    (t('auth.showcase.pipeline', { returnObjects: true }) as string[] | string) ?? [];
  const pipe: string[] = Array.isArray(pipeline) ? pipeline : [];
  const stats = (t('auth.showcase.stats', { returnObjects: true }) as Stat[] | string) ?? [];
  const statList: Stat[] = Array.isArray(stats) ? stats : [];
  const trust = (t('auth.showcase.trust', { returnObjects: true }) as string[] | string) ?? [];
  const trustList: string[] = Array.isArray(trust) ? trust : [];
  const trustIcons = [ShieldCheck, Lock, Activity];

  const [i, setI] = useState(0);
  useEffect(() => {
    if (rot.length < 2) return;
    const reduce = window.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false;
    if (reduce) return;
    const id = setInterval(() => setI((v) => (v + 1) % rot.length), 4600);
    return () => clearInterval(id);
  }, [rot.length]);
  const cur = rot[i] ?? rot[0] ?? { title: '', subtitle: '' };

  const accent = dark ? '#22d3ee' : '#0e9bb8';
  const chipStyle = {
    background: dark ? 'rgba(255,255,255,0.05)' : 'rgba(99,102,241,0.06)',
    border: `1px solid ${dark ? 'rgba(255,255,255,0.10)' : 'rgba(99,102,241,0.18)'}`,
  };

  return (
    <div
      className="relative hidden w-[54%] shrink-0 overflow-hidden lg:flex"
      style={{
        background: dark
          ? 'radial-gradient(130% 120% at 28% 18%, #11162e 0%, #0a0e20 46%, #05070f 100%)'
          : 'radial-gradient(130% 120% at 28% 18%, #ffffff 0%, #e9f0fb 46%, #dde6f4 100%)',
      }}
    >
      <AuthBackdrop theme={theme} />

      {/* aurora */}
      <div
        aria-hidden
        className="pointer-events-none absolute -left-[8%] -top-[14%] h-[560px] w-[620px] rounded-full"
        style={{
          background: `radial-gradient(circle at 50% 50%, ${dark ? 'rgba(99,102,241,0.40)' : 'rgba(99,102,241,0.18)'} 0%, transparent 66%)`,
          filter: 'blur(26px)',
        }}
      />
      <div
        aria-hidden
        className="pointer-events-none absolute -right-[10%] bottom-[2%] h-[520px] w-[560px] rounded-full"
        style={{
          background: `radial-gradient(circle at 50% 50%, ${dark ? 'rgba(34,211,238,0.24)' : 'rgba(34,211,238,0.13)'} 0%, transparent 66%)`,
          filter: 'blur(30px)',
        }}
      />
      {/* blueprint grid */}
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0"
        style={{
          backgroundImage: `linear-gradient(${dark ? 'rgba(129,140,248,0.13)' : 'rgba(99,102,241,0.10)'} 1px, transparent 1px), linear-gradient(90deg, ${dark ? 'rgba(129,140,248,0.13)' : 'rgba(99,102,241,0.10)'} 1px, transparent 1px)`,
          backgroundSize: '52px 52px',
          WebkitMaskImage: 'radial-gradient(ellipse 70% 70% at 60% 50%, #000 0%, transparent 78%)',
          maskImage: 'radial-gradient(ellipse 70% 70% at 60% 50%, #000 0%, transparent 78%)',
          opacity: 0.85,
        }}
      />
      {/* legibility scrim */}
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0"
        style={{
          background: dark
            ? 'linear-gradient(105deg, rgba(5,7,15,0.82) 0%, rgba(5,7,15,0.45) 42%, rgba(5,7,15,0.15) 70%, transparent 100%)'
            : 'linear-gradient(105deg, rgba(255,255,255,0.78) 0%, rgba(255,255,255,0.36) 44%, rgba(255,255,255,0.08) 72%, transparent 100%)',
        }}
      />

      {/* content */}
      <div className="relative z-[2] flex h-full flex-col px-[60px] py-[56px]">
        <Logo size={38} />

        <div className="flex-1" />

        <div className="mb-5 flex items-center gap-2.5">
          <span
            className="h-0.5 w-7 rounded-full"
            style={{ background: `linear-gradient(90deg, ${accent}, transparent)` }}
          />
          <span
            className="text-xs font-semibold uppercase tracking-[0.26em]"
            style={{ color: accent }}
          >
            {t('auth.showcase.eyebrow')}
          </span>
        </div>

        <div
          key={i}
          className="min-h-[118px] max-w-[524px] animate-in fade-in slide-in-from-bottom-2 duration-500"
        >
          <h2 className="m-0 text-[38px] font-bold leading-[1.14] tracking-[-0.02em] text-slate-900 dark:text-white">
            {cur.title}
          </h2>
          <p className="mt-3.5 max-w-[448px] text-[16px] leading-relaxed text-slate-600 dark:text-slate-300">
            {cur.subtitle}
          </p>
        </div>

        <div className="mt-7 flex flex-wrap items-center gap-2">
          {pipe.map((step, idx) => (
            <span key={step} className="flex items-center gap-2">
              <span
                className="rounded-full px-3.5 py-[7px] text-[13px] font-semibold text-slate-800 dark:text-slate-100"
                style={chipStyle}
              >
                {step}
              </span>
              {idx < pipe.length - 1 && (
                <ChevronRight size={14} style={{ color: accent, opacity: 0.7 }} />
              )}
            </span>
          ))}
        </div>

        <div className="mt-9 flex gap-10">
          {statList.map((s) => (
            <div key={s.label}>
              <div className="ca-display text-[27px] font-bold tracking-[-0.02em] text-slate-900 dark:text-white">
                {s.value}
              </div>
              <div className="mt-1 text-[12.5px] text-slate-500 dark:text-slate-400">{s.label}</div>
            </div>
          ))}
        </div>

        <div className="mt-auto" />

        <div className="flex items-center gap-6 pt-9 opacity-75">
          {trustList.map((label, idx) => {
            const Icon = trustIcons[idx] ?? ShieldCheck;
            return (
              <div
                key={label}
                className="flex items-center gap-1.5 text-[12.5px] text-slate-600 dark:text-slate-300"
              >
                <Icon size={15} />
                {label}
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
};
