// CoreAlign — cinematic landing hero (2.5D motion design)
// Reads the timeline engine globals set by animations.jsx (loaded first via x-import).
const { Stage, Sprite, useTime, useTimeline, useSprite, Easing, interpolate, animate, clamp } =
  window;

/* ── Design tokens ───────────────────────────────────────────────────────── */
const C = {
  indigo: '#5b5ee8',
  indigoL: '#7c84f0',
  indigoD: '#4f46e5',
  cyan: '#0e9bb8',
  cyanL: '#1fa9c9',
  emerald: '#0f9d6b',
  amber: '#cf7c1e',
  violet: '#7c5cf0',
  pink: '#db4f93',
  sky: '#0d92cf',
  hot: '#ef6c2e',
  cold: '#3b6fff',
  text: '#1b2236',
  muted: '#586089',
  faint: '#9aa1bd',
};
const SORA = "'Sora', system-ui, sans-serif";
const INTER = "'Inter', system-ui, sans-serif";
const MONO = "'JetBrains Mono', ui-monospace, monospace";

/* ── helpers ─────────────────────────────────────────────────────────────── */
const eo = Easing.easeOutCubic,
  ei = Easing.easeInCubic,
  eio = Easing.easeInOutCubic,
  eob = Easing.easeOutBack,
  eoq = Easing.easeOutQuart,
  eoe = Easing.easeOutExpo,
  es = Easing.easeOutSine;
const seg = (t, a, b, e = eo) => e(clamp((t - a) / Math.max(0.0001, b - a), 0, 1));
const win = (t, a, b, f = 0.5) =>
  Math.min(eio(clamp((t - a) / f, 0, 1)), eio(clamp((b - t) / f, 0, 1)));
const lerp = (a, b, t) => a + (b - a) * t;

function hexA(hex, a) {
  const n = parseInt(hex.slice(1), 16);
  return `rgba(${(n >> 16) & 255},${(n >> 8) & 255},${n & 255},${a})`;
}
function lerpHex(h1, h2, t) {
  const a = parseInt(h1.slice(1), 16),
    b = parseInt(h2.slice(1), 16);
  const r = Math.round(lerp((a >> 16) & 255, (b >> 16) & 255, t));
  const g = Math.round(lerp((a >> 8) & 255, (b >> 8) & 255, t));
  const c = Math.round(lerp(a & 255, b & 255, t));
  return `rgb(${r},${g},${c})`;
}
function mulberry32(a) {
  return function () {
    a |= 0;
    a = (a + 0x6d2b79f5) | 0;
    let t = Math.imul(a ^ (a >>> 15), 1 | a);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}
const RNG = mulberry32(20260626);
const STARS = Array.from({ length: 104 }, () => ({
  x: RNG() * 1920,
  y: RNG() * 900,
  r: RNG() * 1.5 + 0.35,
  p: RNG() * 6.28,
  s: 0.4 + RNG() * 0.7,
}));
const DUST = Array.from({ length: 46 }, () => ({
  x: RNG() * 1920,
  y: RNG() * 1080,
  r: RNG() * 2.4 + 0.8,
  p: RNG() * 6.28,
  sp: 0.2 + RNG() * 0.5,
  dx: RNG() - 0.5,
}));

function trNum(n) {
  try {
    return n.toLocaleString('tr-TR');
  } catch (e) {
    return String(n);
  }
}

// smooth path (Catmull-Rom -> cubic bezier) for premium chart lines
function smoothPath(pts) {
  if (pts.length < 2) return '';
  let d = `M ${pts[0][0].toFixed(1)} ${pts[0][1].toFixed(1)}`;
  for (let i = 0; i < pts.length - 1; i++) {
    const p0 = pts[i - 1] || pts[i],
      p1 = pts[i],
      p2 = pts[i + 1],
      p3 = pts[i + 2] || p2;
    const c1x = p1[0] + (p2[0] - p0[0]) / 6,
      c1y = p1[1] + (p2[1] - p0[1]) / 6;
    const c2x = p2[0] - (p3[0] - p1[0]) / 6,
      c2y = p2[1] - (p3[1] - p1[1]) / 6;
    d += ` C ${c1x.toFixed(1)} ${c1y.toFixed(1)}, ${c2x.toFixed(1)} ${c2y.toFixed(1)}, ${p2[0].toFixed(1)} ${p2[1].toFixed(1)}`;
  }
  return d;
}

/* ── Glyph (compact line icons) ──────────────────────────────────────────── */
function Glyph({ type, color, size = 21 }) {
  const p = {
    fill: 'none',
    stroke: color,
    strokeWidth: 1.7,
    strokeLinecap: 'round',
    strokeLinejoin: 'round',
  };
  const paths = {
    cube: (
      <g {...p}>
        <path d="M11 2.5 19 7v8l-8 4.5L3 15V7z" />
        <path d="M3 7l8 4.5L19 7M11 11.5V20" />
      </g>
    ),
    quote: (
      <g {...p}>
        <rect x="3.5" y="3" width="14" height="16" rx="2" />
        <path d="M7 8h7M7 11.5h7M7 15h4" />
      </g>
    ),
    cost: (
      <g {...p}>
        <circle cx="11" cy="11" r="7.5" />
        <path d="M13.5 8.2c-.6-.8-1.6-1.2-2.6-1.2-1.7 0-2.7.9-2.7 2 0 2.7 5.4 1.4 5.4 4.1 0 1.2-1.1 2.1-2.8 2.1-1.1 0-2.1-.4-2.7-1.3M11 5.4v11.2" />
      </g>
    ),
    bom: (
      <g {...p}>
        <rect x="3.5" y="3.5" width="6.2" height="6.2" rx="1.3" />
        <rect x="12.3" y="3.5" width="6.2" height="6.2" rx="1.3" />
        <rect x="3.5" y="12.3" width="6.2" height="6.2" rx="1.3" />
        <rect x="12.3" y="12.3" width="6.2" height="6.2" rx="1.3" />
      </g>
    ),
    cut: (
      <g {...p}>
        <circle cx="6" cy="6" r="2.4" />
        <circle cx="6" cy="16" r="2.4" />
        <path d="M7.8 7.6 19 16M7.8 14.4 19 6" />
      </g>
    ),
    furnace: (
      <g {...p}>
        <rect x="3.5" y="4" width="15" height="14" rx="2" />
        <path d="M3.5 14h15" />
        <path d="M8 7.5c0 1.2 1.4 1.6 1.4 3 0 .7-.5 1.3-1.4 1.3M13 7c0 1.6 1.6 2 1.6 3.6 0 .8-.6 1.5-1.6 1.5" />
      </g>
    ),
    gantt: (
      <g {...p}>
        <path d="M3.5 4.5h9M3.5 9h13M3.5 13.5h7M3.5 18h11" />
      </g>
    ),
    user: (
      <g {...p}>
        <circle cx="11" cy="7.5" r="3.4" />
        <path d="M4.5 18.5c0-3.4 2.9-5.6 6.5-5.6s6.5 2.2 6.5 5.6" />
      </g>
    ),
    box: (
      <g {...p}>
        <path d="M11 2.6 19 7v8l-8 4.4L3 15V7z" />
        <path d="M3 7l8 4.4 8-4.4M11 11.4V19.8" />
      </g>
    ),
    order: (
      <g {...p}>
        <rect x="4" y="2.6" width="14" height="16.8" rx="2" />
        <path d="M7.5 7h7M7.5 11h7M7.5 15h4.5" />
      </g>
    ),
    invoice: (
      <g {...p}>
        <path d="M5 2.6h12v16.8l-2.3-1.4-2.3 1.4-2.4-1.4-2.3 1.4L5 17.8z" />
        <path d="M8.5 8h5M8.5 11.6h5" />
      </g>
    ),
    b2b: (
      <g {...p}>
        <circle cx="6" cy="6" r="2.3" />
        <circle cx="16" cy="6" r="2.3" />
        <circle cx="11" cy="16.5" r="2.3" />
        <path d="M6.6 8.1 10 14.4M15.4 8.1 12 14.4" />
      </g>
    ),
    pos: (
      <g {...p}>
        <rect x="2.8" y="5" width="16.4" height="11.5" rx="2" />
        <path d="M2.8 9h16.4M6 13h3" />
      </g>
    ),
    ledger: (
      <g {...p}>
        <path d="M11 3v15" />
        <path d="M4.5 6.5h13" />
        <path d="M4.5 6.5c0 2.4 1.6 4 3.2 4s3.2-1.6 3.2-4M11 6.5c0 2.4 1.6 4 3.2 4s3.2-1.6 3.2-4M5.5 18.5h11" />
      </g>
    ),
    chart: (
      <g {...p}>
        <path d="M3.5 3.5v15h15" />
        <path d="M6.5 14l3.2-4 3 2.2 4.3-6" />
      </g>
    ),
    bars: (
      <g {...p}>
        <path d="M3.5 18.5h15" />
        <rect x="5" y="11" width="2.6" height="6" />
        <rect x="9.7" y="7" width="2.6" height="10" />
        <rect x="14.4" y="9.5" width="2.6" height="7.5" />
      </g>
    ),
    spark: (
      <g {...p}>
        <path d="M3 13.5 7 9l3 2.4L15 5l4 3" />
      </g>
    ),
  };
  return (
    <svg width={size} height={size} viewBox="0 0 22 22" style={{ display: 'block' }}>
      {paths[type] || paths.bom}
    </svg>
  );
}

/* ── Pill (status) ───────────────────────────────────────────────────────── */
function Pill({ text, accent }) {
  return (
    <div
      style={{
        display: 'flex',
        alignItems: 'center',
        gap: 6,
        padding: '4px 10px',
        borderRadius: 999,
        background: hexA(accent, 0.12),
        border: `1px solid ${hexA(accent, 0.32)}`,
      }}
    >
      <span
        style={{
          width: 6,
          height: 6,
          borderRadius: 99,
          background: accent,
          boxShadow: `0 0 8px ${accent}`,
        }}
      />
      <span
        style={{
          fontFamily: INTER,
          fontSize: 12.5,
          fontWeight: 600,
          color: hexA(accent, 0.95),
          letterSpacing: '0.01em',
        }}
      >
        {text}
      </span>
    </div>
  );
}

/* ── mini charts ─────────────────────────────────────────────────────────── */
function Sparkline({ accent, w = 300, h = 78, reveal = 1, seed = 3, rising = true }) {
  const pts = React.useMemo(() => {
    const r = mulberry32(seed * 97 + 11);
    const n = 16,
      arr = [];
    let v = 0.45;
    for (let i = 0; i <= n; i++) {
      v = clamp(v + (r() - 0.42) * 0.16 + (rising ? 0.028 : -0.01), 0.08, 0.95);
      arr.push([(i / n) * w, h - 6 - v * (h - 16)]);
    }
    return arr;
  }, [w, h, seed, rising]);
  const d = smoothPath(pts);
  const area = d + ` L ${w} ${h} L 0 ${h} Z`;
  const id = 'sl' + seed;
  const len = w * 1.45;
  return (
    <svg
      width={w}
      height={h}
      viewBox={`0 0 ${w} ${h}`}
      style={{ display: 'block', overflow: 'visible' }}
    >
      <defs>
        <linearGradient id={id} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0" stopColor={accent} stopOpacity="0.32" />
          <stop offset="1" stopColor={accent} stopOpacity="0" />
        </linearGradient>
      </defs>
      <path d={area} fill={`url(#${id})`} opacity={clamp((reveal - 0.15) / 0.85, 0, 1)} />
      <path
        d={d}
        fill="none"
        stroke={accent}
        strokeWidth="2.4"
        strokeLinecap="round"
        strokeDasharray={len}
        strokeDashoffset={len * (1 - reveal)}
        style={{ filter: `drop-shadow(0 0 6px ${hexA(accent, 0.5)})` }}
      />
      {reveal > 0.97 && (
        <circle
          cx={pts[pts.length - 1][0]}
          cy={pts[pts.length - 1][1]}
          r="3.6"
          fill="#fff"
          stroke={accent}
          strokeWidth="2"
        />
      )}
    </svg>
  );
}
function Bars({
  accent,
  reveal = 1,
  data = [0.5, 0.78, 0.42, 0.9, 0.6, 0.84, 0.5, 0.7],
  w = 300,
  h = 78,
}) {
  const gap = 8,
    bw = (w - gap * (data.length - 1)) / data.length;
  return (
    <svg width={w} height={h} viewBox={`0 0 ${w} ${h}`} style={{ display: 'block' }}>
      {data.map((v, i) => {
        const bh = v * (h - 8) * clamp(reveal - i * 0.04, 0, 1);
        return (
          <rect
            key={i}
            x={i * (bw + gap)}
            y={h - bh}
            width={bw}
            height={Math.max(0.5, bh)}
            rx={Math.min(4, bw / 2)}
            fill={i % 2 ? accent : hexA(accent, 0.38)}
          />
        );
      })}
    </svg>
  );
}
function ProgressRows({
  accent,
  reveal = 1,
  rows = [
    ['Cam', 0.86],
    ['Profil', 0.64],
    ['Donanım', 0.42],
  ],
}) {
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 11, width: '100%' }}>
      {rows.map(([label, v], i) => (
        <div key={i} style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
          <span
            style={{ fontFamily: INTER, fontSize: 13, color: C.muted, width: 74, flexShrink: 0 }}
          >
            {label}
          </span>
          <div
            style={{
              flex: 1,
              height: 7,
              borderRadius: 99,
              background: 'rgba(40,52,92,0.10)',
              overflow: 'hidden',
            }}
          >
            <div
              style={{
                height: '100%',
                width: `${v * 100 * clamp(reveal - i * 0.12, 0, 1)}%`,
                borderRadius: 99,
                background: `linear-gradient(90deg, ${hexA(accent, 0.6)}, ${accent})`,
                boxShadow: `0 0 10px ${hexA(accent, 0.5)}`,
              }}
            />
          </div>
        </div>
      ))}
    </div>
  );
}

/* ── Card primitive (product-UI quality) ────────────────────────────────── */
function Card({
  title,
  value,
  sub,
  accent = C.cyan,
  glyph = 'chart',
  status,
  w = 380,
  appear = 1,
  children,
  compact = false,
}) {
  const eApp = appear; // already eased by caller
  const op = clamp(eApp, 0, 1);
  const ty = (1 - eApp) * 26;
  const sc = 0.965 + 0.035 * eApp;
  const pad = compact ? '14px 16px' : '20px 22px 22px';
  return (
    <div
      style={{
        width: w,
        padding: pad,
        boxSizing: 'border-box',
        borderRadius: compact ? 16 : 19,
        background:
          'linear-gradient(158deg, rgba(255,255,255,0.97) 0%, rgba(243,246,252,0.97) 100%)',
        border: '1px solid rgba(40,52,92,0.12)',
        boxShadow: `0 22px 50px -26px rgba(40,52,92,0.28), inset 0 1px 0 rgba(255,255,255,0.9), 0 0 0 1px rgba(40,52,92,0.05)`,
        opacity: op,
        transform: `translateY(${ty}px) scale(${sc})`,
        transformOrigin: 'center',
        willChange: 'transform,opacity',
        position: 'relative',
        overflow: 'hidden',
      }}
    >
      <div
        style={{
          position: 'absolute',
          top: 0,
          left: '8%',
          right: '8%',
          height: 1,
          background: `linear-gradient(90deg, transparent, ${hexA(accent, 0.8)}, transparent)`,
        }}
      />
      <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
        <div
          style={{
            width: compact ? 32 : 40,
            height: compact ? 32 : 40,
            borderRadius: compact ? 9 : 12,
            flexShrink: 0,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            background: hexA(accent, 0.13),
            border: `1px solid ${hexA(accent, 0.42)}`,
            boxShadow: `inset 0 0 18px -6px ${accent}`,
          }}
        >
          <Glyph type={glyph} color={accent} size={compact ? 18 : 21} />
        </div>
        <div
          style={{
            fontFamily: INTER,
            fontSize: compact ? 13.5 : 15,
            fontWeight: 600,
            color: C.muted,
            letterSpacing: '0.005em',
            flex: 1,
          }}
        >
          {title}
        </div>
        {status && <Pill text={status[0]} accent={status[1] || accent} />}
      </div>
      <div
        style={{
          fontFamily: SORA,
          fontSize: compact ? 27 : 43,
          fontWeight: 700,
          color: C.text,
          marginTop: compact ? 10 : 15,
          letterSpacing: '-0.025em',
          lineHeight: 1,
        }}
      >
        {value}
      </div>
      {sub && (
        <div
          style={{
            fontFamily: INTER,
            fontSize: compact ? 12.5 : 13.5,
            color: C.faint,
            marginTop: 7,
            lineHeight: 1.35,
          }}
        >
          {sub}
        </div>
      )}
      {children && (
        <>
          <div
            style={{
              height: 1,
              background: 'rgba(40,52,92,0.12)',
              margin: compact ? '13px 0 12px' : '17px 0 15px',
            }}
          />
          {children}
        </>
      )}
    </div>
  );
}

/* ── Background (persistent atmosphere) ──────────────────────────────────── */
const GRAIN =
  "data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='200' height='200'><filter id='n'><feTurbulence type='fractalNoise' baseFrequency='0.85' numOctaves='2' stitchTiles='stitch'/></filter><rect width='100%25' height='100%25' filter='url(%23n)'/></svg>";

function Background() {
  const t = useTime();
  const gridOp = interpolate([1.4, 3.2, 16, 21], [0, 0.5, 0.5, 0.05], eio)(t);
  const driftA = `translate(${Math.sin(t * 0.07) * 46}px, ${Math.cos(t * 0.05) * 30}px)`;
  const driftB = `translate(${Math.cos(t * 0.06) * -40}px, ${Math.sin(t * 0.045) * 26}px)`;
  const gridShift = (t * 12) % 56;
  return (
    <div style={{ position: 'absolute', inset: 0, overflow: 'hidden' }}>
      <div
        style={{
          position: 'absolute',
          inset: 0,
          background:
            'radial-gradient(135% 100% at 50% 14%, #ffffff 0%, #f2f5fc 42%, #e8edf7 74%, #dde4f0 100%)',
        }}
      />
      {/* soft color washes */}
      <div
        style={{
          position: 'absolute',
          left: '14%',
          top: '-8%',
          width: 980,
          height: 760,
          transform: driftA,
          background: `radial-gradient(circle at 50% 50%, ${hexA(C.indigo, 0.12)} 0%, ${hexA(C.indigo, 0.04)} 40%, transparent 66%)`,
          filter: 'blur(20px)',
        }}
      />
      <div
        style={{
          position: 'absolute',
          right: '8%',
          top: '30%',
          width: 860,
          height: 720,
          transform: driftB,
          background: `radial-gradient(circle at 50% 50%, ${hexA(C.cyan, 0.1)} 0%, ${hexA(C.cyan, 0.03)} 44%, transparent 68%)`,
          filter: 'blur(22px)',
        }}
      />
      <div
        style={{
          position: 'absolute',
          left: '38%',
          bottom: '-18%',
          width: 900,
          height: 560,
          background: `radial-gradient(circle at 50% 50%, ${hexA(C.violet, 0.07)} 0%, transparent 64%)`,
          filter: 'blur(24px)',
        }}
      />
      {/* blueprint floor grid (perspective) */}
      <div
        style={{
          position: 'absolute',
          left: '-30%',
          right: '-30%',
          bottom: '-6%',
          height: '62%',
          opacity: gridOp,
          transform: 'perspective(760px) rotateX(63deg)',
          transformOrigin: '50% 100%',
          backgroundImage: `linear-gradient(${hexA(C.indigo, 0.22)} 1px, transparent 1px), linear-gradient(90deg, ${hexA(C.indigo, 0.22)} 1px, transparent 1px)`,
          backgroundSize: '56px 56px',
          backgroundPosition: `0px ${gridShift}px`,
          WebkitMaskImage:
            'radial-gradient(ellipse 52% 64% at 50% 30%, #000 0%, rgba(0,0,0,0.5) 48%, transparent 78%)',
          maskImage:
            'radial-gradient(ellipse 52% 64% at 50% 30%, #000 0%, rgba(0,0,0,0.5) 48%, transparent 78%)',
        }}
      />
      {/* dust motes */}
      <svg width="1920" height="1080" style={{ position: 'absolute', inset: 0 }}>
        {DUST.map((d, i) => {
          const y = (d.y - t * 8 * d.sp) % 1080;
          const yy = y < 0 ? y + 1080 : y;
          const x = d.x + Math.sin(t * d.sp + d.p) * 22;
          return (
            <circle
              key={i}
              cx={x}
              cy={yy}
              r={d.r}
              fill={hexA(C.indigo, 0.16)}
              opacity={0.1 + 0.07 * Math.sin(t * 1.4 + d.p)}
            />
          );
        })}
      </svg>
    </div>
  );
}

/* ── Atmosphere overlay (grain, vignette, bars) ─────────────────────────── */
function Atmosphere() {
  return (
    <div style={{ position: 'absolute', inset: 0, pointerEvents: 'none' }}>
      <div
        style={{
          position: 'absolute',
          inset: 0,
          background:
            'radial-gradient(125% 108% at 50% 42%, transparent 58%, rgba(70,86,130,0.10) 100%)',
        }}
      />
      <div
        style={{
          position: 'absolute',
          inset: 0,
          opacity: 0.025,
          mixBlendMode: 'multiply',
          backgroundImage: `url("${GRAIN}")`,
        }}
      />
      <div
        style={{
          position: 'absolute',
          top: 0,
          left: 0,
          right: 0,
          height: 90,
          background: 'linear-gradient(rgba(233,237,246,0.9), transparent)',
        }}
      />
      <div
        style={{
          position: 'absolute',
          bottom: 0,
          left: 0,
          right: 0,
          height: 120,
          background: 'linear-gradient(transparent, rgba(221,228,240,0.85))',
        }}
      />
    </div>
  );
}

/* ── Brandmark ───────────────────────────────────────────────────────────── */
function Brandmark() {
  const t = useTime();
  const op =
    (t > 4.4 ? clamp((t - 4.4) / 0.8, 0, 1) : 0) * (t > 34.2 ? clamp((35.0 - t) / 0.8, 0, 1) : 1);
  return (
    <div
      style={{
        position: 'absolute',
        top: 54,
        left: 74,
        display: 'flex',
        alignItems: 'center',
        gap: 13,
        opacity: op,
      }}
    >
      <img
        src="./corealign-mark.svg"
        alt=""
        style={{ width: 40, height: 40, filter: `drop-shadow(0 0 11px ${hexA(C.indigo, 0.7)})` }}
      />
      <span
        style={{
          fontFamily: SORA,
          fontWeight: 700,
          fontSize: 18,
          letterSpacing: '0.04em',
          color: C.text,
        }}
      >
        CoreAlign
      </span>
    </div>
  );
}

/* ── Caption (eyebrow + title + sub, strictly non-overlapping) ──────────── */
const PHASES = [
  {
    a: 3.7,
    b: 7.6,
    no: '01',
    label: 'Tasarım',
    title: 'Köşe cam cephe · kış bahçesi',
    sub: 'Köşe plan; cam cephe panelleri ve cam çatı — saha ölçüsünden modele.',
  },
  {
    a: 7.9,
    b: 12.0,
    no: '01',
    label: 'Tasarım',
    title: 'Yay (eğri) cephe · R 1820',
    sub: 'Eğri cam cephe — yarıçap, panel genişliği ve yükseklik otomatik.',
  },
  {
    a: 12.3,
    b: 16.4,
    no: '01',
    label: 'Tasarım',
    title: 'Şekilli cam: kemer · üçgen · yamuk',
    sub: 'Standart dışı cam formları — ölçü ve kesim şablonu hazır.',
  },
  {
    a: 16.9,
    b: 20.6,
    no: '02',
    label: 'Teklif',
    title: 'Anlık teklif, maliyet ve BOM',
    sub: 'Tasarımdan otomatik fiyat, kâr marjı ve malzeme listesi.',
  },
  {
    a: 21.0,
    b: 25.5,
    no: '03',
    label: 'MRP · Üretim',
    title: 'Kesim, temper, iş emri',
    sub: 'Nesting optimizasyonu, fırın planı ve kapasite — uçtan uca.',
  },
  {
    a: 25.9,
    b: 30.5,
    no: '04',
    label: 'İşletme',
    title: 'Sipariş, stok, tahsilat — tek akış',
    sub: 'B2B portal ve sanal POS ile otomatik, kesintisiz işleyiş.',
  },
  {
    a: 30.9,
    b: 34.5,
    no: '05',
    label: 'Raporlama',
    title: 'Gerçek zamanlı BI ve öngörü',
    sub: 'Canlı panolar; ciro, marj ve ileriye dönük tahmin.',
  },
];
function Caption() {
  const t = useTime();
  let cur = null,
    op = 0;
  for (const ph of PHASES) {
    if (t >= ph.a - 0.5 && t <= ph.b + 0.5) {
      const v = win(t, ph.a, ph.b, 0.5);
      if (v > op) {
        op = v;
        cur = ph;
      }
    }
  }
  if (!cur) return null;
  return (
    <div
      style={{
        position: 'absolute',
        left: 74,
        bottom: 96,
        maxWidth: 760,
        opacity: op,
        transform: `translateY(${(1 - op) * 14}px)`,
        willChange: 'transform,opacity',
      }}
    >
      <div style={{ display: 'flex', alignItems: 'center', gap: 11, marginBottom: 14 }}>
        <span
          style={{
            width: 30,
            height: 1.5,
            background: `linear-gradient(90deg, ${C.cyan}, transparent)`,
          }}
        />
        <span
          style={{
            fontFamily: SORA,
            fontWeight: 600,
            fontSize: 13,
            letterSpacing: '0.28em',
            textTransform: 'uppercase',
            color: C.cyan,
          }}
        >
          {cur.no} · {cur.label}
        </span>
      </div>
      <div
        style={{
          fontFamily: SORA,
          fontWeight: 700,
          fontSize: 44,
          lineHeight: 1.07,
          letterSpacing: '-0.02em',
          background: 'linear-gradient(180deg, #1b2236, #3a4470)',
          WebkitBackgroundClip: 'text',
          backgroundClip: 'text',
          color: 'transparent',
        }}
      >
        {cur.title}
      </div>
      <div
        style={{
          fontFamily: INTER,
          fontWeight: 400,
          fontSize: 19,
          color: C.muted,
          marginTop: 12,
          maxWidth: 560,
          lineHeight: 1.45,
        }}
      >
        {cur.sub}
      </div>
    </div>
  );
}

/* ── Scene wrapper (gate + fade + gentle push) ──────────────────────────── */
function Scene({ start, end, fade = 0.55, children }) {
  return (
    <Sprite start={start} end={end}>
      {({ localTime, duration }) => {
        const op = win(localTime, 0, duration, fade);
        const sc = 1 + 0.04 * (localTime / duration);
        return (
          <div
            style={{
              position: 'absolute',
              inset: 0,
              opacity: op,
              transform: `scale(${sc})`,
              transformOrigin: '50% 48%',
              willChange: 'transform,opacity',
            }}
          >
            {children}
          </div>
        );
      }}
    </Sprite>
  );
}
function World({ children }) {
  const t = useTime();
  return (
    <div
      style={{
        position: 'absolute',
        inset: 0,
        transform: `translate(${Math.sin(t * 0.45) * 5}px, ${Math.cos(t * 0.38) * 4}px)`,
      }}
    >
      {children}
    </div>
  );
}
function Center({ children, x = 960, y = 540, style }) {
  return (
    <div
      style={{ position: 'absolute', left: x, top: y, transform: 'translate(-50%,-50%)', ...style }}
    >
      {children}
    </div>
  );
}

/* ── Scene 1 · Brand intro (CoreAlign fades in, dissolves into CAD) ──────── */
function SceneIntro() {
  const { localTime: lt } = useSprite();
  const s = 0.95 + 0.05 * eo(clamp(lt / 1.8, 0, 1));
  const tag = eo(clamp((lt - 0.5) / 1.2, 0, 1));
  return (
    <div
      style={{
        position: 'absolute',
        inset: 0,
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
      }}
    >
      <div style={{ display: 'flex', alignItems: 'center', gap: 18, transform: `scale(${s})` }}>
        <img
          src="./corealign-mark.svg"
          alt=""
          style={{ width: 74, height: 74, filter: `drop-shadow(0 0 28px ${hexA(C.indigo, 0.6)})` }}
        />
        <div
          style={{
            fontFamily: SORA,
            fontWeight: 800,
            fontSize: 100,
            letterSpacing: '-0.03em',
            lineHeight: 1,
            background: 'linear-gradient(105deg, #2a2f6e 0%, #5b5ee8 45%, #0e9bb8 100%)',
            WebkitBackgroundClip: 'text',
            backgroundClip: 'text',
            color: 'transparent',
          }}
        >
          CoreAlign
        </div>
      </div>
      <div
        style={{
          fontFamily: INTER,
          fontSize: 24,
          color: '#586089',
          marginTop: 24,
          opacity: tag,
          transform: `translateY(${(1 - tag) * 8}px)`,
        }}
      >
        Tasarımdan muhasebeye — cam &amp; doğrama için tek platform
      </div>
    </div>
  );
}

/* ── CAD section top headline (fills the empty upper area) ───────────────── */
function TopHeadline() {
  const { localTime: lt } = useSprite();
  const op = win(lt, 0.6, 13.2, 0.8);
  return (
    <div
      style={{
        position: 'absolute',
        top: 168,
        left: '50%',
        transform: `translate(-50%, ${(1 - clamp(op, 0, 1)) * -10}px)`,
        textAlign: 'center',
        opacity: op,
        width: 1000,
        pointerEvents: 'none',
      }}
    >
      <div
        style={{
          fontFamily: SORA,
          fontWeight: 600,
          fontSize: 12.5,
          letterSpacing: '0.3em',
          textTransform: 'uppercase',
          color: C.cyan,
          marginBottom: 12,
        }}
      >
        3D CAD · CAM Motoru
      </div>
      <div
        style={{
          fontFamily: SORA,
          fontWeight: 700,
          fontSize: 34,
          lineHeight: 1.18,
          letterSpacing: '-0.02em',
          background: 'linear-gradient(180deg, #1b2236, #3a4470)',
          WebkitBackgroundClip: 'text',
          backgroundClip: 'text',
          color: 'transparent',
        }}
      >
        Her cam formu — ölçüsü, profili ve üretimiyle tek modelde.
      </div>
    </div>
  );
}

/* ── Scene 2 · CAD glass-room is GlassRoom() (defined below) ── */
function FeatureTicker() {
  const { localTime: lt } = useSprite();
  const op = win(lt, 0.6, 12.4, 0.7);
  const items = [
    ['Köşe cam cephe · çatı', 0.6, C.cyan],
    ['Profil · dikme 60 mm', 2.2, C.indigoL],
    ['Yay (eğri) cephe', 5.0, C.sky],
    ['Yarıçap · otomatik ölçü', 6.6, C.cyanL],
    ['Şekilli cam: kemer/üçgen', 9.6, C.emerald],
    ['Cam tipi · Ug 1.0', 10.8, C.violet],
  ];
  return (
    <div
      style={{
        position: 'absolute',
        top: 150,
        right: 92,
        width: 316,
        opacity: op,
        pointerEvents: 'none',
      }}
    >
      <div style={{ display: 'flex', alignItems: 'center', gap: 9, marginBottom: 15 }}>
        <span
          style={{
            width: 24,
            height: 1.5,
            background: `linear-gradient(90deg, ${C.cyan}, transparent)`,
          }}
        />
        <span
          style={{
            fontFamily: SORA,
            fontWeight: 600,
            fontSize: 11.5,
            letterSpacing: '0.26em',
            textTransform: 'uppercase',
            color: C.cyan,
          }}
        >
          CAD Motoru
        </span>
      </div>
      <div style={{ display: 'flex', flexDirection: 'column', gap: 11 }}>
        {items.map((it, i) => {
          const ap = eo(clamp((lt - it[1]) / 0.5, 0, 1));
          return (
            <div
              key={i}
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: 11,
                opacity: 0.3 + 0.7 * ap,
                transform: `translateX(${(1 - ap) * 10}px)`,
              }}
            >
              <div
                style={{
                  width: 21,
                  height: 21,
                  borderRadius: 6,
                  flexShrink: 0,
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  background: hexA(it[2], 0.04 + 0.14 * ap),
                  border: `1px solid ${hexA(it[2], 0.28 + 0.4 * ap)}`,
                }}
              >
                <svg width="11" height="11" viewBox="0 0 12 12">
                  <path
                    d="M2.4 6.4l2.3 2.3 5-5.6"
                    fill="none"
                    stroke={it[2]}
                    strokeWidth="1.8"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    opacity={ap}
                  />
                </svg>
              </div>
              <span
                style={{
                  fontFamily: INTER,
                  fontSize: 14,
                  fontWeight: 500,
                  color: ap > 0.5 ? C.text : C.faint,
                }}
              >
                {it[0]}
              </span>
            </div>
          );
        })}
      </div>
    </div>
  );
}
function SceneCAD() {
  return (
    <div style={{ position: 'absolute', inset: 0 }}>
      <CADDemos />
      <TopHeadline />
      <FeatureTicker />
    </div>
  );
}

/* ── Scene 3 · Teklif / Maliyet / BOM ───────────────────────────────────── */
function SceneTeklif() {
  const { localTime: lt } = useSprite();
  const a = (d) => eo(clamp((lt - d) / 0.7, 0, 1));
  const r = (d) => clamp((lt - d - 0.25) / 1.0, 0, 1);
  const teklif = '€ ' + trNum(Math.round(12480 * clamp((lt - 0.3) / 1.2, 0, 1)));
  return (
    <div style={{ position: 'absolute', inset: 0 }}>
      {/* design -> quote connector */}
      <svg
        width="1920"
        height="1080"
        style={{ position: 'absolute', inset: 0, pointerEvents: 'none' }}
      >
        {(() => {
          const A = [362, 320],
            B = [432, 452],
            mx = 405,
            my = 406;
          const dd = `M ${A[0]} ${A[1]} Q ${mx} ${my} ${B[0]} ${B[1]}`;
          let u = ((lt - 0.2) * 0.5) % 1;
          if (u < 0) u += 1;
          const bx = (1 - u) * (1 - u) * A[0] + 2 * (1 - u) * u * mx + u * u * B[0];
          const by = (1 - u) * (1 - u) * A[1] + 2 * (1 - u) * u * my + u * u * B[1];
          return (
            <g opacity={a(0)}>
              <path
                d={dd}
                fill="none"
                stroke={hexA(C.cyan, 0.45)}
                strokeWidth="1.5"
                strokeDasharray="5 6"
              />
              <circle
                cx={bx}
                cy={by}
                r="3.2"
                fill="#0b8fae"
                opacity={seg(lt, 0.3, 1.2)}
                style={{ filter: `drop-shadow(0 0 7px ${C.cyan})` }}
              />
            </g>
          );
        })()}
      </svg>
      {/* origin: design -> quote cue */}
      <Center x={250} y={300} style={{ opacity: a(0) }}>
        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: 10,
            padding: '10px 16px',
            borderRadius: 13,
            background: 'rgba(255,255,255,0.94)',
            border: `1px solid ${hexA(C.indigoL, 0.4)}`,
          }}
        >
          <Glyph type="cube" color={C.indigoL} size={20} />
          <span style={{ fontFamily: INTER, fontSize: 15, fontWeight: 600, color: C.text }}>
            Tasarım
          </span>
          <span style={{ fontFamily: INTER, fontSize: 13, color: C.faint }}>· 5460×2400</span>
        </div>
      </Center>
      <Center x={520} y={560}>
        <Card
          w={392}
          appear={a(0.15)}
          accent={C.cyan}
          glyph="quote"
          title="Teklif"
          value={teklif}
          sub="3 kalem · onaya hazır"
          status={['Hazır', C.cyan]}
        >
          <Sparkline accent={C.cyan} w={344} h={76} reveal={r(0.2)} seed={3} />
        </Card>
      </Center>
      <Center x={960} y={520}>
        <Card
          w={392}
          appear={a(0.4)}
          accent={C.emerald}
          glyph="cost"
          title="Maliyet"
          value="€ 7.310"
          sub="malzeme + işçilik"
          status={['Marj %41', C.emerald]}
        >
          <Bars
            accent={C.emerald}
            w={344}
            h={76}
            reveal={r(0.45)}
            data={[0.5, 0.72, 0.46, 0.62, 0.84, 0.58, 0.9, 0.66]}
          />
        </Card>
      </Center>
      <Center x={1400} y={560}>
        <Card
          w={392}
          appear={a(0.65)}
          accent={C.indigoL}
          glyph="bom"
          title="BOM · Malzeme Listesi"
          value="126 kalem"
          sub="cam · profil · donanım"
        >
          <ProgressRows accent={C.indigoL} reveal={r(0.7)} />
        </Card>
      </Center>
    </div>
  );
}

/* ── Scene 4 · MRP / production ─────────────────────────────────────────── */
const NEST_PAL = [C.cyan, C.indigo, C.violet, C.emerald, C.amber, C.sky, C.pink, C.indigoL];
function Nesting({ reveal }) {
  const cols = 6,
    rows = 4,
    gx = 5,
    W = 336,
    H = 150,
    pw = (W - gx * (cols - 1)) / cols,
    ph = (H - gx * (rows - 1)) / rows;
  return (
    <svg width={W} height={H} viewBox={`0 0 ${W} ${H}`} style={{ display: 'block' }}>
      <rect
        x="0"
        y="0"
        width={W}
        height={H}
        rx="7"
        fill="rgba(40,52,92,0.05)"
        stroke="rgba(40,52,92,0.12)"
      />
      {Array.from({ length: cols * rows }).map((_, i) => {
        if (i >= 22) return null;
        const c = i % cols,
          rr = Math.floor(i / cols),
          p = clamp(reveal * 1.15 - i * 0.04, 0, 1);
        return (
          <rect
            key={i}
            x={c * (pw + gx)}
            y={rr * (ph + gx)}
            width={pw}
            height={ph}
            rx="3"
            fill={hexA(NEST_PAL[i % NEST_PAL.length], 0.7)}
            stroke={hexA(NEST_PAL[i % NEST_PAL.length], 0.9)}
            strokeWidth="0.8"
            opacity={p}
            transform={`scale(${0.7 + 0.3 * p})`}
            style={{ transformOrigin: `${c * (pw + gx) + pw / 2}px ${rr * (ph + gx) + ph / 2}px` }}
          />
        );
      })}
    </svg>
  );
}
function Furnace({ heat, lt }) {
  const col = lerpHex(C.cold, C.hot, heat);
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
      <svg width={336} height={120} viewBox="0 0 336 120">
        <rect
          x="2"
          y="6"
          width="332"
          height="108"
          rx="10"
          fill={hexA(col, 0.16)}
          stroke={hexA(C.hot, 0.4 + 0.3 * heat)}
          strokeWidth="1.4"
        />
        {Array.from({ length: 7 }).map((_, i) => {
          const flick = 0.55 + 0.45 * Math.sin(lt * 5 + i);
          return (
            <rect
              key={i}
              x={26 + i * 44}
              y={28}
              width={22}
              height={64}
              rx={5}
              fill={lerpHex('#2a3a8a', C.hot, heat * flick)}
              opacity={0.5 + 0.5 * heat}
              style={{ filter: `drop-shadow(0 0 ${8 * heat}px ${hexA(C.hot, 0.7)})` }}
            />
          );
        })}
        <line x1="14" y1="96" x2="322" y2="96" stroke={hexA(C.hot, 0.5)} strokeWidth="1" />
      </svg>
      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          fontFamily: MONO,
          fontSize: 13,
          color: C.muted,
        }}
      >
        <span>çevrim 5/gün</span>
        <span style={{ color: heat > 0.94 ? C.emerald : lerpHex('#8fb0ff', C.amber, heat) }}>
          {heat > 0.94 ? 'hazır ✓' : 'ısınıyor…'}
        </span>
      </div>
    </div>
  );
}
function GanttBars({ reveal }) {
  const rows = [
    [0.0, 0.62, C.cyan],
    [0.12, 0.42, C.indigo],
    [0.26, 0.8, C.emerald],
    [0.4, 0.55, C.violet],
    [0.54, 0.7, C.amber],
  ];
  const W = 336;
  return (
    <svg width={W} height={132} viewBox={`0 0 ${W} 132`}>
      {rows.map((r, i) => {
        const x0 = r[0] * W * 0.5,
          full = r[1] * W * 0.78,
          p = clamp(reveal * 1.2 - i * 0.12, 0, 1);
        return (
          <g key={i}>
            <rect x="0" y={8 + i * 25} width={W} height={12} rx="6" fill="rgba(40,52,92,0.08)" />
            <rect
              x={x0}
              y={8 + i * 25}
              width={full * p}
              height={12}
              rx="6"
              fill={hexA(r[2], 0.85)}
              style={{ filter: `drop-shadow(0 0 8px ${hexA(r[2], 0.5)})` }}
            />
          </g>
        );
      })}
    </svg>
  );
}
function SceneMRP() {
  const { localTime: lt } = useSprite();
  const a = (d) => eo(clamp((lt - d) / 0.7, 0, 1));
  const r = (d) => clamp((lt - d) / 1.6, 0, 1);
  const fheat = seg(lt - 0.4, 0.5, 3.2, es),
    ftemp = Math.round(lerp(60, 700, fheat));
  return (
    <div style={{ position: 'absolute', inset: 0 }}>
      <Center x={350} y={540}>
        <Card
          w={400}
          appear={a(0.1)}
          accent={C.cyan}
          glyph="cut"
          title="Kesim · Nesting"
          value="%98,8 verim"
          sub="42 parça · 6 levha"
        >
          <Nesting reveal={r(0.4)} />
        </Card>
      </Center>
      <Center x={960} y={540}>
        <Card
          w={400}
          appear={a(0.32)}
          accent={C.amber}
          glyph="furnace"
          title="Temper Fırını"
          value={ftemp + '°C'}
          sub="ısıl işlem · 700°C hedef"
        >
          <Furnace heat={fheat} lt={lt - 0.4} />
        </Card>
      </Center>
      <Center x={1570} y={540}>
        <Card
          w={400}
          appear={a(0.54)}
          accent={C.indigoL}
          glyph="gantt"
          title="İş Emri Planı"
          value="212 emir"
          sub="kapasite kullanımı"
          status={['%86', C.indigoL]}
        >
          <GanttBars reveal={r(0.7)} />
        </Card>
      </Center>
    </div>
  );
}

/* ── Scene 5 · Business flow ─────────────────────────────────────────────── */
const HUBS = [
  { id: 'tas', t: 'Tasarım', v: 'kaynak', acc: C.indigoL, g: 'cube', x: 170, y: 560 },
  { id: 'tek', t: 'Teklif', v: '€12.480', acc: C.cyan, g: 'quote', x: 330, y: 330 },
  { id: 'sip', t: 'Sipariş', v: '212 açık', acc: C.indigo, g: 'order', x: 520, y: 560 },
  { id: 'b2b', t: 'B2B', v: '+38', acc: C.cyan, g: 'b2b', x: 700, y: 330 },
  { id: 'fat', t: 'Fatura', v: '€2,4M', acc: C.violet, g: 'invoice', x: 870, y: 560 },
  { id: 'stk', t: 'Stok', v: '%99', acc: C.emerald, g: 'box', x: 1050, y: 330 },
  { id: 'pos', t: 'Sanal POS', v: '%100', acc: C.cyan, g: 'pos', x: 1220, y: 560 },
  { id: 'car', t: 'Cariler', v: '1.240', acc: C.indigoL, g: 'user', x: 1400, y: 330 },
  { id: 'muh', t: 'Muhasebe', v: 'Dengeli', acc: C.emerald, g: 'ledger', x: 1570, y: 560 },
];
const LINKS = [
  ['tas', 'sip'],
  ['tek', 'sip'],
  ['sip', 'fat'],
  ['b2b', 'fat'],
  ['fat', 'pos'],
  ['stk', 'pos'],
  ['pos', 'muh'],
  ['car', 'muh'],
];
function bez(a, b) {
  const mx = (a.x + b.x) / 2,
    my = (a.y + b.y) / 2 - 38;
  return {
    pts: Array.from({ length: 60 }, (_, i) => {
      const t = i / 59;
      const x = (1 - t) * (1 - t) * a.x + 2 * (1 - t) * t * mx + t * t * b.x,
        y = (1 - t) * (1 - t) * a.y + 2 * (1 - t) * t * my + t * t * b.y;
      return [x, y];
    }),
    d: `M ${a.x} ${a.y} Q ${mx} ${my} ${b.x} ${b.y}`,
  };
}
function FlowHub({ h, appear }) {
  return (
    <div
      style={{
        position: 'absolute',
        left: h.x,
        top: h.y,
        transform: `translate(-50%,-50%) scale(${0.9 + 0.1 * appear})`,
        opacity: appear,
        willChange: 'transform,opacity',
      }}
    >
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          gap: 11,
          padding: '12px 16px',
          borderRadius: 15,
          minWidth: 152,
          background: 'linear-gradient(155deg, rgba(255,255,255,0.98), rgba(242,245,251,0.98))',
          border: `1px solid ${hexA(h.acc, 0.34)}`,
          boxShadow: `0 16px 36px -18px rgba(40,52,92,0.30), inset 0 1px 0 rgba(255,255,255,0.9), 0 0 0 1px rgba(40,52,92,0.04)`,
        }}
      >
        <div
          style={{
            width: 34,
            height: 34,
            borderRadius: 10,
            flexShrink: 0,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            background: hexA(h.acc, 0.14),
            border: `1px solid ${hexA(h.acc, 0.4)}`,
          }}
        >
          <Glyph type={h.g} color={h.acc} size={19} />
        </div>
        <div>
          <div
            style={{
              fontFamily: INTER,
              fontSize: 14.5,
              fontWeight: 600,
              color: C.text,
              lineHeight: 1.1,
            }}
          >
            {h.t}
          </div>
          <div style={{ fontFamily: MONO, fontSize: 12, color: hexA(h.acc, 0.95), marginTop: 2 }}>
            {h.v}
          </div>
        </div>
      </div>
    </div>
  );
}
function SceneFlow() {
  const { localTime: lt } = useSprite();
  const byId = {};
  HUBS.forEach((h) => (byId[h.id] = h));
  const curves = React.useMemo(() => LINKS.map(([a, b]) => bez(byId[a], byId[b])), []);
  const lineP = seg(lt, 0.4, 2.2, eo);
  const appearOf = (i) => eo(clamp((lt - 0.2 - i * 0.12) / 0.7, 0, 1));
  // pulses
  const pulses = [];
  curves.forEach((c, ci) => {
    for (let k = 0; k < 2; k++) {
      const u = (lt * 0.26 + ci * 0.13 + k * 0.5) % 1;
      const f = u * (c.pts.length - 1),
        i0 = Math.floor(f),
        p0 = c.pts[i0],
        p1 = c.pts[Math.min(i0 + 1, c.pts.length - 1)];
      pulses.push({
        x: lerp(p0[0], p1[0], f - i0),
        y: lerp(p0[1], p1[1], f - i0),
        o: Math.sin(u * Math.PI),
      });
    }
  });
  return (
    <div style={{ position: 'absolute', inset: 0 }}>
      <svg width="1920" height="1080" style={{ position: 'absolute', inset: 0 }}>
        {curves.map((c, i) => (
          <path
            key={i}
            d={c.d}
            fill="none"
            stroke={hexA(C.indigoL, 0.3)}
            strokeWidth="1.6"
            strokeDasharray="1200"
            strokeDashoffset={1200 * (1 - lineP)}
          />
        ))}
        {pulses.map((p, i) => (
          <circle
            key={i}
            cx={p.x}
            cy={p.y}
            r={3.4}
            fill="#0b8fae"
            opacity={p.o * lineP}
            style={{ filter: `drop-shadow(0 0 7px ${C.cyan})` }}
          />
        ))}
      </svg>
      {HUBS.map((h, i) => (
        <FlowHub key={h.id} h={h} appear={appearOf(i)} />
      ))}
    </div>
  );
}

/* ── Scene 6 · BI ────────────────────────────────────────────────────────── */
function BIChart({ reveal }) {
  const W = 820,
    H = 300,
    padL = 20,
    padB = 34,
    padT = 20;
  const main = React.useMemo(() => {
    const r = mulberry32(77);
    const n = 11,
      arr = [];
    let v = 0.32;
    for (let i = 0; i <= n; i++) {
      v = clamp(v + (r() - 0.34) * 0.12 + 0.03, 0.12, 0.92);
      arr.push([padL + (i / n) * (W * 0.62 - padL), H - padB - v * (H - padB - padT)]);
    }
    return arr;
  }, []);
  const fore = React.useMemo(() => {
    const last = main[main.length - 1];
    const arr = [last.slice()];
    const n = 5;
    for (let i = 1; i <= n; i++) {
      arr.push([last[0] + (i / n) * (W - 40 - last[0]), last[1] - (i / n) * 86]);
    }
    return arr;
  }, [main]);
  const dMain = smoothPath(main),
    dFore = smoothPath(fore);
  const area = dMain + ` L ${main[main.length - 1][0]} ${H - padB} L ${padL} ${H - padB} Z`;
  const months = ['O', 'Ş', 'M', 'N', 'M', 'H', 'T', 'A', 'E', 'E', 'K', 'A'];
  const drawM = clamp(reveal / 0.7, 0, 1),
    drawF = clamp((reveal - 0.6) / 0.4, 0, 1);
  return (
    <svg width={W} height={H} viewBox={`0 0 ${W} ${H}`} style={{ display: 'block' }}>
      <defs>
        <linearGradient id="biA" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0" stopColor={C.cyan} stopOpacity="0.3" />
          <stop offset="1" stopColor={C.cyan} stopOpacity="0" />
        </linearGradient>
      </defs>
      {[0, 0.25, 0.5, 0.75, 1].map((g, i) => (
        <line
          key={i}
          x1={padL}
          y1={padT + g * (H - padB - padT)}
          x2={W - 20}
          y2={padT + g * (H - padB - padT)}
          stroke="rgba(40,52,92,0.09)"
          strokeWidth="1"
        />
      ))}
      {months.map((m, i) => (
        <text
          key={i}
          x={padL + (i / 11) * (W * 0.62 - padL)}
          y={H - 12}
          fill={C.faint}
          textAnchor="middle"
          style={{ fontFamily: MONO, fontSize: 11 }}
        >
          {m}
        </text>
      ))}
      <path d={area} fill="url(#biA)" opacity={drawM} />
      <path
        d={dMain}
        fill="none"
        stroke={C.cyan}
        strokeWidth="3"
        strokeLinecap="round"
        strokeDasharray="1400"
        strokeDashoffset={1400 * (1 - drawM)}
        style={{ filter: `drop-shadow(0 0 7px ${hexA(C.cyan, 0.6)})` }}
      />
      <path
        d={dFore}
        fill="none"
        stroke={C.amber}
        strokeWidth="2.6"
        strokeLinecap="round"
        strokeDasharray="10 8"
        opacity={drawF}
        style={{ filter: `drop-shadow(0 0 7px ${hexA(C.amber, 0.5)})` }}
      />
      {drawF > 0.5 && (
        <g opacity={drawF}>
          <circle
            cx={fore[fore.length - 1][0]}
            cy={fore[fore.length - 1][1]}
            r="4.5"
            fill="#fff"
            stroke={C.amber}
            strokeWidth="2"
          />
          <rect
            x={fore[fore.length - 1][0] - 58}
            y={fore[fore.length - 1][1] - 40}
            width={116}
            height={24}
            rx={6}
            fill="rgba(9,12,24,0.9)"
            stroke={hexA(C.amber, 0.4)}
          />
          <text
            x={fore[fore.length - 1][0]}
            y={fore[fore.length - 1][1] - 23}
            fill={C.amber}
            textAnchor="middle"
            style={{ fontFamily: MONO, fontSize: 13 }}
          >
            tahmin ▲
          </text>
        </g>
      )}
    </svg>
  );
}
function SceneBI() {
  const { localTime: lt } = useSprite();
  const a = (d) => eo(clamp((lt - d) / 0.7, 0, 1));
  const ciro = '€ ' + (clamp((lt - 0.4) / 1.6, 0, 1) * 4.2).toFixed(1).replace('.', ',') + 'M';
  return (
    <div style={{ position: 'absolute', inset: 0 }}>
      <Center x={730} y={540}>
        <Card
          w={900}
          appear={a(0.05)}
          accent={C.cyan}
          glyph="chart"
          title="Canlı Pano · BI"
          value={ciro}
          sub="ciro · son 12 ay + öngörü"
          status={['Canlı', C.emerald]}
        >
          <BIChart reveal={clamp((lt - 0.6) / 2.4, 0, 1)} />
        </Card>
      </Center>
      <Center x={1500} y={400}>
        <Card
          w={356}
          appear={a(0.45)}
          accent={C.emerald}
          glyph="cost"
          title="Brüt Marj"
          value="%38"
          sub="▲ 4 puan / yıl"
          compact
        />
      </Center>
      <Center x={1500} y={560}>
        <Card
          w={356}
          appear={a(0.62)}
          accent={C.violet}
          glyph="bars"
          title="Büyüme"
          value="▲ %24"
          sub="yıllık ciro artışı"
          compact
        />
      </Center>
      <Center x={1500} y={720}>
        <Card
          w={356}
          appear={a(0.79)}
          accent={C.cyan}
          glyph="invoice"
          title="Nakit Akışı"
          value="€ 1,2M"
          sub="30 günlük projeksiyon"
          compact
        />
      </Center>
    </div>
  );
}

/* ── Scene 7 · Brand resolve ─────────────────────────────────────────────── */
function SceneResolve() {
  const { localTime: lt } = useSprite();
  const a = (d, dur = 0.9) => eo(clamp((lt - d) / dur, 0, 1));
  const chips = [
    'Tasarım',
    'Teklif',
    'MRP',
    'Sipariş',
    'Stok',
    'B2B',
    'Sanal POS',
    'Muhasebe',
    'Rapor',
  ];
  return (
    <div
      style={{
        position: 'absolute',
        inset: 0,
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
      }}
    >
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          gap: 18,
          opacity: a(0.0),
          transform: `translateY(${(1 - a(0.0)) * 20}px)`,
        }}
      >
        <img
          src="./corealign-mark.svg"
          alt=""
          style={{ width: 77, height: 77, filter: `drop-shadow(0 0 30px ${hexA(C.indigo, 0.6)})` }}
        />
        <div
          style={{
            fontFamily: SORA,
            fontWeight: 800,
            fontSize: 104,
            letterSpacing: '-0.03em',
            lineHeight: 1,
            background: 'linear-gradient(105deg, #2a2f6e 0%, #5b5ee8 45%, #0e9bb8 100%)',
            WebkitBackgroundClip: 'text',
            backgroundClip: 'text',
            color: 'transparent',
          }}
        >
          CoreAlign
        </div>
      </div>
      <div
        style={{
          fontFamily: INTER,
          fontSize: 25,
          color: '#586089',
          marginTop: 26,
          opacity: a(0.45),
          textAlign: 'center',
        }}
      >
        Tasarımdan muhasebeye — cam &amp; doğrama için tek platform
      </div>
      <div
        style={{
          display: 'flex',
          flexWrap: 'wrap',
          gap: 10,
          justifyContent: 'center',
          marginTop: 34,
          maxWidth: 920,
          opacity: a(0.75),
        }}
      >
        {chips.map((c, i) => (
          <React.Fragment key={i}>
            <span
              style={{
                fontFamily: INTER,
                fontSize: 14.5,
                fontWeight: 600,
                color: '#4a5488',
                padding: '9px 16px',
                borderRadius: 999,
                border: '1px solid rgba(129,140,248,0.34)',
                background: 'rgba(99,102,241,0.07)',
              }}
            >
              {c}
            </span>
            {i < chips.length - 1 && (
              <span style={{ color: C.cyan, alignSelf: 'center', opacity: 0.6 }}>›</span>
            )}
          </React.Fragment>
        ))}
      </div>
      <div style={{ display: 'flex', gap: 16, marginTop: 42, opacity: a(1.05) }}>
        <div
          style={{
            fontFamily: SORA,
            fontWeight: 600,
            fontSize: 18,
            color: '#fff',
            padding: '16px 34px',
            borderRadius: 15,
            background: `linear-gradient(135deg, ${C.indigo}, ${C.indigoD})`,
            boxShadow: `0 14px 40px ${hexA(C.indigoD, 0.55)}`,
          }}
        >
          Ücretsiz deneyin
        </div>
        <div
          style={{
            fontFamily: SORA,
            fontWeight: 600,
            fontSize: 18,
            color: '#3a4470',
            padding: '16px 34px',
            borderRadius: 15,
            border: '1px solid rgba(91,94,232,0.4)',
            background: 'rgba(91,94,232,0.05)',
          }}
        >
          Demo planlayın
        </div>
      </div>
    </div>
  );
}

/* ── Root ────────────────────────────────────────────────────────────────── */
function CoreAlignHeroLight() {
  const reduce =
    typeof window !== 'undefined' &&
    window.matchMedia &&
    window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  return (
    <Stage
      width={1920}
      height={1080}
      duration={38.4}
      background="#e9edf6"
      persistKey="corealign-hero-light"
      autoplay={!reduce}
      loop={true}
    >
      <Background />
      <World>
        <Scene start={0} end={4.0} fade={0.95}>
          <SceneIntro />
        </Scene>
        <Scene start={3.2} end={16.8}>
          {' '}
          <SceneCAD />
        </Scene>
        <Scene start={16.7} end={20.9}>
          {' '}
          <SceneTeklif />
        </Scene>
        <Scene start={20.8} end={25.8}>
          {' '}
          <SceneMRP />
        </Scene>
        <Scene start={25.7} end={30.8}>
          {' '}
          <SceneFlow />
        </Scene>
        <Scene start={30.7} end={34.8}>
          {' '}
          <SceneBI />
        </Scene>
        <Scene start={34.6} end={38.4}>
          {' '}
          <SceneResolve />
        </Scene>
      </World>
      <Atmosphere />

      <Caption />
    </Stage>
  );
}
/* ── CAD demo shared helpers ─────────────────────────────────────────────── */
function CADdefs() {
  return (
    <defs>
      <linearGradient id="cadGlass" x1="0" y1="0" x2="0.25" y2="1">
        <stop offset="0" stopColor="#a6ccf4" stopOpacity="0.34" />
        <stop offset="0.5" stopColor="#7aa6e0" stopOpacity="0.12" />
        <stop offset="1" stopColor="#92bdf2" stopOpacity="0.22" />
      </linearGradient>
      <radialGradient id="cadFloor" cx="0.45" cy="0.4" r="0.7">
        <stop offset="0" stopColor={hexA(C.indigo, 0.12)} />
        <stop offset="1" stopColor={hexA(C.indigo, 0)} />
      </radialGradient>
      <linearGradient id="cadAluV" x1="0" y1="0" x2="1" y2="0">
        <stop offset="0" stopColor="#9aa4ba" />
        <stop offset="0.45" stopColor="#e7ecf4" />
        <stop offset="0.6" stopColor="#f3f6fb" />
        <stop offset="1" stopColor="#9099b0" />
      </linearGradient>
      <linearGradient id="cadAluH" x1="0" y1="0" x2="1" y2="0.3">
        <stop offset="0" stopColor="#aab3c6" />
        <stop offset="0.5" stopColor="#eef2f8" />
        <stop offset="1" stopColor="#959eb4" />
      </linearGradient>
    </defs>
  );
}
function dimSeg(A, B, txt, col, op) {
  const mx = (A[0] + B[0]) / 2,
    my = (A[1] + B[1]) / 2,
    tw = txt.length * 8.4 + 22;
  return (
    <g opacity={op}>
      <line x1={A[0]} y1={A[1]} x2={B[0]} y2={B[1]} stroke={hexA(col, 0.7)} strokeWidth="1.3" />
      <circle cx={A[0]} cy={A[1]} r="2.6" fill={col} />
      <circle cx={B[0]} cy={B[1]} r="2.6" fill={col} />
      <rect
        x={mx - tw / 2}
        y={my - 13}
        width={tw}
        height="26"
        rx="6"
        fill="rgba(255,255,255,0.95)"
        stroke={hexA(col, 0.4)}
      />
      <text
        x={mx}
        y={my + 4}
        fill="#27406e"
        textAnchor="middle"
        style={{ fontFamily: MONO, fontSize: 13, fontWeight: 500 }}
      >
        {txt}
      </text>
    </g>
  );
}
function leadTag(tx, ty, cx, cy, txt, col, op) {
  const tw = txt.length * 8.1 + 22;
  return (
    <g opacity={op}>
      <line x1={cx} y1={cy} x2={tx} y2={ty} stroke={hexA(col, 0.55)} strokeWidth="1.1" />
      <circle cx={tx} cy={ty} r="3" fill={col} />
      <rect
        x={cx - tw / 2}
        y={cy - 14}
        width={tw}
        height="28"
        rx="7"
        fill="rgba(255,255,255,0.96)"
        stroke={hexA(col, 0.45)}
      />
      <text
        x={cx}
        y={cy + 5}
        fill={hexA(col, 0.98)}
        textAnchor="middle"
        style={{ fontFamily: MONO, fontSize: 13, fontWeight: 500 }}
      >
        {txt}
      </text>
    </g>
  );
}

/* ── Demo A · L glass facade (flat panels, zoomed, detailed dims) ─────────── */
function DemoLSpace() {
  const { localTime: lt } = useSprite();
  const op = win(lt, 0, 4.7, 0.5);
  if (op <= 0.002) return null;
  const dl = lt;
  const W = 470,
    D = 300,
    Wn = 270,
    Dn = 150,
    H = 224,
    S = 1.26,
    OX = 214,
    OY = 516;
  const pj = (x, y, z) => {
    const px = x * 0.94 + z * 0.62,
      py = x * 0.3 - z * 0.46 - y;
    return [OX + S * px, OY + S * py];
  };
  const PT = (x, y, z) => {
    const p = pj(x, y, z);
    return p[0].toFixed(1) + ',' + p[1].toFixed(1);
  };
  const sub = (a, b, n) =>
    Array.from({ length: n + 1 }, (_, i) => [lerp(a[0], b[0], i / n), lerp(a[1], b[1], i / n)]);
  const planPts = [
    [0, 0],
    [W, 0],
    [W, Dn],
    [Wn, Dn],
    [Wn, D],
    [0, D],
  ];
  const floorD = 'M ' + planPts.map((p) => PT(p[0], 0, p[1])).join(' L ') + ' Z';
  const roofD = 'M ' + planPts.map((p) => PT(p[0], H, p[1])).join(' L ') + ' Z';
  const walls = [
    { pts: sub([Wn, D], [0, D], 3), t0: 1.7, sh: 0.42 },
    { pts: sub([Wn, Dn], [Wn, D], 2), t0: 2.0, sh: 0.54 },
    { pts: sub([W, Dn], [Wn, Dn], 2), t0: 2.2, sh: 0.6 },
    { pts: sub([W, 0], [W, Dn], 2), t0: 1.3, sh: 0.72 },
    { pts: sub([0, D], [0, 0], 3), t0: 1.0, sh: 0.85 },
    { pts: sub([0, 0], [W, 0], 4), t0: 1.7, sh: 1.0, front: true },
  ];
  const floorP = seg(dl, 0.15, 1.4),
    cornerP = seg(dl, 0.5, 1.7, eo),
    roofP = seg(dl, 3.5, 4.6, eo),
    dimP = seg(dl, 2.7, 3.9);
  const p0 = pj(0, 0, 0),
    pW = pj(W, 0, 0),
    pD = pj(0, 0, D),
    pH = pj(0, H, 0),
    pf = pj(W / 4, 0, 0),
    gl = pj(W * 0.6, H * 0.5, 0),
    dk = pj(Wn, H * 0.42, Dn);
  return (
    <div style={{ position: 'absolute', inset: 0, opacity: op }}>
      <svg
        width="1100"
        height="720"
        viewBox="0 0 1100 720"
        style={{
          position: 'absolute',
          left: '50%',
          top: '49%',
          transform: 'translate(-50%,-50%)',
          overflow: 'visible',
        }}
      >
        <CADdefs />
        <path d={floorD} fill="url(#cadFloor)" opacity={0.85 * floorP} />
        <path
          d={floorD}
          fill="none"
          stroke={hexA(C.cyan, 0.55)}
          strokeWidth="1.6"
          strokeDasharray="1700"
          strokeDashoffset={1700 * (1 - floorP)}
        />
        {planPts.map((c, i) => {
          const a = pj(c[0], 0, c[1]),
            b = pj(c[0], H * cornerP, c[1]);
          return (
            <line
              key={i}
              x1={a[0]}
              y1={a[1]}
              x2={b[0]}
              y2={b[1]}
              stroke="url(#cadAluV)"
              strokeWidth="7"
            />
          );
        })}
        {walls.map((w, wi) => {
          const wp = seg(dl, w.t0, w.t0 + 1.7, eo);
          if (wp <= 0.002) return null;
          const h = H * wp;
          return (
            <g key={wi}>
              {w.pts.slice(0, -1).map((p, i) => {
                const q = w.pts[i + 1],
                  poly = `${PT(p[0], 0, p[1])} ${PT(q[0], 0, q[1])} ${PT(q[0], h, q[1])} ${PT(p[0], h, p[1])}`;
                return (
                  <g key={i}>
                    <polygon points={poly} fill="url(#cadGlass)" />
                    <polygon points={poly} fill="#33416b" opacity={(1 - w.sh) * 0.42} />
                    <polygon
                      points={poly}
                      fill="none"
                      stroke={hexA(C.cyanL, 0.5)}
                      strokeWidth="1"
                    />
                  </g>
                );
              })}
              {w.pts.map((p, i) => {
                const a = pj(p[0], 0, p[1]),
                  b = pj(p[0], h, p[1]);
                return (
                  <line
                    key={'m' + i}
                    x1={a[0]}
                    y1={a[1]}
                    x2={b[0]}
                    y2={b[1]}
                    stroke="url(#cadAluV)"
                    strokeWidth={w.front ? 4 : 5}
                  />
                );
              })}
              <polyline
                points={w.pts.map((p) => PT(p[0], h, p[1])).join(' ')}
                fill="none"
                stroke="url(#cadAluH)"
                strokeWidth="6"
                opacity={wp}
              />
              {w.front &&
                (() => {
                  const kp = seg(dl, w.t0 + 0.9, w.t0 + 1.9);
                  if (kp <= 0.01) return null;
                  return (
                    <g opacity={kp}>
                      {w.pts.slice(0, -1).map((p, i) => {
                        const px = p[0],
                          qx = w.pts[i + 1][0],
                          ix = 13,
                          iyB = 22,
                          iyT = 24,
                          top = h - iyT;
                        if (top <= iyB + 24) return null;
                        const midY = (iyB + top) / 2;
                        const A = pj(px + ix, iyB, 0),
                          B = pj(qx - ix, iyB, 0),
                          Ct = pj(qx - ix, top, 0),
                          Dt = pj(px + ix, top, 0);
                        const ml = pj(px + ix, midY, 0),
                          mr = pj(qx - ix, midY, 0);
                        const handle = i === 1 || i === 2,
                          hx = qx - ix - 11,
                          hk1 = pj(hx, midY - 26, 0),
                          hk2 = pj(hx, midY + 26, 0);
                        return (
                          <g key={'k' + i}>
                            <polygon
                              points={`${A[0]},${A[1]} ${B[0]},${B[1]} ${Ct[0]},${Ct[1]} ${Dt[0]},${Dt[1]}`}
                              fill="none"
                              stroke="url(#cadAluH)"
                              strokeWidth="3.5"
                            />
                            <line
                              x1={ml[0]}
                              y1={ml[1]}
                              x2={mr[0]}
                              y2={mr[1]}
                              stroke="url(#cadAluH)"
                              strokeWidth="3"
                            />
                            {handle && (
                              <line
                                x1={hk1[0]}
                                y1={hk1[1]}
                                x2={hk2[0]}
                                y2={hk2[1]}
                                stroke="url(#cadAluV)"
                                strokeWidth="4"
                                strokeLinecap="round"
                              />
                            )}
                          </g>
                        );
                      })}
                    </g>
                  );
                })()}
            </g>
          );
        })}
        <g opacity={roofP} transform={`translate(0 ${(-34 * (1 - roofP)).toFixed(1)})`}>
          <path d={roofD} fill="url(#cadGlass)" opacity="0.72" />
          <path d={roofD} fill="none" stroke="url(#cadAluH)" strokeWidth="6" />
        </g>
        {dimSeg([p0[0], p0[1] + 40], [pW[0], pW[1] + 40], '5460 mm', C.cyan, dimP)}
        {dimSeg([p0[0] - 32, p0[1] + 16], [pD[0] - 32, pD[1] + 16], '3600 mm', C.cyan, dimP)}
        {dimSeg([p0[0] - 46, p0[1]], [pH[0] - 46, pH[1]], '2400 mm', C.cyan, dimP)}
        {dimSeg([p0[0], p0[1] + 18], [pf[0], pf[1] + 18], '1365 mm', C.cyanL, seg(dl, 3.1, 4.1))}
        {leadTag(
          gl[0],
          gl[1],
          gl[0] + 18,
          gl[1] - 76,
          '10 mm temperli',
          C.violet,
          seg(dl, 3.3, 4.3),
        )}
        {leadTag(dk[0], dk[1], dk[0] + 96, dk[1] - 30, 'dikme 60 mm', C.indigoL, seg(dl, 3.5, 4.5))}
      </svg>
    </div>
  );
}

/* ── Demo B · Curved (yay) glass facade ──────────────────────────────────── */
function DemoArc() {
  const { localTime: lt } = useSprite();
  const op = win(lt, 4.6, 9.1, 0.5);
  if (op <= 0.002) return null;
  const dl = lt - 4.6;
  const NSEG = 44,
    chord = 640,
    bulge = 150,
    H = 250,
    S = 1.06,
    OX = 250,
    OY = 405;
  const zc = ((chord / 2) * (chord / 2) - bulge * bulge) / (2 * bulge),
    R = zc + bulge,
    cx = chord / 2,
    thm = Math.asin(chord / 2 / R);
  const arcPt = (t) => {
    const th = lerp(-thm, thm, t);
    return [cx + R * Math.sin(th), zc - R * Math.cos(th)];
  };
  const pts = Array.from({ length: NSEG + 1 }, (_, k) => arcPt(k / NSEG));
  const pj = (x, y, z) => {
    const px = x * 0.94 + z * 0.62,
      py = x * 0.3 - z * 0.46 - y;
    return [OX + S * px, OY + S * py];
  };
  const PT = (x, y, z) => {
    const p = pj(x, y, z);
    return p[0].toFixed(1) + ',' + p[1].toFixed(1);
  };
  const rise = seg(dl, 0.4, 1.9, eo),
    h = H * rise,
    dimP = seg(dl, 2.0, 3.2);
  const mulI = [0, 1, 2, 3, 4, 5, 6].map((i) => Math.round((i / 6) * NSEG));
  const topPath = 'M ' + pts.map((p) => PT(p[0], h, p[1])).join(' L ');
  const botPath = 'M ' + pts.map((p) => PT(p[0], 0, p[1])).join(' L ');
  const bandD =
    'M ' +
    pts.map((p) => PT(p[0], 0, p[1])).join(' L ') +
    ' L ' +
    pts
      .slice()
      .reverse()
      .map((p) => PT(p[0], h, p[1]))
      .join(' L ') +
    ' Z';
  const cen = pj(cx, 0, zc),
    mid = pj(cx, 0, -bulge),
    midTop = pj(cx, H * 0.55, arcPt(0.5)[1]);
  const pH0 = pj(pts[0][0], 0, pts[0][1]),
    pHt = pj(pts[0][0], H, pts[0][1]);
  const fa = pj(pts[mulI[0]][0], 0, pts[mulI[0]][1]),
    fb = pj(pts[mulI[1]][0], 0, pts[mulI[1]][1]);
  return (
    <div style={{ position: 'absolute', inset: 0, opacity: op }}>
      <svg
        width="1100"
        height="720"
        viewBox="0 0 1100 720"
        style={{
          position: 'absolute',
          left: '50%',
          top: '49%',
          transform: 'translate(-50%,-50%)',
          overflow: 'visible',
        }}
      >
        <CADdefs />
        {/* smooth curved glass band */}
        <path d={botPath} fill="none" stroke={hexA(C.cyan, 0.5)} strokeWidth="1.6" opacity={rise} />
        <path d={bandD} fill="url(#cadGlass)" opacity={rise} />
        <path
          d={topPath}
          fill="none"
          stroke={hexA(C.cyanL, 0.55)}
          strokeWidth="1.4"
          opacity={rise}
        />
        <path d={topPath} fill="none" stroke="url(#cadAluH)" strokeWidth="6" opacity={rise} />
        <path d={botPath} fill="none" stroke="url(#cadAluH)" strokeWidth="5" opacity={rise} />
        {mulI.map((idx, i) => {
          const p = pts[idx],
            a = pj(p[0], 0, p[1]),
            b = pj(p[0], h, p[1]);
          return (
            <line
              key={i}
              x1={a[0]}
              y1={a[1]}
              x2={b[0]}
              y2={b[1]}
              stroke="url(#cadAluV)"
              strokeWidth="5"
              opacity={rise}
            />
          );
        })}
        {/* radius dimension */}
        <g opacity={dimP}>
          <line
            x1={cen[0]}
            y1={cen[1]}
            x2={mid[0]}
            y2={mid[1]}
            stroke={hexA(C.amber, 0.6)}
            strokeWidth="1.2"
            strokeDasharray="6 5"
          />
          <path
            d={`M${cen[0] - 7} ${cen[1]} h14 M${cen[0]} ${cen[1] - 7} v14`}
            stroke={hexA(C.amber, 0.8)}
            strokeWidth="1.4"
          />
          <rect
            x={(cen[0] + mid[0]) / 2 - 54}
            y={(cen[1] + mid[1]) / 2 - 13}
            width="108"
            height="26"
            rx="6"
            fill="rgba(255,255,255,0.96)"
            stroke={hexA(C.amber, 0.45)}
          />
          <text
            x={(cen[0] + mid[0]) / 2}
            y={(cen[1] + mid[1]) / 2 + 4}
            fill="#9a5a12"
            textAnchor="middle"
            style={{ fontFamily: MONO, fontSize: 13, fontWeight: 500 }}
          >
            R 1820 mm
          </text>
        </g>
        {dimSeg([pH0[0] - 44, pH0[1]], [pHt[0] - 44, pHt[1]], '2400 mm', C.cyan, dimP)}
        {dimSeg([fa[0], fa[1] + 34], [fb[0], fb[1] + 34], '780 mm', C.cyanL, seg(dl, 2.2, 3.2))}
        {leadTag(
          midTop[0],
          midTop[1],
          midTop[0],
          midTop[1] - 92,
          '8 + 8 lamine',
          C.violet,
          seg(dl, 2.4, 3.4),
        )}
      </svg>
    </div>
  );
}

/* ── Demo C · Shaped glass (kemer / üçgen / yamuk) ───────────────────────── */
function DemoShaped() {
  const { localTime: lt } = useSprite();
  const op = win(lt, 9.0, 13.6, 0.5);
  if (op <= 0.002) return null;
  const dl = lt - 9.0;
  const S = 1.0,
    OX = 196,
    OY = 486;
  const pj = (x, y, z) => {
    const px = x * 0.94 + z * 0.62,
      py = x * 0.3 - z * 0.46 - y;
    return [OX + S * px, OY + S * py];
  };
  const PT = (x, y) => {
    const p = pj(x, y, 0);
    return p[0].toFixed(1) + ',' + p[1].toFixed(1);
  };
  // arched-top (kemer)
  const wa = 196,
    hs = 190,
    ra = wa / 2;
  const arch = [];
  for (let k = 0; k <= 36; k++) {
    const th = lerp(0, Math.PI, k / 36);
    arch.push([wa / 2 + ra * Math.cos(th), hs + ra * Math.sin(th)]);
  }
  const kemer = [[0, 0], [wa, 0], [wa, hs], ...arch, [0, hs]];
  const ucgen = [
    [0, 0],
    [188, 0],
    [94, 300],
  ];
  const yamuk = [
    [0, 0],
    [182, 0],
    [182, 300],
    [0, 212],
  ];
  const shapes = [
    { pts: kemer, xb: 18, label: 'Kemer', t0: 0.4 },
    { pts: ucgen, xb: 300, label: 'Üçgen / alınlık', t0: 1.3 },
    { pts: yamuk, xb: 560, label: 'Yamuk', t0: 2.2 },
  ];
  const dimP = seg(dl, 2.6, 3.8);
  // arched dims anchors
  const ka = pj(18, 0, 0),
    kb = pj(18 + wa, 0, 0),
    kt = pj(18, hs + ra, 0),
    apex = pj(18 + wa / 2, hs + ra, 0);
  return (
    <div style={{ position: 'absolute', inset: 0, opacity: op }}>
      <svg
        width="1100"
        height="720"
        viewBox="0 0 1100 720"
        style={{
          position: 'absolute',
          left: '50%',
          top: '50%',
          transform: 'translate(-50%,-50%)',
          overflow: 'visible',
        }}
      >
        <CADdefs />
        {shapes.map((s, si) => {
          const sp = seg(dl, s.t0, s.t0 + 1.0, eo);
          if (sp <= 0.002) return null;
          const poly = s.pts.map((p) => PT(p[0] + s.xb, p[1])).join(' ');
          const baseL = pj(s.xb, 0, 0),
            baseR = pj(s.xb + s.pts[1][0], 0, 0);
          const lbl = pj(s.xb + s.pts[1][0] / 2, 0, 0);
          return (
            <g key={si} opacity={sp} transform={`translate(0 ${(1 - sp) * 24})`}>
              <line
                x1={baseL[0] - 6}
                y1={baseL[1] + 6}
                x2={baseR[0] + 6}
                y2={baseR[1] + 6}
                stroke={hexA(C.cyan, 0.3)}
                strokeWidth="6"
                strokeLinecap="round"
                opacity="0.5"
              />
              <polygon points={poly} fill="url(#cadGlass)" />
              <polygon points={poly} fill="none" stroke={hexA(C.cyanL, 0.65)} strokeWidth="1.4" />
              <polygon
                points={poly}
                fill="none"
                stroke="url(#cadAluH)"
                strokeWidth="4"
                opacity="0.5"
              />
              <text
                x={lbl[0]}
                y={lbl[1] + 66}
                fill={C.muted}
                textAnchor="middle"
                style={{ fontFamily: INTER, fontSize: 16, fontWeight: 600 }}
              >
                {s.label}
              </text>
            </g>
          );
        })}
        {dimSeg([ka[0], ka[1] + 34], [kb[0], kb[1] + 34], '1000 mm', C.cyan, dimP)}
        {dimSeg([ka[0] - 40, ka[1]], [kt[0] - 40, kt[1]], '2400 mm', C.cyan, dimP)}
        {leadTag(
          apex[0],
          apex[1],
          apex[0] + 8,
          apex[1] - 44,
          'kemer R 500',
          C.amber,
          seg(dl, 3.0, 4.0),
        )}
      </svg>
    </div>
  );
}
function floorClamp(v) {
  return clamp(v, 0, 1);
}
function CADDemos() {
  return (
    <>
      <DemoLSpace />
      <DemoArc />
      <DemoShaped />
    </>
  );
}

window.CoreAlignHeroLight = CoreAlignHeroLight;
