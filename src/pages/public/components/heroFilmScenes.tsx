import { memo } from 'react';
import { useTranslation } from 'react-i18next';
import { CheckCircle2, Flame, Scale, Truck, Warehouse, MapPin } from 'lucide-react';
import { clamp01, smooth, lerp, seg } from './heroFilmUtils';

export interface SceneProps {
  p: number;
}

export const SceneDesign = memo(({ p }: SceneProps) => {
  const { t } = useTranslation();
  const grow = smooth(clamp01((p - 0.05) / 0.7));
  const xR = lerp(372, 540, grow);
  const widthMm = Math.round(lerp(1400, 3200, grow));
  const panels = Math.max(1, Math.round((xR - 300) / 80));
  const mullions = Array.from(
    { length: panels - 1 },
    (_, i) => 300 + ((xR - 300) * (i + 1)) / panels,
  );
  const dimOp = seg(p, 0.45, 0.25);
  const valid = p > 0.82;
  return (
    <div className="relative flex h-full w-full flex-col">
      <div className="px-5 pt-4 sm:px-8">
        <div className="mb-1.5 flex items-center justify-between text-[11px] font-semibold text-slate-500 dark:text-slate-400">
          <span>{t('LandingPage.hero.film.widthLabel')}</span>
          <span className="font-bold text-primary-600 dark:text-primary-300">{widthMm} mm</span>
        </div>
        <div className="relative h-1.5 w-full rounded-full bg-slate-200 dark:bg-slate-700">
          <div
            className="h-full rounded-full bg-gradient-to-r from-primary-500 to-accent-500"
            style={{ width: `${clamp01(grow) * 100}%` }}
          />
          <div
            className="absolute top-1/2 h-4 w-4 -translate-x-1/2 -translate-y-1/2 rounded-full border-2 border-primary-500 bg-white shadow dark:bg-slate-900"
            style={{ left: `${clamp01(grow) * 100}%` }}
          />
        </div>
      </div>
      <div className="relative flex flex-1 items-center justify-center">
        <div className="animate-cam w-[78%] max-w-[420px]">
          <svg viewBox="0 0 600 440" className="h-auto w-full" preserveAspectRatio="xMidYMid meet">
            <defs>
              <linearGradient id="caFilmFront" x1="0" y1="0" x2="1" y2="1">
                <stop
                  offset="0%"
                  className="[stop-color:var(--color-primary-400)]"
                  stopOpacity="0.32"
                />
                <stop
                  offset="100%"
                  className="[stop-color:var(--color-accent-400)]"
                  stopOpacity="0.1"
                />
              </linearGradient>
              <linearGradient id="caFilmSide" x1="0" y1="0" x2="1" y2="0">
                <stop
                  offset="0%"
                  className="[stop-color:var(--color-primary-500)]"
                  stopOpacity="0.24"
                />
                <stop
                  offset="100%"
                  className="[stop-color:var(--color-accent-400)]"
                  stopOpacity="0.05"
                />
              </linearGradient>
            </defs>
            <polygon points="300,90 132,52 132,348 300,386" fill="url(#caFilmSide)" />
            <polygon points={`300,90 ${xR},90 ${xR},386 300,386`} fill="url(#caFilmFront)" />
            <path
              d={`M132,52 L300,90 L${xR},90 M300,90 L300,386 M132,348 L300,386 L${xR},386 L${xR},90`}
              fill="none"
              className="stroke-primary-500 dark:stroke-primary-300"
              strokeWidth="2.5"
              strokeLinejoin="round"
            />
            {mullions.map((mx, i) => (
              <line
                key={i}
                x1={mx}
                y1="90"
                x2={mx}
                y2="386"
                className="stroke-primary-400/50 dark:stroke-primary-300/40"
                strokeWidth="1.4"
              />
            ))}
            <g className="fill-primary-600 dark:fill-primary-300">
              <circle cx="300" cy="90" r="5.5" />
              <circle cx={xR} cy="90" r="5.5" />
              <circle cx="300" cy="386" r="5.5" />
              <circle cx="132" cy="52" r="5.5" />
            </g>
            <g
              className="stroke-accent-500 dark:stroke-accent-300"
              strokeWidth="1.4"
              style={{ opacity: dimOp }}
            >
              <line x1="300" y1="68" x2={xR} y2="68" />
              <line x1="300" y1="62" x2="300" y2="74" />
              <line x1={xR} y1="62" x2={xR} y2="74" />
            </g>
            <path
              d="M300 124 A34 34 0 0 0 266 104"
              fill="none"
              className="stroke-accent-500 dark:stroke-accent-300"
              strokeWidth="1.6"
              style={{ opacity: dimOp }}
            />
            <text
              x="250"
              y="132"
              className="fill-accent-600 dark:fill-accent-300"
              fontSize="13"
              style={{ opacity: dimOp }}
            >
              90°
            </text>
          </svg>
        </div>
        <div
          className="absolute bottom-3 left-4 inline-flex items-center gap-1.5 rounded-lg bg-success-500/15 px-3 py-1.5 text-xs font-bold text-success-700 backdrop-blur dark:text-success-300"
          style={{
            opacity: valid ? 1 : 0,
            transform: `translateY(${valid ? 0 : 8}px)`,
            transition: 'opacity .3s, transform .3s',
          }}
        >
          <CheckCircle2 size={13} />
          {t('LandingPage.hero.showcase.constraints')}
        </div>
      </div>
    </div>
  );
});
SceneDesign.displayName = 'SceneDesign';

export const SceneQuote = memo(({ p }: SceneProps) => {
  const { t } = useTranslation();
  const area = lerp(0, 5.28, seg(p, 0.05, 0.35));
  const price = Math.round(lerp(0, 6840, seg(p, 0.3, 0.5)));
  const rows = [
    { k: t('LandingPage.hero.film.quoteRowGlass'), v: '€ 4.180', at: 0.3 },
    { k: t('LandingPage.hero.film.quoteRowProfile'), v: '€ 1.910', at: 0.42 },
    { k: t('LandingPage.hero.film.quoteRowHardware'), v: '€ 750', at: 0.54 },
  ];
  const ready = p > 0.85;
  return (
    <div className="flex h-full w-full items-center justify-center p-4 sm:p-6">
      <div className="w-full max-w-md rounded-2xl border border-slate-200/70 bg-white/85 p-5 shadow-lg backdrop-blur dark:border-white/10 dark:bg-slate-900/80">
        <div className="mb-4 flex items-center justify-between border-b border-slate-100 pb-3 dark:border-slate-800">
          <span className="text-sm font-bold text-slate-900 dark:text-white">
            {t('LandingPage.hero.film.quoteTitleFull')}
          </span>
          <span className="rounded-md bg-primary-500/10 px-2 py-0.5 text-[10px] font-bold text-primary-600 dark:text-primary-300">
            Q-2026-0421
          </span>
        </div>
        <div className="mb-4 flex items-center justify-between text-xs text-slate-500 dark:text-slate-400">
          <span>{t('LandingPage.hero.showcase.quoteArea')}</span>
          <span className="font-bold text-slate-900 dark:text-white">{area.toFixed(2)} m²</span>
        </div>
        <ul className="space-y-2">
          {rows.map((r) => {
            const o = clamp01((p - r.at) / 0.08);
            return (
              <li
                key={r.k}
                className="flex items-center justify-between text-sm"
                style={{ opacity: o, transform: `translateX(${(1 - o) * 10}px)` }}
              >
                <span className="text-slate-600 dark:text-slate-300">{r.k}</span>
                <span className="font-semibold text-slate-900 dark:text-white">{r.v}</span>
              </li>
            );
          })}
        </ul>
        <div className="mt-4 flex items-end justify-between border-t border-slate-100 pt-3 dark:border-slate-800">
          <div>
            <div className="text-[10px] uppercase tracking-wider text-slate-400">
              {t('LandingPage.hero.showcase.quotePrice')}
            </div>
            <div className="ca-display text-2xl font-extrabold text-primary-600 dark:text-primary-300">
              € {price.toLocaleString('tr-TR')}
            </div>
          </div>
          <span
            className="inline-flex items-center gap-1.5 rounded-full bg-success-500/10 px-3 py-1 text-xs font-bold text-success-600 dark:text-success-300"
            style={{
              opacity: ready ? 1 : 0,
              transform: `scale(${ready ? 1 : 0.85})`,
              transition: 'opacity .3s, transform .3s',
            }}
          >
            <CheckCircle2 size={14} />
            {t('LandingPage.hero.film.readyToBuild')}
          </span>
        </div>
      </div>
    </div>
  );
});
SceneQuote.displayName = 'SceneQuote';

export const SceneMrp = memo(({ p }: SceneProps) => {
  const { t } = useTranslation();
  const yieldVal = lerp(0, 98.8, seg(p, 0.15, 0.55));
  const bars = [62, 88, 74, 95, 80];
  return (
    <div className="flex h-full w-full items-center justify-center gap-4 p-4 sm:gap-7 sm:p-6">
      <div className="w-1/2 max-w-[230px]">
        <div className="mb-2 text-[10px] font-bold uppercase tracking-wider text-slate-400">
          {t('LandingPage.hero.film.nestingLabel')}
        </div>
        <div className="grid grid-cols-4 grid-rows-4 gap-1 rounded-xl border border-slate-200/70 bg-white/60 p-2 dark:border-white/10 dark:bg-slate-900/60">
          {Array.from({ length: 16 }).map((_, i) => {
            const filled = p > 0.15 + (i / 16) * 0.6 && i < 15;
            return (
              <span
                key={i}
                className="aspect-square rounded-[3px] transition-colors duration-200"
                style={{
                  background: filled
                    ? 'linear-gradient(135deg, var(--color-primary-500), var(--color-primary-600))'
                    : 'var(--color-slate-200, #e2e8f0)',
                }}
              />
            );
          })}
        </div>
        <div className="mt-2 inline-flex items-baseline gap-1.5">
          <span className="ca-display text-lg font-extrabold text-success-600 dark:text-success-300">
            %{yieldVal.toFixed(1)}
          </span>
          <span className="text-[10px] font-medium text-slate-400">
            {t('LandingPage.hero.showcase.mrpYield')}
          </span>
        </div>
      </div>
      <div className="w-1/2 max-w-[210px]">
        <div className="mb-2 flex items-center gap-1.5 text-[10px] font-bold uppercase tracking-wider text-slate-400">
          <Flame size={12} className="text-warning-500" />
          {t('LandingPage.hero.film.furnaceLoad')}
        </div>
        <div className="flex h-28 items-end justify-between gap-2 rounded-xl border border-slate-200/70 bg-white/60 p-2.5 dark:border-white/10 dark:bg-slate-900/60">
          {bars.map((h, i) => (
            <div
              key={i}
              className="w-full rounded-t bg-gradient-to-t from-warning-500 to-warning-400"
              style={{ height: `${h * seg(p, 0.2 + i * 0.06, 0.4)}%` }}
            />
          ))}
        </div>
      </div>
    </div>
  );
});
SceneMrp.displayName = 'SceneMrp';

export const SceneShipping = memo(({ p }: SceneProps) => {
  const { t } = useTranslation();
  const prog = smooth(clamp01((p - 0.1) / 0.7));
  const stops = [
    { icon: Warehouse, label: t('LandingPage.hero.film.shipWarehouse'), at: 0 },
    { icon: Truck, label: t('LandingPage.hero.film.shipTransit'), at: 0.5 },
    { icon: MapPin, label: t('LandingPage.hero.film.shipDelivered'), at: 1 },
  ];
  return (
    <div className="flex h-full w-full flex-col items-center justify-center p-5 sm:p-8">
      <div className="w-full max-w-lg">
        <div className="mb-6 flex items-center justify-between text-[10px] font-bold uppercase tracking-wider text-slate-400">
          <span>{t('LandingPage.hero.film.shipping.title')} · SHP-2026-1187</span>
          <span className="text-success-600 dark:text-success-300">
            {t('LandingPage.hero.film.eta')}: 2 {t('LandingPage.hero.film.days')}
          </span>
        </div>
        <div className="relative mx-2 h-1.5 rounded-full bg-slate-200 dark:bg-slate-700">
          <div
            className="h-full rounded-full bg-gradient-to-r from-success-500 to-primary-500"
            style={{ width: `${prog * 100}%` }}
          />
          <div
            className="absolute -top-3.5 flex h-8 w-8 -translate-x-1/2 items-center justify-center rounded-full bg-primary-600 text-white shadow-lg"
            style={{ left: `${prog * 100}%` }}
          >
            <Truck size={15} />
          </div>
          {stops.map((s, i) => {
            const reached = prog >= s.at - 0.02;
            const Icon = s.icon;
            return (
              <div
                key={i}
                className="absolute w-16 -translate-x-1/2"
                style={{ left: `${s.at * 100}%`, top: '22px' }}
              >
                <div
                  className={`mx-auto flex h-8 w-8 items-center justify-center rounded-full border-2 transition-colors duration-300 ${
                    reached
                      ? 'border-success-500 bg-success-500/15 text-success-600 dark:text-success-300'
                      : 'border-slate-300 bg-white text-slate-400 dark:border-slate-600 dark:bg-slate-800'
                  }`}
                >
                  <Icon size={15} />
                </div>
                <div className="mt-1.5 text-center text-[10px] font-semibold text-slate-500 dark:text-slate-400">
                  {s.label}
                </div>
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
});
SceneShipping.displayName = 'SceneShipping';

export const SceneLedger = memo(({ p }: SceneProps) => {
  const { t } = useTranslation();
  const total = lerp(0, 18540, seg(p, 0.2, 0.5));
  const fmt = (n: number) => `€ ${Math.round(n).toLocaleString('tr-TR')}`;
  const lines = [
    { acc: '120 · Alıcılar', debit: total, credit: 0, at: 0.2 },
    { acc: '600 · Yurtiçi Satışlar', debit: 0, credit: total * 0.83, at: 0.38 },
    { acc: '391 · Hesaplanan KDV', debit: 0, credit: total * 0.17, at: 0.56 },
  ];
  const balanced = p > 0.82;
  return (
    <div className="flex h-full w-full items-center justify-center p-4 sm:p-6">
      <div className="w-full max-w-md rounded-2xl border border-slate-200/70 bg-white/85 p-5 shadow-lg backdrop-blur dark:border-white/10 dark:bg-slate-900/80">
        <div className="mb-3 flex items-center justify-between text-[10px] font-bold uppercase tracking-wider text-slate-400">
          <span>{t('LandingPage.hero.film.postedLabel')}</span>
          <span className="rounded bg-success-500/10 px-2 py-0.5 text-success-600 dark:text-success-300">
            ● {t('LandingPage.hero.showcase.live')}
          </span>
        </div>
        <table className="w-full text-left text-xs">
          <thead className="text-[10px] uppercase tracking-wider text-slate-400">
            <tr>
              <th className="pb-2 font-semibold">{t('LandingPage.hero.film.account')}</th>
              <th className="pb-2 text-right font-semibold">{t('LandingPage.hero.film.debit')}</th>
              <th className="pb-2 text-right font-semibold">{t('LandingPage.hero.film.credit')}</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
            {lines.map((l) => {
              const o = clamp01((p - l.at) / 0.1);
              return (
                <tr key={l.acc} style={{ opacity: o }}>
                  <td className="py-2 font-medium text-slate-700 dark:text-slate-200">{l.acc}</td>
                  <td className="py-2 text-right font-semibold text-success-600 dark:text-success-300">
                    {l.debit ? fmt(l.debit) : '—'}
                  </td>
                  <td className="py-2 text-right font-semibold text-primary-600 dark:text-primary-300">
                    {l.credit ? fmt(l.credit) : '—'}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
        <div
          className="mt-4 flex items-center justify-center gap-2 rounded-xl bg-success-500/10 py-2.5 text-sm font-bold text-success-700 dark:text-success-300"
          style={{ opacity: balanced ? 1 : 0.25, transition: 'opacity .4s' }}
        >
          <Scale size={15} />
          {t('LandingPage.hero.showcase.ledgerBalanced')}
          <CheckCircle2 size={15} />
        </div>
      </div>
    </div>
  );
});
SceneLedger.displayName = 'SceneLedger';
