import { useCallback, useEffect, useRef, useState, type ComponentType } from 'react';
import { useTranslation } from 'react-i18next';
import { MousePointer2, Sparkles } from 'lucide-react';
import { clamp01, lerp, smooth } from './heroFilmUtils';
import {
  SceneDesign,
  SceneQuote,
  SceneMrp,
  SceneShipping,
  SceneLedger,
  type SceneProps,
} from './heroFilmScenes';

const SHOTS: { key: string; dur: number; Scene: ComponentType<SceneProps> }[] = [
  { key: 'design', dur: 5.5, Scene: SceneDesign },
  { key: 'quote', dur: 5, Scene: SceneQuote },
  { key: 'production', dur: 5, Scene: SceneMrp },
  { key: 'shipping', dur: 4.5, Scene: SceneShipping },
  { key: 'accounting', dur: 5.5, Scene: SceneLedger },
];

const STARTS = SHOTS.reduce<number[]>((acc, _s, i) => {
  acc.push(i === 0 ? 0 : acc[i - 1] + SHOTS[i - 1].dur);
  return acc;
}, []);
const TOTAL = STARTS[STARTS.length - 1] + SHOTS[SHOTS.length - 1].dur;
const CROSS = 0.7;

type CursorKey = { p: number; x: number; y: number; click?: boolean };
const TRACKS: Record<string, CursorKey[]> = {
  design: [
    { p: 0, x: 12, y: 13, click: true },
    { p: 1, x: 86, y: 13, click: true },
  ],
  quote: [
    { p: 0, x: 46, y: 40 },
    { p: 0.18, x: 50, y: 31, click: true },
    { p: 0.6, x: 40, y: 74 },
    { p: 1, x: 40, y: 74 },
  ],
  production: [
    { p: 0, x: 30, y: 44, click: true },
    { p: 0.5, x: 46, y: 52 },
    { p: 1, x: 60, y: 42 },
  ],
  shipping: [
    { p: 0, x: 50, y: 30, click: true },
    { p: 0.22, x: 14, y: 47 },
    { p: 1, x: 84, y: 47 },
  ],
  accounting: [
    { p: 0, x: 54, y: 27, click: true },
    { p: 0.45, x: 44, y: 52 },
    { p: 1, x: 44, y: 74 },
  ],
};

const sampleCursor = (track: CursorKey[], p: number) => {
  let a = track[0];
  let b = track[track.length - 1];
  for (let i = 0; i < track.length - 1; i += 1) {
    if (p >= track[i].p && p <= track[i + 1].p) {
      a = track[i];
      b = track[i + 1];
      break;
    }
  }
  const span = b.p - a.p || 1;
  const k = smooth(clamp01((p - a.p) / span));
  return { x: lerp(a.x, b.x, k), y: lerp(a.y, b.y, k) };
};
const isClicking = (track: CursorKey[], p: number) =>
  track.some((kf) => kf.click && Math.abs(p - kf.p) < 0.07);

const useFilmClock = (running: boolean) => {
  const [t, setT] = useState(0);
  const acc = useRef(0);
  const last = useRef(0);
  const frame = useRef(-1);
  useEffect(() => {
    if (!running) return;
    let raf = 0;
    last.current = 0;
    const loop = (ts: number) => {
      if (!last.current) last.current = ts;
      acc.current = (acc.current + (ts - last.current) / 1000) % TOTAL;
      last.current = ts;
      const f = Math.floor(acc.current * 30);
      if (f !== frame.current) {
        frame.current = f;
        setT(acc.current);
      }
      raf = requestAnimationFrame(loop);
    };
    raf = requestAnimationFrame(loop);
    return () => cancelAnimationFrame(raf);
  }, [running]);
  const seek = useCallback((to: number) => {
    acc.current = ((to % TOTAL) + TOTAL) % TOTAL;
    last.current = 0;
    frame.current = -1;
    setT(acc.current);
  }, []);
  return [t, seek] as const;
};

const opacityFor = (i: number, t: number) => {
  const s = STARTS[i];
  const e = s + SHOTS[i].dur;
  const fin = i === 0 ? 1 : clamp01((t - (s - CROSS / 2)) / CROSS);
  const fout = clamp01((e + CROSS / 2 - t) / CROSS);
  return Math.min(fin, fout);
};

export const HeroFilm = () => {
  const { t } = useTranslation();
  const [paused, setPaused] = useState(false);
  const [visible, setVisible] = useState(true);
  const [reduced] = useState(
    () =>
      typeof window !== 'undefined' &&
      !!window.matchMedia?.('(prefers-reduced-motion: reduce)').matches,
  );
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const el = ref.current;
    if (!el || typeof IntersectionObserver === 'undefined') return;
    const io = new IntersectionObserver(([entry]) => setVisible(entry.isIntersecting), {
      threshold: 0.15,
    });
    io.observe(el);
    return () => io.disconnect();
  }, []);

  const [clock, seek] = useFilmClock(!reduced && !paused && visible);
  const active = STARTS.reduce((acc, s, i) => (clock >= s ? i : acc), 0);
  const activeKey = SHOTS[active].key;
  const pActive = clamp01((clock - STARTS[active]) / SHOTS[active].dur);
  const cursor = sampleCursor(TRACKS[activeKey], pActive);
  const cursorOpacity = clamp01(Math.min(pActive / 0.08, (1 - pActive) / 0.08, 1));
  const clicking = isClicking(TRACKS[activeKey], pActive);

  if (reduced) {
    return (
      <div ref={ref} className="relative mx-auto w-full max-w-4xl">
        <div className="ca-panel grid gap-3 rounded-3xl p-6 sm:grid-cols-5">
          {SHOTS.map((s, i) => (
            <div
              key={s.key}
              className="rounded-2xl border border-slate-200/60 p-4 dark:border-white/10"
            >
              <span className="text-[10px] font-bold text-primary-600 dark:text-primary-300">
                0{i + 1}
              </span>
              <h3 className="mt-1 text-sm font-bold text-slate-900 dark:text-white">
                {t(`LandingPage.hero.film.${s.key}.title`)}
              </h3>
              <p className="mt-1 text-xs leading-relaxed text-slate-500 dark:text-slate-400">
                {t(`LandingPage.hero.film.${s.key}.caption`)}
              </p>
            </div>
          ))}
        </div>
      </div>
    );
  }

  return (
    <div
      ref={ref}
      className="relative mx-auto w-full max-w-6xl xl:max-w-[1500px] 2xl:max-w-[1600px]"
      onMouseEnter={() => setPaused(true)}
      onMouseLeave={() => setPaused(false)}
    >
      <div
        aria-hidden="true"
        className="ca-aurora pointer-events-none absolute -inset-12 -z-10 opacity-60 dark:opacity-70"
      />

      <div className="ca-panel relative overflow-hidden rounded-[1.5rem] p-2.5 shadow-2xl sm:rounded-[2rem] sm:p-3">
        <div className="flex items-center gap-3 px-2 pb-3 pt-1.5">
          <span className="flex shrink-0 gap-1.5">
            <span className="h-2.5 w-2.5 rounded-full bg-danger-400/80" />
            <span className="h-2.5 w-2.5 rounded-full bg-warning-400/80" />
            <span className="h-2.5 w-2.5 rounded-full bg-success-400/80" />
          </span>
          <div className="flex min-w-0 flex-1 items-center gap-2 rounded-lg border border-slate-200/70 bg-slate-100/70 px-3 py-1 text-[11px] text-slate-500 dark:border-white/10 dark:bg-white/5 dark:text-slate-400">
            <span className="h-1.5 w-1.5 shrink-0 rounded-full bg-success-500" />
            <span className="truncate">
              corealign.app/
              <span className="font-semibold text-slate-700 dark:text-slate-200">
                {t(`LandingPage.hero.film.${activeKey}.url`)}
              </span>
            </span>
          </div>
          <span className="hidden shrink-0 items-center gap-1.5 rounded-full bg-primary-500/10 px-2.5 py-1 text-[10px] font-bold uppercase tracking-wider text-primary-600 sm:inline-flex dark:text-primary-300">
            <Sparkles size={11} />
            {t('LandingPage.hero.film.tour')}
          </span>
        </div>

        <div className="relative aspect-[4/3] overflow-hidden rounded-xl border border-slate-200/60 bg-gradient-to-br from-slate-50 to-slate-100/60 sm:aspect-[16/10] sm:rounded-2xl lg:aspect-[16/9] dark:border-white/5 dark:from-surface-deep dark:to-canvas">
          <div
            aria-hidden="true"
            className="ca-blueprint pointer-events-none absolute inset-0 opacity-25 [mask-image:radial-gradient(ellipse_at_center,black_30%,transparent_82%)]"
          />
          {SHOTS.map((s, i) => {
            const op = opacityFor(i, clock);
            if (op <= 0.001) return null;
            const p = clamp01((clock - STARTS[i]) / s.dur);
            const Scene = s.Scene;
            return (
              <div
                key={s.key}
                className="absolute inset-0"
                style={{
                  opacity: op,
                  transform: `scale(${1 + 0.045 * smooth(p)})`,
                  transformOrigin: 'center 45%',
                }}
              >
                <Scene p={p} />
              </div>
            );
          })}

          <div
            aria-hidden="true"
            className="pointer-events-none absolute z-20"
            style={{
              left: `${cursor.x}%`,
              top: `${cursor.y}%`,
              opacity: cursorOpacity,
              transition: 'left .12s linear, top .12s linear, opacity .2s',
            }}
          >
            {clicking && (
              <span className="absolute -left-2 -top-2 h-7 w-7 animate-ping rounded-full bg-primary-500/40" />
            )}
            <MousePointer2
              size={22}
              className="relative -left-1 -top-1 fill-white text-slate-900 drop-shadow-md dark:fill-slate-900 dark:text-white"
            />
          </div>

          <div className="pointer-events-none absolute inset-x-0 bottom-0 bg-gradient-to-t from-slate-900/70 to-transparent p-3 sm:p-4">
            <div
              key={activeKey}
              className="animate-fade-up flex items-center gap-2 text-xs font-semibold text-white sm:text-sm"
            >
              <span className="rounded-md bg-primary-500 px-1.5 py-0.5 text-[10px] font-bold tabular-nums">
                {active + 1}/{SHOTS.length}
              </span>
              {t(`LandingPage.hero.film.${activeKey}.caption`)}
            </div>
          </div>
        </div>
      </div>

      <div className="mx-auto mt-3 grid max-w-3xl grid-cols-2 gap-2 sm:grid-cols-5" role="tablist">
        {SHOTS.map((s, i) => {
          const on = i === active;
          const done = i < active;
          return (
            <button
              key={s.key}
              type="button"
              role="tab"
              aria-selected={on}
              onClick={() => seek(STARTS[i] + 0.001)}
              className="group flex flex-col gap-2 rounded-xl px-1 py-1.5 text-left"
            >
              <span className="h-1 w-full overflow-hidden rounded-full bg-slate-200 dark:bg-slate-700">
                <span
                  className="block h-full origin-left rounded-full bg-primary-500"
                  style={{ transform: `scaleX(${done ? 1 : on ? pActive : 0})` }}
                />
              </span>
              <span className="flex items-center gap-1.5">
                <span
                  className={`text-[10px] font-bold tabular-nums ${on ? 'text-primary-600 dark:text-primary-300' : 'text-slate-400'}`}
                >
                  0{i + 1}
                </span>
                <span
                  className={`truncate text-xs font-semibold transition-colors ${
                    on
                      ? 'text-slate-900 dark:text-white'
                      : 'text-slate-500 group-hover:text-slate-700 dark:text-slate-400 dark:group-hover:text-slate-200'
                  }`}
                >
                  {t(`LandingPage.hero.film.${s.key}.title`)}
                </span>
              </span>
            </button>
          );
        })}
      </div>
    </div>
  );
};
