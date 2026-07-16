/* AUTO-GENERATED from corealign-hero.jsx (Babel 7.26.4 presets react,typescript). Edit the .jsx then re-run the transpile. */
// CoreAlign — cinematic landing hero (2.5D motion design)
// Reads the timeline engine globals set by animations.jsx (loaded first via x-import).
const { Stage, Sprite, useTime, useTimeline, useSprite, Easing, interpolate, animate, clamp } =
  window;

/* ── Design tokens ───────────────────────────────────────────────────────── */
const C = {
  indigo: '#6366f1',
  indigoL: '#818cf8',
  indigoD: '#4f46e5',
  cyan: '#22d3ee',
  cyanL: '#67e8f9',
  emerald: '#34d399',
  amber: '#f5b14c',
  violet: '#a78bfa',
  pink: '#f472b6',
  sky: '#38bdf8',
  hot: '#ff7a3d',
  cold: '#3a63ff',
  text: '#eef1ff',
  muted: '#aeb6da',
  faint: '#737ba6',
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
const STARS = Array.from(
  {
    length: 104,
  },
  () => ({
    x: RNG() * 1920,
    y: RNG() * 900,
    r: RNG() * 1.5 + 0.35,
    p: RNG() * 6.28,
    s: 0.4 + RNG() * 0.7,
  }),
);
const DUST = Array.from(
  {
    length: 46,
  },
  () => ({
    x: RNG() * 1920,
    y: RNG() * 1080,
    r: RNG() * 2.4 + 0.8,
    p: RNG() * 6.28,
    sp: 0.2 + RNG() * 0.5,
    dx: RNG() - 0.5,
  }),
);
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
    cube: /*#__PURE__*/ React.createElement(
      'g',
      p,
      /*#__PURE__*/ React.createElement('path', {
        d: 'M11 2.5 19 7v8l-8 4.5L3 15V7z',
      }),
      /*#__PURE__*/ React.createElement('path', {
        d: 'M3 7l8 4.5L19 7M11 11.5V20',
      }),
    ),
    quote: /*#__PURE__*/ React.createElement(
      'g',
      p,
      /*#__PURE__*/ React.createElement('rect', {
        x: '3.5',
        y: '3',
        width: '14',
        height: '16',
        rx: '2',
      }),
      /*#__PURE__*/ React.createElement('path', {
        d: 'M7 8h7M7 11.5h7M7 15h4',
      }),
    ),
    cost: /*#__PURE__*/ React.createElement(
      'g',
      p,
      /*#__PURE__*/ React.createElement('circle', {
        cx: '11',
        cy: '11',
        r: '7.5',
      }),
      /*#__PURE__*/ React.createElement('path', {
        d: 'M13.5 8.2c-.6-.8-1.6-1.2-2.6-1.2-1.7 0-2.7.9-2.7 2 0 2.7 5.4 1.4 5.4 4.1 0 1.2-1.1 2.1-2.8 2.1-1.1 0-2.1-.4-2.7-1.3M11 5.4v11.2',
      }),
    ),
    bom: /*#__PURE__*/ React.createElement(
      'g',
      p,
      /*#__PURE__*/ React.createElement('rect', {
        x: '3.5',
        y: '3.5',
        width: '6.2',
        height: '6.2',
        rx: '1.3',
      }),
      /*#__PURE__*/ React.createElement('rect', {
        x: '12.3',
        y: '3.5',
        width: '6.2',
        height: '6.2',
        rx: '1.3',
      }),
      /*#__PURE__*/ React.createElement('rect', {
        x: '3.5',
        y: '12.3',
        width: '6.2',
        height: '6.2',
        rx: '1.3',
      }),
      /*#__PURE__*/ React.createElement('rect', {
        x: '12.3',
        y: '12.3',
        width: '6.2',
        height: '6.2',
        rx: '1.3',
      }),
    ),
    cut: /*#__PURE__*/ React.createElement(
      'g',
      p,
      /*#__PURE__*/ React.createElement('circle', {
        cx: '6',
        cy: '6',
        r: '2.4',
      }),
      /*#__PURE__*/ React.createElement('circle', {
        cx: '6',
        cy: '16',
        r: '2.4',
      }),
      /*#__PURE__*/ React.createElement('path', {
        d: 'M7.8 7.6 19 16M7.8 14.4 19 6',
      }),
    ),
    furnace: /*#__PURE__*/ React.createElement(
      'g',
      p,
      /*#__PURE__*/ React.createElement('rect', {
        x: '3.5',
        y: '4',
        width: '15',
        height: '14',
        rx: '2',
      }),
      /*#__PURE__*/ React.createElement('path', {
        d: 'M3.5 14h15',
      }),
      /*#__PURE__*/ React.createElement('path', {
        d: 'M8 7.5c0 1.2 1.4 1.6 1.4 3 0 .7-.5 1.3-1.4 1.3M13 7c0 1.6 1.6 2 1.6 3.6 0 .8-.6 1.5-1.6 1.5',
      }),
    ),
    gantt: /*#__PURE__*/ React.createElement(
      'g',
      p,
      /*#__PURE__*/ React.createElement('path', {
        d: 'M3.5 4.5h9M3.5 9h13M3.5 13.5h7M3.5 18h11',
      }),
    ),
    user: /*#__PURE__*/ React.createElement(
      'g',
      p,
      /*#__PURE__*/ React.createElement('circle', {
        cx: '11',
        cy: '7.5',
        r: '3.4',
      }),
      /*#__PURE__*/ React.createElement('path', {
        d: 'M4.5 18.5c0-3.4 2.9-5.6 6.5-5.6s6.5 2.2 6.5 5.6',
      }),
    ),
    box: /*#__PURE__*/ React.createElement(
      'g',
      p,
      /*#__PURE__*/ React.createElement('path', {
        d: 'M11 2.6 19 7v8l-8 4.4L3 15V7z',
      }),
      /*#__PURE__*/ React.createElement('path', {
        d: 'M3 7l8 4.4 8-4.4M11 11.4V19.8',
      }),
    ),
    order: /*#__PURE__*/ React.createElement(
      'g',
      p,
      /*#__PURE__*/ React.createElement('rect', {
        x: '4',
        y: '2.6',
        width: '14',
        height: '16.8',
        rx: '2',
      }),
      /*#__PURE__*/ React.createElement('path', {
        d: 'M7.5 7h7M7.5 11h7M7.5 15h4.5',
      }),
    ),
    invoice: /*#__PURE__*/ React.createElement(
      'g',
      p,
      /*#__PURE__*/ React.createElement('path', {
        d: 'M5 2.6h12v16.8l-2.3-1.4-2.3 1.4-2.4-1.4-2.3 1.4L5 17.8z',
      }),
      /*#__PURE__*/ React.createElement('path', {
        d: 'M8.5 8h5M8.5 11.6h5',
      }),
    ),
    b2b: /*#__PURE__*/ React.createElement(
      'g',
      p,
      /*#__PURE__*/ React.createElement('circle', {
        cx: '6',
        cy: '6',
        r: '2.3',
      }),
      /*#__PURE__*/ React.createElement('circle', {
        cx: '16',
        cy: '6',
        r: '2.3',
      }),
      /*#__PURE__*/ React.createElement('circle', {
        cx: '11',
        cy: '16.5',
        r: '2.3',
      }),
      /*#__PURE__*/ React.createElement('path', {
        d: 'M6.6 8.1 10 14.4M15.4 8.1 12 14.4',
      }),
    ),
    pos: /*#__PURE__*/ React.createElement(
      'g',
      p,
      /*#__PURE__*/ React.createElement('rect', {
        x: '2.8',
        y: '5',
        width: '16.4',
        height: '11.5',
        rx: '2',
      }),
      /*#__PURE__*/ React.createElement('path', {
        d: 'M2.8 9h16.4M6 13h3',
      }),
    ),
    ledger: /*#__PURE__*/ React.createElement(
      'g',
      p,
      /*#__PURE__*/ React.createElement('path', {
        d: 'M11 3v15',
      }),
      /*#__PURE__*/ React.createElement('path', {
        d: 'M4.5 6.5h13',
      }),
      /*#__PURE__*/ React.createElement('path', {
        d: 'M4.5 6.5c0 2.4 1.6 4 3.2 4s3.2-1.6 3.2-4M11 6.5c0 2.4 1.6 4 3.2 4s3.2-1.6 3.2-4M5.5 18.5h11',
      }),
    ),
    chart: /*#__PURE__*/ React.createElement(
      'g',
      p,
      /*#__PURE__*/ React.createElement('path', {
        d: 'M3.5 3.5v15h15',
      }),
      /*#__PURE__*/ React.createElement('path', {
        d: 'M6.5 14l3.2-4 3 2.2 4.3-6',
      }),
    ),
    bars: /*#__PURE__*/ React.createElement(
      'g',
      p,
      /*#__PURE__*/ React.createElement('path', {
        d: 'M3.5 18.5h15',
      }),
      /*#__PURE__*/ React.createElement('rect', {
        x: '5',
        y: '11',
        width: '2.6',
        height: '6',
      }),
      /*#__PURE__*/ React.createElement('rect', {
        x: '9.7',
        y: '7',
        width: '2.6',
        height: '10',
      }),
      /*#__PURE__*/ React.createElement('rect', {
        x: '14.4',
        y: '9.5',
        width: '2.6',
        height: '7.5',
      }),
    ),
    spark: /*#__PURE__*/ React.createElement(
      'g',
      p,
      /*#__PURE__*/ React.createElement('path', {
        d: 'M3 13.5 7 9l3 2.4L15 5l4 3',
      }),
    ),
  };
  return /*#__PURE__*/ React.createElement(
    'svg',
    {
      width: size,
      height: size,
      viewBox: '0 0 22 22',
      style: {
        display: 'block',
      },
    },
    paths[type] || paths.bom,
  );
}

/* ── Pill (status) ───────────────────────────────────────────────────────── */
function Pill({ text, accent }) {
  return /*#__PURE__*/ React.createElement(
    'div',
    {
      style: {
        display: 'flex',
        alignItems: 'center',
        gap: 6,
        padding: '4px 10px',
        borderRadius: 999,
        background: hexA(accent, 0.12),
        border: `1px solid ${hexA(accent, 0.32)}`,
      },
    },
    /*#__PURE__*/ React.createElement('span', {
      style: {
        width: 6,
        height: 6,
        borderRadius: 99,
        background: accent,
        boxShadow: `0 0 8px ${accent}`,
      },
    }),
    /*#__PURE__*/ React.createElement(
      'span',
      {
        style: {
          fontFamily: INTER,
          fontSize: 12.5,
          fontWeight: 600,
          color: hexA(accent, 0.95),
          letterSpacing: '0.01em',
        },
      },
      text,
    ),
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
  return /*#__PURE__*/ React.createElement(
    'svg',
    {
      width: w,
      height: h,
      viewBox: `0 0 ${w} ${h}`,
      style: {
        display: 'block',
        overflow: 'visible',
      },
    },
    /*#__PURE__*/ React.createElement(
      'defs',
      null,
      /*#__PURE__*/ React.createElement(
        'linearGradient',
        {
          id: id,
          x1: '0',
          y1: '0',
          x2: '0',
          y2: '1',
        },
        /*#__PURE__*/ React.createElement('stop', {
          offset: '0',
          stopColor: accent,
          stopOpacity: '0.32',
        }),
        /*#__PURE__*/ React.createElement('stop', {
          offset: '1',
          stopColor: accent,
          stopOpacity: '0',
        }),
      ),
    ),
    /*#__PURE__*/ React.createElement('path', {
      d: area,
      fill: `url(#${id})`,
      opacity: clamp((reveal - 0.15) / 0.85, 0, 1),
    }),
    /*#__PURE__*/ React.createElement('path', {
      d: d,
      fill: 'none',
      stroke: accent,
      strokeWidth: '2.4',
      strokeLinecap: 'round',
      strokeDasharray: len,
      strokeDashoffset: len * (1 - reveal),
      style: {
        filter: `drop-shadow(0 0 6px ${hexA(accent, 0.5)})`,
      },
    }),
    reveal > 0.97 &&
      /*#__PURE__*/ React.createElement('circle', {
        cx: pts[pts.length - 1][0],
        cy: pts[pts.length - 1][1],
        r: '3.6',
        fill: '#fff',
        stroke: accent,
        strokeWidth: '2',
      }),
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
  return /*#__PURE__*/ React.createElement(
    'svg',
    {
      width: w,
      height: h,
      viewBox: `0 0 ${w} ${h}`,
      style: {
        display: 'block',
      },
    },
    data.map((v, i) => {
      const bh = v * (h - 8) * clamp(reveal - i * 0.04, 0, 1);
      return /*#__PURE__*/ React.createElement('rect', {
        key: i,
        x: i * (bw + gap),
        y: h - bh,
        width: bw,
        height: Math.max(0.5, bh),
        rx: Math.min(4, bw / 2),
        fill: i % 2 ? accent : hexA(accent, 0.38),
      });
    }),
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
  return /*#__PURE__*/ React.createElement(
    'div',
    {
      style: {
        display: 'flex',
        flexDirection: 'column',
        gap: 11,
        width: '100%',
      },
    },
    rows.map(([label, v], i) =>
      /*#__PURE__*/ React.createElement(
        'div',
        {
          key: i,
          style: {
            display: 'flex',
            alignItems: 'center',
            gap: 12,
          },
        },
        /*#__PURE__*/ React.createElement(
          'span',
          {
            style: {
              fontFamily: INTER,
              fontSize: 13,
              color: C.muted,
              width: 74,
              flexShrink: 0,
            },
          },
          label,
        ),
        /*#__PURE__*/ React.createElement(
          'div',
          {
            style: {
              flex: 1,
              height: 7,
              borderRadius: 99,
              background: 'rgba(255,255,255,0.07)',
              overflow: 'hidden',
            },
          },
          /*#__PURE__*/ React.createElement('div', {
            style: {
              height: '100%',
              width: `${v * 100 * clamp(reveal - i * 0.12, 0, 1)}%`,
              borderRadius: 99,
              background: `linear-gradient(90deg, ${hexA(accent, 0.6)}, ${accent})`,
              boxShadow: `0 0 10px ${hexA(accent, 0.5)}`,
            },
          }),
        ),
      ),
    ),
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
  return /*#__PURE__*/ React.createElement(
    'div',
    {
      style: {
        width: w,
        padding: pad,
        boxSizing: 'border-box',
        borderRadius: compact ? 16 : 19,
        background: 'linear-gradient(158deg, rgba(26,30,55,0.94) 0%, rgba(12,14,28,0.94) 100%)',
        border: '1px solid rgba(255,255,255,0.10)',
        boxShadow: `0 30px 70px -28px rgba(0,0,0,0.85), inset 0 1px 0 rgba(255,255,255,0.08), inset 0 0 60px -40px ${accent}, 0 0 0 1px rgba(255,255,255,0.015)`,
        opacity: op,
        transform: `translateY(${ty}px) scale(${sc})`,
        transformOrigin: 'center',
        willChange: 'transform,opacity',
        position: 'relative',
        overflow: 'hidden',
      },
    },
    /*#__PURE__*/ React.createElement('div', {
      style: {
        position: 'absolute',
        top: 0,
        left: '8%',
        right: '8%',
        height: 1,
        background: `linear-gradient(90deg, transparent, ${hexA(accent, 0.8)}, transparent)`,
      },
    }),
    /*#__PURE__*/ React.createElement(
      'div',
      {
        style: {
          display: 'flex',
          alignItems: 'center',
          gap: 12,
        },
      },
      /*#__PURE__*/ React.createElement(
        'div',
        {
          style: {
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
          },
        },
        /*#__PURE__*/ React.createElement(Glyph, {
          type: glyph,
          color: accent,
          size: compact ? 18 : 21,
        }),
      ),
      /*#__PURE__*/ React.createElement(
        'div',
        {
          style: {
            fontFamily: INTER,
            fontSize: compact ? 13.5 : 15,
            fontWeight: 600,
            color: C.muted,
            letterSpacing: '0.005em',
            flex: 1,
          },
        },
        title,
      ),
      status &&
        /*#__PURE__*/ React.createElement(Pill, {
          text: status[0],
          accent: status[1] || accent,
        }),
    ),
    /*#__PURE__*/ React.createElement(
      'div',
      {
        style: {
          fontFamily: SORA,
          fontSize: compact ? 27 : 43,
          fontWeight: 700,
          color: '#fff',
          marginTop: compact ? 10 : 15,
          letterSpacing: '-0.025em',
          lineHeight: 1,
          textShadow: '0 2px 24px rgba(0,0,0,0.4)',
        },
      },
      value,
    ),
    sub &&
      /*#__PURE__*/ React.createElement(
        'div',
        {
          style: {
            fontFamily: INTER,
            fontSize: compact ? 12.5 : 13.5,
            color: C.faint,
            marginTop: 7,
            lineHeight: 1.35,
          },
        },
        sub,
      ),
    children &&
      /*#__PURE__*/ React.createElement(
        React.Fragment,
        null,
        /*#__PURE__*/ React.createElement('div', {
          style: {
            height: 1,
            background: 'rgba(255,255,255,0.08)',
            margin: compact ? '13px 0 12px' : '17px 0 15px',
          },
        }),
        children,
      ),
  );
}

/* ── Background (persistent atmosphere) ──────────────────────────────────── */
const GRAIN =
  "data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='200' height='200'><filter id='n'><feTurbulence type='fractalNoise' baseFrequency='0.85' numOctaves='2' stitchTiles='stitch'/></filter><rect width='100%25' height='100%25' filter='url(%23n)'/></svg>";
function Background() {
  const t = useTime();
  const gridOp = interpolate([1.4, 3.2, 16, 21], [0, 0.55, 0.55, 0.06], eio)(t);
  const driftA = `translate(${Math.sin(t * 0.07) * 46}px, ${Math.cos(t * 0.05) * 30}px)`;
  const driftB = `translate(${Math.cos(t * 0.06) * -40}px, ${Math.sin(t * 0.045) * 26}px)`;
  const gridShift = (t * 12) % 56;
  return /*#__PURE__*/ React.createElement(
    'div',
    {
      style: {
        position: 'absolute',
        inset: 0,
        overflow: 'hidden',
      },
    },
    /*#__PURE__*/ React.createElement('div', {
      style: {
        position: 'absolute',
        inset: 0,
        background:
          'radial-gradient(135% 100% at 50% 16%, #11152f 0%, #0a0d20 38%, #06070f 72%, #04050b 100%)',
      },
    }),
    /*#__PURE__*/ React.createElement('div', {
      style: {
        position: 'absolute',
        left: '14%',
        top: '-8%',
        width: 980,
        height: 760,
        transform: driftA,
        background: `radial-gradient(circle at 50% 50%, ${hexA(C.indigo, 0.3)} 0%, ${hexA(C.indigo, 0.08)} 38%, transparent 66%)`,
        filter: 'blur(18px)',
      },
    }),
    /*#__PURE__*/ React.createElement('div', {
      style: {
        position: 'absolute',
        right: '8%',
        top: '30%',
        width: 860,
        height: 720,
        transform: driftB,
        background: `radial-gradient(circle at 50% 50%, ${hexA(C.cyan, 0.2)} 0%, ${hexA(C.cyan, 0.05)} 42%, transparent 68%)`,
        filter: 'blur(20px)',
      },
    }),
    /*#__PURE__*/ React.createElement('div', {
      style: {
        position: 'absolute',
        left: '38%',
        bottom: '-18%',
        width: 900,
        height: 560,
        background: `radial-gradient(circle at 50% 50%, ${hexA(C.violet, 0.14)} 0%, transparent 64%)`,
        filter: 'blur(22px)',
      },
    }),
    /*#__PURE__*/ React.createElement(
      'svg',
      {
        width: '1920',
        height: '1080',
        style: {
          position: 'absolute',
          inset: 0,
        },
      },
      STARS.map((s, i) => {
        const tw = 0.35 + 0.65 * (0.5 + 0.5 * Math.sin(t * s.s + s.p));
        return /*#__PURE__*/ React.createElement('circle', {
          key: i,
          cx: s.x,
          cy: s.y,
          r: s.r,
          fill: '#cdd8ff',
          opacity: tw * 0.8,
        });
      }),
    ),
    /*#__PURE__*/ React.createElement('div', {
      style: {
        position: 'absolute',
        left: '-30%',
        right: '-30%',
        bottom: '-6%',
        height: '62%',
        opacity: gridOp,
        transform: 'perspective(760px) rotateX(63deg)',
        transformOrigin: '50% 100%',
        backgroundImage: `linear-gradient(${hexA(C.indigoL, 0.5)} 1px, transparent 1px), linear-gradient(90deg, ${hexA(C.indigoL, 0.5)} 1px, transparent 1px)`,
        backgroundSize: '56px 56px',
        backgroundPosition: `0px ${gridShift}px`,
        WebkitMaskImage:
          'radial-gradient(ellipse 52% 64% at 50% 30%, #000 0%, rgba(0,0,0,0.5) 48%, transparent 78%)',
        maskImage:
          'radial-gradient(ellipse 52% 64% at 50% 30%, #000 0%, rgba(0,0,0,0.5) 48%, transparent 78%)',
      },
    }),
    /*#__PURE__*/ React.createElement(
      'svg',
      {
        width: '1920',
        height: '1080',
        style: {
          position: 'absolute',
          inset: 0,
        },
      },
      DUST.map((d, i) => {
        const y = (d.y - t * 8 * d.sp) % 1080;
        const yy = y < 0 ? y + 1080 : y;
        const x = d.x + Math.sin(t * d.sp + d.p) * 22;
        return /*#__PURE__*/ React.createElement('circle', {
          key: i,
          cx: x,
          cy: yy,
          r: d.r,
          fill: hexA(C.indigoL, 0.5),
          opacity: 0.18 + 0.12 * Math.sin(t * 1.4 + d.p),
        });
      }),
    ),
  );
}

/* ── Atmosphere overlay (grain, vignette, bars) ─────────────────────────── */
function Atmosphere() {
  return /*#__PURE__*/ React.createElement(
    'div',
    {
      style: {
        position: 'absolute',
        inset: 0,
        pointerEvents: 'none',
      },
    },
    /*#__PURE__*/ React.createElement('div', {
      style: {
        position: 'absolute',
        inset: 0,
        background: 'radial-gradient(125% 105% at 50% 44%, transparent 52%, rgba(0,0,0,0.5) 100%)',
      },
    }),
    /*#__PURE__*/ React.createElement('div', {
      style: {
        position: 'absolute',
        inset: 0,
        opacity: 0.04,
        mixBlendMode: 'overlay',
        backgroundImage: `url("${GRAIN}")`,
      },
    }),
    /*#__PURE__*/ React.createElement('div', {
      style: {
        position: 'absolute',
        top: 0,
        left: 0,
        right: 0,
        height: 96,
        background: 'linear-gradient(rgba(4,5,12,0.82), transparent)',
      },
    }),
    /*#__PURE__*/ React.createElement('div', {
      style: {
        position: 'absolute',
        bottom: 0,
        left: 0,
        right: 0,
        height: 120,
        background: 'linear-gradient(transparent, rgba(4,5,12,0.86))',
      },
    }),
  );
}

/* ── Brandmark ───────────────────────────────────────────────────────────── */
function Brandmark() {
  const t = useTime();
  const op =
    (t > 4.4 ? clamp((t - 4.4) / 0.8, 0, 1) : 0) * (t > 34.2 ? clamp((35.0 - t) / 0.8, 0, 1) : 1);
  return /*#__PURE__*/ React.createElement(
    'div',
    {
      style: {
        position: 'absolute',
        top: 54,
        left: 74,
        display: 'flex',
        alignItems: 'center',
        gap: 13,
        opacity: op,
      },
    },
    /*#__PURE__*/ React.createElement('img', {
      src: './corealign-mark.svg',
      alt: '',
      style: {
        width: 40,
        height: 40,
        filter: `drop-shadow(0 0 11px ${hexA(C.indigo, 0.7)})`,
      },
    }),
    /*#__PURE__*/ React.createElement(
      'span',
      {
        style: {
          fontFamily: SORA,
          fontWeight: 700,
          fontSize: 18,
          letterSpacing: '0.04em',
          color: '#fff',
        },
      },
      'CoreAlign',
    ),
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
  return /*#__PURE__*/ React.createElement(
    'div',
    {
      style: {
        position: 'absolute',
        left: 74,
        bottom: 96,
        maxWidth: 760,
        opacity: op,
        transform: `translateY(${(1 - op) * 14}px)`,
        willChange: 'transform,opacity',
      },
    },
    /*#__PURE__*/ React.createElement(
      'div',
      {
        style: {
          display: 'flex',
          alignItems: 'center',
          gap: 11,
          marginBottom: 14,
        },
      },
      /*#__PURE__*/ React.createElement('span', {
        style: {
          width: 30,
          height: 1.5,
          background: `linear-gradient(90deg, ${C.cyan}, transparent)`,
        },
      }),
      /*#__PURE__*/ React.createElement(
        'span',
        {
          style: {
            fontFamily: SORA,
            fontWeight: 600,
            fontSize: 13,
            letterSpacing: '0.28em',
            textTransform: 'uppercase',
            color: C.cyan,
          },
        },
        cur.no,
        ' \xB7 ',
        cur.label,
      ),
    ),
    /*#__PURE__*/ React.createElement(
      'div',
      {
        style: {
          fontFamily: SORA,
          fontWeight: 700,
          fontSize: 44,
          lineHeight: 1.07,
          letterSpacing: '-0.02em',
          background: 'linear-gradient(180deg, #ffffff, #c6cdf4)',
          WebkitBackgroundClip: 'text',
          backgroundClip: 'text',
          color: 'transparent',
        },
      },
      cur.title,
    ),
    /*#__PURE__*/ React.createElement(
      'div',
      {
        style: {
          fontFamily: INTER,
          fontWeight: 400,
          fontSize: 19,
          color: C.muted,
          marginTop: 12,
          maxWidth: 560,
          lineHeight: 1.45,
        },
      },
      cur.sub,
    ),
  );
}

/* ── Scene wrapper (gate + fade + gentle push) ──────────────────────────── */
function Scene({ start, end, fade = 0.55, children }) {
  return /*#__PURE__*/ React.createElement(
    Sprite,
    {
      start: start,
      end: end,
    },
    ({ localTime, duration }) => {
      const op = win(localTime, 0, duration, fade);
      const sc = 1 + 0.04 * (localTime / duration);
      return /*#__PURE__*/ React.createElement(
        'div',
        {
          style: {
            position: 'absolute',
            inset: 0,
            opacity: op,
            transform: `scale(${sc})`,
            transformOrigin: '50% 48%',
            willChange: 'transform,opacity',
          },
        },
        children,
      );
    },
  );
}
function World({ children }) {
  const t = useTime();
  return /*#__PURE__*/ React.createElement(
    'div',
    {
      style: {
        position: 'absolute',
        inset: 0,
        transform: `translate(${Math.sin(t * 0.45) * 5}px, ${Math.cos(t * 0.38) * 4}px)`,
      },
    },
    children,
  );
}
function Center({ children, x = 960, y = 540, style }) {
  return /*#__PURE__*/ React.createElement(
    'div',
    {
      style: {
        position: 'absolute',
        left: x,
        top: y,
        transform: 'translate(-50%,-50%)',
        ...style,
      },
    },
    children,
  );
}

/* ── Scene 1 · Brand intro (CoreAlign fades in, dissolves into CAD) ──────── */
function SceneIntro() {
  const { localTime: lt } = useSprite();
  const s = 0.95 + 0.05 * eo(clamp(lt / 1.8, 0, 1));
  const tag = eo(clamp((lt - 0.5) / 1.2, 0, 1));
  return /*#__PURE__*/ React.createElement(
    'div',
    {
      style: {
        position: 'absolute',
        inset: 0,
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
      },
    },
    /*#__PURE__*/ React.createElement(
      'div',
      {
        style: {
          display: 'flex',
          alignItems: 'center',
          gap: 18,
          transform: `scale(${s})`,
        },
      },
      /*#__PURE__*/ React.createElement('img', {
        src: './corealign-mark.svg',
        alt: '',
        style: {
          width: 74,
          height: 74,
          filter: `drop-shadow(0 0 28px ${hexA(C.indigo, 0.6)})`,
        },
      }),
      /*#__PURE__*/ React.createElement(
        'div',
        {
          style: {
            fontFamily: SORA,
            fontWeight: 800,
            fontSize: 100,
            letterSpacing: '-0.03em',
            lineHeight: 1,
            background: 'linear-gradient(105deg, #ffffff 0%, #c2c8ff 46%, #6ee7ff 100%)',
            WebkitBackgroundClip: 'text',
            backgroundClip: 'text',
            color: 'transparent',
          },
        },
        'CoreAlign',
      ),
    ),
    /*#__PURE__*/ React.createElement(
      'div',
      {
        style: {
          fontFamily: INTER,
          fontSize: 24,
          color: '#d7dcf5',
          marginTop: 24,
          opacity: tag,
          transform: `translateY(${(1 - tag) * 8}px)`,
        },
      },
      'Tasar\u0131mdan muhasebeye \u2014 cam & do\u011Frama i\xE7in tek platform',
    ),
  );
}

/* ── CAD section top headline (fills the empty upper area) ───────────────── */
function TopHeadline() {
  const { localTime: lt } = useSprite();
  const op = win(lt, 0.6, 13.2, 0.8);
  return /*#__PURE__*/ React.createElement(
    'div',
    {
      style: {
        position: 'absolute',
        top: 240,
        left: '50%',
        transform: `translate(-50%, ${(1 - clamp(op, 0, 1)) * -10}px)`,
        textAlign: 'center',
        opacity: op,
        width: 1000,
        pointerEvents: 'none',
      },
    },
    /*#__PURE__*/ React.createElement(
      'div',
      {
        style: {
          fontFamily: SORA,
          fontWeight: 600,
          fontSize: 12.5,
          letterSpacing: '0.3em',
          textTransform: 'uppercase',
          color: C.cyan,
          marginBottom: 12,
        },
      },
      '3D CAD \xB7 CAM Motoru',
    ),
    /*#__PURE__*/ React.createElement(
      'div',
      {
        style: {
          fontFamily: SORA,
          fontWeight: 700,
          fontSize: 34,
          lineHeight: 1.18,
          letterSpacing: '-0.02em',
          background: 'linear-gradient(180deg, #ffffff, #c6cdf4)',
          WebkitBackgroundClip: 'text',
          backgroundClip: 'text',
          color: 'transparent',
        },
      },
      'Her cam formu \u2014 \xF6l\xE7\xFCs\xFC, profili ve \xFCretimiyle tek modelde.',
    ),
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
  return /*#__PURE__*/ React.createElement(
    'div',
    {
      style: {
        position: 'absolute',
        top: 150,
        right: 92,
        width: 316,
        opacity: op,
        pointerEvents: 'none',
      },
    },
    /*#__PURE__*/ React.createElement(
      'div',
      {
        style: {
          display: 'flex',
          alignItems: 'center',
          gap: 9,
          marginBottom: 15,
        },
      },
      /*#__PURE__*/ React.createElement('span', {
        style: {
          width: 24,
          height: 1.5,
          background: `linear-gradient(90deg, ${C.cyan}, transparent)`,
        },
      }),
      /*#__PURE__*/ React.createElement(
        'span',
        {
          style: {
            fontFamily: SORA,
            fontWeight: 600,
            fontSize: 11.5,
            letterSpacing: '0.26em',
            textTransform: 'uppercase',
            color: C.cyan,
          },
        },
        'CAD Motoru',
      ),
    ),
    /*#__PURE__*/ React.createElement(
      'div',
      {
        style: {
          display: 'flex',
          flexDirection: 'column',
          gap: 11,
        },
      },
      items.map((it, i) => {
        const ap = eo(clamp((lt - it[1]) / 0.5, 0, 1));
        return /*#__PURE__*/ React.createElement(
          'div',
          {
            key: i,
            style: {
              display: 'flex',
              alignItems: 'center',
              gap: 11,
              opacity: 0.3 + 0.7 * ap,
              transform: `translateX(${(1 - ap) * 10}px)`,
            },
          },
          /*#__PURE__*/ React.createElement(
            'div',
            {
              style: {
                width: 21,
                height: 21,
                borderRadius: 6,
                flexShrink: 0,
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                background: hexA(it[2], 0.04 + 0.14 * ap),
                border: `1px solid ${hexA(it[2], 0.28 + 0.4 * ap)}`,
              },
            },
            /*#__PURE__*/ React.createElement(
              'svg',
              {
                width: '11',
                height: '11',
                viewBox: '0 0 12 12',
              },
              /*#__PURE__*/ React.createElement('path', {
                d: 'M2.4 6.4l2.3 2.3 5-5.6',
                fill: 'none',
                stroke: it[2],
                strokeWidth: '1.8',
                strokeLinecap: 'round',
                strokeLinejoin: 'round',
                opacity: ap,
              }),
            ),
          ),
          /*#__PURE__*/ React.createElement(
            'span',
            {
              style: {
                fontFamily: INTER,
                fontSize: 14,
                fontWeight: 500,
                color: ap > 0.5 ? C.text : C.faint,
              },
            },
            it[0],
          ),
        );
      }),
    ),
  );
}
function SceneCAD() {
  return /*#__PURE__*/ React.createElement(
    'div',
    {
      style: {
        position: 'absolute',
        inset: 0,
      },
    },
    /*#__PURE__*/ React.createElement(CADDemos, null),
    /*#__PURE__*/ React.createElement(TopHeadline, null),
    /*#__PURE__*/ React.createElement(FeatureTicker, null),
  );
}

/* ── Scene 3 · Teklif / Maliyet / BOM ───────────────────────────────────── */
function SceneTeklif() {
  const { localTime: lt } = useSprite();
  const a = (d) => eo(clamp((lt - d) / 0.7, 0, 1));
  const r = (d) => clamp((lt - d - 0.25) / 1.0, 0, 1);
  const teklif = '€ ' + trNum(Math.round(12480 * clamp((lt - 0.3) / 1.2, 0, 1)));
  return /*#__PURE__*/ React.createElement(
    'div',
    {
      style: {
        position: 'absolute',
        inset: 0,
      },
    },
    /*#__PURE__*/ React.createElement(
      'svg',
      {
        width: '1920',
        height: '1080',
        style: {
          position: 'absolute',
          inset: 0,
          pointerEvents: 'none',
        },
      },
      (() => {
        const A = [362, 320],
          B = [432, 452],
          mx = 405,
          my = 406;
        const dd = `M ${A[0]} ${A[1]} Q ${mx} ${my} ${B[0]} ${B[1]}`;
        let u = ((lt - 0.2) * 0.5) % 1;
        if (u < 0) u += 1;
        const bx = (1 - u) * (1 - u) * A[0] + 2 * (1 - u) * u * mx + u * u * B[0];
        const by = (1 - u) * (1 - u) * A[1] + 2 * (1 - u) * u * my + u * u * B[1];
        return /*#__PURE__*/ React.createElement(
          'g',
          {
            opacity: a(0),
          },
          /*#__PURE__*/ React.createElement('path', {
            d: dd,
            fill: 'none',
            stroke: hexA(C.cyan, 0.45),
            strokeWidth: '1.5',
            strokeDasharray: '5 6',
          }),
          /*#__PURE__*/ React.createElement('circle', {
            cx: bx,
            cy: by,
            r: '3.2',
            fill: '#eafcff',
            opacity: seg(lt, 0.3, 1.2),
            style: {
              filter: `drop-shadow(0 0 7px ${C.cyan})`,
            },
          }),
        );
      })(),
    ),
    /*#__PURE__*/ React.createElement(
      Center,
      {
        x: 250,
        y: 300,
        style: {
          opacity: a(0),
        },
      },
      /*#__PURE__*/ React.createElement(
        'div',
        {
          style: {
            display: 'flex',
            alignItems: 'center',
            gap: 10,
            padding: '10px 16px',
            borderRadius: 13,
            background: 'rgba(16,20,40,0.86)',
            border: `1px solid ${hexA(C.indigoL, 0.4)}`,
          },
        },
        /*#__PURE__*/ React.createElement(Glyph, {
          type: 'cube',
          color: C.indigoL,
          size: 20,
        }),
        /*#__PURE__*/ React.createElement(
          'span',
          {
            style: {
              fontFamily: INTER,
              fontSize: 15,
              fontWeight: 600,
              color: C.text,
            },
          },
          'Tasar\u0131m',
        ),
        /*#__PURE__*/ React.createElement(
          'span',
          {
            style: {
              fontFamily: INTER,
              fontSize: 13,
              color: C.faint,
            },
          },
          '\xB7 5460\xD72400',
        ),
      ),
    ),
    /*#__PURE__*/ React.createElement(
      Center,
      {
        x: 520,
        y: 560,
      },
      /*#__PURE__*/ React.createElement(
        Card,
        {
          w: 392,
          appear: a(0.15),
          accent: C.cyan,
          glyph: 'quote',
          title: 'Teklif',
          value: teklif,
          sub: '3 kalem \xB7 onaya haz\u0131r',
          status: ['Hazır', C.cyan],
        },
        /*#__PURE__*/ React.createElement(Sparkline, {
          accent: C.cyan,
          w: 344,
          h: 76,
          reveal: r(0.2),
          seed: 3,
        }),
      ),
    ),
    /*#__PURE__*/ React.createElement(
      Center,
      {
        x: 960,
        y: 520,
      },
      /*#__PURE__*/ React.createElement(
        Card,
        {
          w: 392,
          appear: a(0.4),
          accent: C.emerald,
          glyph: 'cost',
          title: 'Maliyet',
          value: '\u20AC 7.310',
          sub: 'malzeme + i\u015F\xE7ilik',
          status: ['Marj %41', C.emerald],
        },
        /*#__PURE__*/ React.createElement(Bars, {
          accent: C.emerald,
          w: 344,
          h: 76,
          reveal: r(0.45),
          data: [0.5, 0.72, 0.46, 0.62, 0.84, 0.58, 0.9, 0.66],
        }),
      ),
    ),
    /*#__PURE__*/ React.createElement(
      Center,
      {
        x: 1400,
        y: 560,
      },
      /*#__PURE__*/ React.createElement(
        Card,
        {
          w: 392,
          appear: a(0.65),
          accent: C.indigoL,
          glyph: 'bom',
          title: 'BOM \xB7 Malzeme Listesi',
          value: '126 kalem',
          sub: 'cam \xB7 profil \xB7 donan\u0131m',
        },
        /*#__PURE__*/ React.createElement(ProgressRows, {
          accent: C.indigoL,
          reveal: r(0.7),
        }),
      ),
    ),
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
  return /*#__PURE__*/ React.createElement(
    'svg',
    {
      width: W,
      height: H,
      viewBox: `0 0 ${W} ${H}`,
      style: {
        display: 'block',
      },
    },
    /*#__PURE__*/ React.createElement('rect', {
      x: '0',
      y: '0',
      width: W,
      height: H,
      rx: '7',
      fill: 'rgba(255,255,255,0.03)',
      stroke: 'rgba(255,255,255,0.08)',
    }),
    Array.from({
      length: cols * rows,
    }).map((_, i) => {
      if (i >= 22) return null;
      const c = i % cols,
        rr = Math.floor(i / cols),
        p = clamp(reveal * 1.15 - i * 0.04, 0, 1);
      return /*#__PURE__*/ React.createElement('rect', {
        key: i,
        x: c * (pw + gx),
        y: rr * (ph + gx),
        width: pw,
        height: ph,
        rx: '3',
        fill: hexA(NEST_PAL[i % NEST_PAL.length], 0.7),
        stroke: hexA(NEST_PAL[i % NEST_PAL.length], 0.9),
        strokeWidth: '0.8',
        opacity: p,
        transform: `scale(${0.7 + 0.3 * p})`,
        style: {
          transformOrigin: `${c * (pw + gx) + pw / 2}px ${rr * (ph + gx) + ph / 2}px`,
        },
      });
    }),
  );
}
function Furnace({ heat, lt }) {
  const col = lerpHex(C.cold, C.hot, heat);
  return /*#__PURE__*/ React.createElement(
    'div',
    {
      style: {
        display: 'flex',
        flexDirection: 'column',
        gap: 12,
      },
    },
    /*#__PURE__*/ React.createElement(
      'svg',
      {
        width: 336,
        height: 120,
        viewBox: '0 0 336 120',
      },
      /*#__PURE__*/ React.createElement('rect', {
        x: '2',
        y: '6',
        width: '332',
        height: '108',
        rx: '10',
        fill: hexA(col, 0.16),
        stroke: hexA(C.hot, 0.4 + 0.3 * heat),
        strokeWidth: '1.4',
      }),
      Array.from({
        length: 7,
      }).map((_, i) => {
        const flick = 0.55 + 0.45 * Math.sin(lt * 5 + i);
        return /*#__PURE__*/ React.createElement('rect', {
          key: i,
          x: 26 + i * 44,
          y: 28,
          width: 22,
          height: 64,
          rx: 5,
          fill: lerpHex('#2a3a8a', C.hot, heat * flick),
          opacity: 0.5 + 0.5 * heat,
          style: {
            filter: `drop-shadow(0 0 ${8 * heat}px ${hexA(C.hot, 0.7)})`,
          },
        });
      }),
      /*#__PURE__*/ React.createElement('line', {
        x1: '14',
        y1: '96',
        x2: '322',
        y2: '96',
        stroke: hexA(C.hot, 0.5),
        strokeWidth: '1',
      }),
    ),
    /*#__PURE__*/ React.createElement(
      'div',
      {
        style: {
          display: 'flex',
          justifyContent: 'space-between',
          fontFamily: MONO,
          fontSize: 13,
          color: C.muted,
        },
      },
      /*#__PURE__*/ React.createElement('span', null, '\xE7evrim 5/g\xFCn'),
      /*#__PURE__*/ React.createElement(
        'span',
        {
          style: {
            color: heat > 0.94 ? C.emerald : lerpHex('#8fb0ff', C.amber, heat),
          },
        },
        heat > 0.94 ? 'hazır ✓' : 'ısınıyor…',
      ),
    ),
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
  return /*#__PURE__*/ React.createElement(
    'svg',
    {
      width: W,
      height: 132,
      viewBox: `0 0 ${W} 132`,
    },
    rows.map((r, i) => {
      const x0 = r[0] * W * 0.5,
        full = r[1] * W * 0.78,
        p = clamp(reveal * 1.2 - i * 0.12, 0, 1);
      return /*#__PURE__*/ React.createElement(
        'g',
        {
          key: i,
        },
        /*#__PURE__*/ React.createElement('rect', {
          x: '0',
          y: 8 + i * 25,
          width: W,
          height: 12,
          rx: '6',
          fill: 'rgba(255,255,255,0.04)',
        }),
        /*#__PURE__*/ React.createElement('rect', {
          x: x0,
          y: 8 + i * 25,
          width: full * p,
          height: 12,
          rx: '6',
          fill: hexA(r[2], 0.85),
          style: {
            filter: `drop-shadow(0 0 8px ${hexA(r[2], 0.5)})`,
          },
        }),
      );
    }),
  );
}
function SceneMRP() {
  const { localTime: lt } = useSprite();
  const a = (d) => eo(clamp((lt - d) / 0.7, 0, 1));
  const r = (d) => clamp((lt - d) / 1.6, 0, 1);
  const fheat = seg(lt - 0.4, 0.5, 3.2, es),
    ftemp = Math.round(lerp(60, 700, fheat));
  return /*#__PURE__*/ React.createElement(
    'div',
    {
      style: {
        position: 'absolute',
        inset: 0,
      },
    },
    /*#__PURE__*/ React.createElement(
      Center,
      {
        x: 350,
        y: 540,
      },
      /*#__PURE__*/ React.createElement(
        Card,
        {
          w: 400,
          appear: a(0.1),
          accent: C.cyan,
          glyph: 'cut',
          title: 'Kesim \xB7 Nesting',
          value: '%98,8 verim',
          sub: '42 par\xE7a \xB7 6 levha',
        },
        /*#__PURE__*/ React.createElement(Nesting, {
          reveal: r(0.4),
        }),
      ),
    ),
    /*#__PURE__*/ React.createElement(
      Center,
      {
        x: 960,
        y: 540,
      },
      /*#__PURE__*/ React.createElement(
        Card,
        {
          w: 400,
          appear: a(0.32),
          accent: C.amber,
          glyph: 'furnace',
          title: 'Temper F\u0131r\u0131n\u0131',
          value: ftemp + '°C',
          sub: '\u0131s\u0131l i\u015Flem \xB7 700\xB0C hedef',
        },
        /*#__PURE__*/ React.createElement(Furnace, {
          heat: fheat,
          lt: lt - 0.4,
        }),
      ),
    ),
    /*#__PURE__*/ React.createElement(
      Center,
      {
        x: 1570,
        y: 540,
      },
      /*#__PURE__*/ React.createElement(
        Card,
        {
          w: 400,
          appear: a(0.54),
          accent: C.indigoL,
          glyph: 'gantt',
          title: '\u0130\u015F Emri Plan\u0131',
          value: '212 emir',
          sub: 'kapasite kullan\u0131m\u0131',
          status: ['%86', C.indigoL],
        },
        /*#__PURE__*/ React.createElement(GanttBars, {
          reveal: r(0.7),
        }),
      ),
    ),
  );
}

/* ── Scene 5 · Business flow ─────────────────────────────────────────────── */
const HUBS = [
  {
    id: 'tas',
    t: 'Tasarım',
    v: 'kaynak',
    acc: C.indigoL,
    g: 'cube',
    x: 170,
    y: 560,
  },
  {
    id: 'tek',
    t: 'Teklif',
    v: '€12.480',
    acc: C.cyan,
    g: 'quote',
    x: 330,
    y: 330,
  },
  {
    id: 'sip',
    t: 'Sipariş',
    v: '212 açık',
    acc: C.indigo,
    g: 'order',
    x: 520,
    y: 560,
  },
  {
    id: 'b2b',
    t: 'B2B',
    v: '+38',
    acc: C.cyan,
    g: 'b2b',
    x: 700,
    y: 330,
  },
  {
    id: 'fat',
    t: 'Fatura',
    v: '€2,4M',
    acc: C.violet,
    g: 'invoice',
    x: 870,
    y: 560,
  },
  {
    id: 'stk',
    t: 'Stok',
    v: '%99',
    acc: C.emerald,
    g: 'box',
    x: 1050,
    y: 330,
  },
  {
    id: 'pos',
    t: 'Sanal POS',
    v: '%100',
    acc: C.cyan,
    g: 'pos',
    x: 1220,
    y: 560,
  },
  {
    id: 'car',
    t: 'Cariler',
    v: '1.240',
    acc: C.indigoL,
    g: 'user',
    x: 1400,
    y: 330,
  },
  {
    id: 'muh',
    t: 'Muhasebe',
    v: 'Dengeli',
    acc: C.emerald,
    g: 'ledger',
    x: 1570,
    y: 560,
  },
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
    pts: Array.from(
      {
        length: 60,
      },
      (_, i) => {
        const t = i / 59;
        const x = (1 - t) * (1 - t) * a.x + 2 * (1 - t) * t * mx + t * t * b.x,
          y = (1 - t) * (1 - t) * a.y + 2 * (1 - t) * t * my + t * t * b.y;
        return [x, y];
      },
    ),
    d: `M ${a.x} ${a.y} Q ${mx} ${my} ${b.x} ${b.y}`,
  };
}
function FlowHub({ h, appear }) {
  return /*#__PURE__*/ React.createElement(
    'div',
    {
      style: {
        position: 'absolute',
        left: h.x,
        top: h.y,
        transform: `translate(-50%,-50%) scale(${0.9 + 0.1 * appear})`,
        opacity: appear,
        willChange: 'transform,opacity',
      },
    },
    /*#__PURE__*/ React.createElement(
      'div',
      {
        style: {
          display: 'flex',
          alignItems: 'center',
          gap: 11,
          padding: '12px 16px',
          borderRadius: 15,
          minWidth: 152,
          background: 'linear-gradient(155deg, rgba(24,28,52,0.96), rgba(11,13,26,0.96))',
          border: `1px solid ${hexA(h.acc, 0.34)}`,
          boxShadow: `0 18px 44px -18px rgba(0,0,0,0.85), inset 0 1px 0 rgba(255,255,255,0.07), 0 0 50px -30px ${h.acc}`,
        },
      },
      /*#__PURE__*/ React.createElement(
        'div',
        {
          style: {
            width: 34,
            height: 34,
            borderRadius: 10,
            flexShrink: 0,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            background: hexA(h.acc, 0.14),
            border: `1px solid ${hexA(h.acc, 0.4)}`,
          },
        },
        /*#__PURE__*/ React.createElement(Glyph, {
          type: h.g,
          color: h.acc,
          size: 19,
        }),
      ),
      /*#__PURE__*/ React.createElement(
        'div',
        null,
        /*#__PURE__*/ React.createElement(
          'div',
          {
            style: {
              fontFamily: INTER,
              fontSize: 14.5,
              fontWeight: 600,
              color: C.text,
              lineHeight: 1.1,
            },
          },
          h.t,
        ),
        /*#__PURE__*/ React.createElement(
          'div',
          {
            style: {
              fontFamily: MONO,
              fontSize: 12,
              color: hexA(h.acc, 0.95),
              marginTop: 2,
            },
          },
          h.v,
        ),
      ),
    ),
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
  return /*#__PURE__*/ React.createElement(
    'div',
    {
      style: {
        position: 'absolute',
        inset: 0,
      },
    },
    /*#__PURE__*/ React.createElement(
      'svg',
      {
        width: '1920',
        height: '1080',
        style: {
          position: 'absolute',
          inset: 0,
        },
      },
      curves.map((c, i) =>
        /*#__PURE__*/ React.createElement('path', {
          key: i,
          d: c.d,
          fill: 'none',
          stroke: hexA(C.indigoL, 0.3),
          strokeWidth: '1.6',
          strokeDasharray: '1200',
          strokeDashoffset: 1200 * (1 - lineP),
        }),
      ),
      pulses.map((p, i) =>
        /*#__PURE__*/ React.createElement('circle', {
          key: i,
          cx: p.x,
          cy: p.y,
          r: 3.4,
          fill: '#eafcff',
          opacity: p.o * lineP,
          style: {
            filter: `drop-shadow(0 0 7px ${C.cyan})`,
          },
        }),
      ),
    ),
    HUBS.map((h, i) =>
      /*#__PURE__*/ React.createElement(FlowHub, {
        key: h.id,
        h: h,
        appear: appearOf(i),
      }),
    ),
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
  return /*#__PURE__*/ React.createElement(
    'svg',
    {
      width: W,
      height: H,
      viewBox: `0 0 ${W} ${H}`,
      style: {
        display: 'block',
      },
    },
    /*#__PURE__*/ React.createElement(
      'defs',
      null,
      /*#__PURE__*/ React.createElement(
        'linearGradient',
        {
          id: 'biA',
          x1: '0',
          y1: '0',
          x2: '0',
          y2: '1',
        },
        /*#__PURE__*/ React.createElement('stop', {
          offset: '0',
          stopColor: C.cyan,
          stopOpacity: '0.3',
        }),
        /*#__PURE__*/ React.createElement('stop', {
          offset: '1',
          stopColor: C.cyan,
          stopOpacity: '0',
        }),
      ),
    ),
    [0, 0.25, 0.5, 0.75, 1].map((g, i) =>
      /*#__PURE__*/ React.createElement('line', {
        key: i,
        x1: padL,
        y1: padT + g * (H - padB - padT),
        x2: W - 20,
        y2: padT + g * (H - padB - padT),
        stroke: 'rgba(255,255,255,0.05)',
        strokeWidth: '1',
      }),
    ),
    months.map((m, i) =>
      /*#__PURE__*/ React.createElement(
        'text',
        {
          key: i,
          x: padL + (i / 11) * (W * 0.62 - padL),
          y: H - 12,
          fill: C.faint,
          textAnchor: 'middle',
          style: {
            fontFamily: MONO,
            fontSize: 11,
          },
        },
        m,
      ),
    ),
    /*#__PURE__*/ React.createElement('path', {
      d: area,
      fill: 'url(#biA)',
      opacity: drawM,
    }),
    /*#__PURE__*/ React.createElement('path', {
      d: dMain,
      fill: 'none',
      stroke: C.cyan,
      strokeWidth: '3',
      strokeLinecap: 'round',
      strokeDasharray: '1400',
      strokeDashoffset: 1400 * (1 - drawM),
      style: {
        filter: `drop-shadow(0 0 7px ${hexA(C.cyan, 0.6)})`,
      },
    }),
    /*#__PURE__*/ React.createElement('path', {
      d: dFore,
      fill: 'none',
      stroke: C.amber,
      strokeWidth: '2.6',
      strokeLinecap: 'round',
      strokeDasharray: '10 8',
      opacity: drawF,
      style: {
        filter: `drop-shadow(0 0 7px ${hexA(C.amber, 0.5)})`,
      },
    }),
    drawF > 0.5 &&
      /*#__PURE__*/ React.createElement(
        'g',
        {
          opacity: drawF,
        },
        /*#__PURE__*/ React.createElement('circle', {
          cx: fore[fore.length - 1][0],
          cy: fore[fore.length - 1][1],
          r: '4.5',
          fill: '#fff',
          stroke: C.amber,
          strokeWidth: '2',
        }),
        /*#__PURE__*/ React.createElement('rect', {
          x: fore[fore.length - 1][0] - 58,
          y: fore[fore.length - 1][1] - 40,
          width: 116,
          height: 24,
          rx: 6,
          fill: 'rgba(9,12,24,0.9)',
          stroke: hexA(C.amber, 0.4),
        }),
        /*#__PURE__*/ React.createElement(
          'text',
          {
            x: fore[fore.length - 1][0],
            y: fore[fore.length - 1][1] - 23,
            fill: C.amber,
            textAnchor: 'middle',
            style: {
              fontFamily: MONO,
              fontSize: 13,
            },
          },
          'tahmin \u25B2',
        ),
      ),
  );
}
function SceneBI() {
  const { localTime: lt } = useSprite();
  const a = (d) => eo(clamp((lt - d) / 0.7, 0, 1));
  const ciro = '€ ' + (clamp((lt - 0.4) / 1.6, 0, 1) * 4.2).toFixed(1).replace('.', ',') + 'M';
  return /*#__PURE__*/ React.createElement(
    'div',
    {
      style: {
        position: 'absolute',
        inset: 0,
      },
    },
    /*#__PURE__*/ React.createElement(
      Center,
      {
        x: 730,
        y: 540,
      },
      /*#__PURE__*/ React.createElement(
        Card,
        {
          w: 900,
          appear: a(0.05),
          accent: C.cyan,
          glyph: 'chart',
          title: 'Canl\u0131 Pano \xB7 BI',
          value: ciro,
          sub: 'ciro \xB7 son 12 ay + \xF6ng\xF6r\xFC',
          status: ['Canlı', C.emerald],
        },
        /*#__PURE__*/ React.createElement(BIChart, {
          reveal: clamp((lt - 0.6) / 2.4, 0, 1),
        }),
      ),
    ),
    /*#__PURE__*/ React.createElement(
      Center,
      {
        x: 1500,
        y: 400,
      },
      /*#__PURE__*/ React.createElement(Card, {
        w: 356,
        appear: a(0.45),
        accent: C.emerald,
        glyph: 'cost',
        title: 'Br\xFCt Marj',
        value: '%38',
        sub: '\u25B2 4 puan / y\u0131l',
        compact: true,
      }),
    ),
    /*#__PURE__*/ React.createElement(
      Center,
      {
        x: 1500,
        y: 560,
      },
      /*#__PURE__*/ React.createElement(Card, {
        w: 356,
        appear: a(0.62),
        accent: C.violet,
        glyph: 'bars',
        title: 'B\xFCy\xFCme',
        value: '\u25B2 %24',
        sub: 'y\u0131ll\u0131k ciro art\u0131\u015F\u0131',
        compact: true,
      }),
    ),
    /*#__PURE__*/ React.createElement(
      Center,
      {
        x: 1500,
        y: 720,
      },
      /*#__PURE__*/ React.createElement(Card, {
        w: 356,
        appear: a(0.79),
        accent: C.cyan,
        glyph: 'invoice',
        title: 'Nakit Ak\u0131\u015F\u0131',
        value: '\u20AC 1,2M',
        sub: '30 g\xFCnl\xFCk projeksiyon',
        compact: true,
      }),
    ),
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
  return /*#__PURE__*/ React.createElement(
    'div',
    {
      style: {
        position: 'absolute',
        inset: 0,
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
      },
    },
    /*#__PURE__*/ React.createElement(
      'div',
      {
        style: {
          display: 'flex',
          alignItems: 'center',
          gap: 18,
          opacity: a(0.0),
          transform: `translateY(${(1 - a(0.0)) * 20}px)`,
        },
      },
      /*#__PURE__*/ React.createElement('img', {
        src: './corealign-mark.svg',
        alt: '',
        style: {
          width: 77,
          height: 77,
          filter: `drop-shadow(0 0 30px ${hexA(C.indigo, 0.6)})`,
        },
      }),
      /*#__PURE__*/ React.createElement(
        'div',
        {
          style: {
            fontFamily: SORA,
            fontWeight: 800,
            fontSize: 104,
            letterSpacing: '-0.03em',
            lineHeight: 1,
            background: 'linear-gradient(105deg, #ffffff 0%, #c2c8ff 46%, #6ee7ff 100%)',
            WebkitBackgroundClip: 'text',
            backgroundClip: 'text',
            color: 'transparent',
          },
        },
        'CoreAlign',
      ),
    ),
    /*#__PURE__*/ React.createElement(
      'div',
      {
        style: {
          fontFamily: INTER,
          fontSize: 25,
          color: '#d7dcf5',
          marginTop: 26,
          opacity: a(0.45),
          textAlign: 'center',
        },
      },
      'Tasar\u0131mdan muhasebeye \u2014 cam & do\u011Frama i\xE7in tek platform',
    ),
    /*#__PURE__*/ React.createElement(
      'div',
      {
        style: {
          display: 'flex',
          flexWrap: 'wrap',
          gap: 10,
          justifyContent: 'center',
          marginTop: 34,
          maxWidth: 920,
          opacity: a(0.75),
        },
      },
      chips.map((c, i) =>
        /*#__PURE__*/ React.createElement(
          React.Fragment,
          {
            key: i,
          },
          /*#__PURE__*/ React.createElement(
            'span',
            {
              style: {
                fontFamily: INTER,
                fontSize: 14.5,
                fontWeight: 600,
                color: '#cfd4f5',
                padding: '9px 16px',
                borderRadius: 999,
                border: '1px solid rgba(129,140,248,0.34)',
                background: 'rgba(99,102,241,0.07)',
              },
            },
            c,
          ),
          i < chips.length - 1 &&
            /*#__PURE__*/ React.createElement(
              'span',
              {
                style: {
                  color: C.cyan,
                  alignSelf: 'center',
                  opacity: 0.6,
                },
              },
              '\u203A',
            ),
        ),
      ),
    ),
    /*#__PURE__*/ React.createElement(
      'div',
      {
        style: {
          display: 'flex',
          gap: 16,
          marginTop: 42,
          opacity: a(1.05),
        },
      },
      /*#__PURE__*/ React.createElement(
        'div',
        {
          style: {
            fontFamily: SORA,
            fontWeight: 600,
            fontSize: 18,
            color: '#fff',
            padding: '16px 34px',
            borderRadius: 15,
            background: `linear-gradient(135deg, ${C.indigo}, ${C.indigoD})`,
            boxShadow: `0 14px 40px ${hexA(C.indigoD, 0.55)}`,
          },
        },
        '\xDCcretsiz deneyin',
      ),
      /*#__PURE__*/ React.createElement(
        'div',
        {
          style: {
            fontFamily: SORA,
            fontWeight: 600,
            fontSize: 18,
            color: '#e2e6ff',
            padding: '16px 34px',
            borderRadius: 15,
            border: '1px solid rgba(165,175,255,0.4)',
            background: 'rgba(255,255,255,0.03)',
          },
        },
        'Demo planlay\u0131n',
      ),
    ),
  );
}

/* ── Root ────────────────────────────────────────────────────────────────── */
function CoreAlignHero() {
  const reduce =
    typeof window !== 'undefined' &&
    window.matchMedia &&
    window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  return /*#__PURE__*/ React.createElement(
    Stage,
    {
      width: 1920,
      height: 1080,
      duration: 38.4,
      background: '#04050b',
      persistKey: 'corealign-hero',
      autoplay: !reduce,
      loop: true,
    },
    /*#__PURE__*/ React.createElement(Background, null),
    /*#__PURE__*/ React.createElement(
      World,
      null,
      /*#__PURE__*/ React.createElement(
        Scene,
        {
          start: 0,
          end: 4.0,
          fade: 0.95,
        },
        /*#__PURE__*/ React.createElement(SceneIntro, null),
      ),
      /*#__PURE__*/ React.createElement(
        Scene,
        {
          start: 3.2,
          end: 16.8,
        },
        ' ',
        /*#__PURE__*/ React.createElement(SceneCAD, null),
      ),
      /*#__PURE__*/ React.createElement(
        Scene,
        {
          start: 16.7,
          end: 20.9,
        },
        ' ',
        /*#__PURE__*/ React.createElement(SceneTeklif, null),
      ),
      /*#__PURE__*/ React.createElement(
        Scene,
        {
          start: 20.8,
          end: 25.8,
        },
        ' ',
        /*#__PURE__*/ React.createElement(SceneMRP, null),
      ),
      /*#__PURE__*/ React.createElement(
        Scene,
        {
          start: 25.7,
          end: 30.8,
        },
        ' ',
        /*#__PURE__*/ React.createElement(SceneFlow, null),
      ),
      /*#__PURE__*/ React.createElement(
        Scene,
        {
          start: 30.7,
          end: 34.8,
        },
        ' ',
        /*#__PURE__*/ React.createElement(SceneBI, null),
      ),
      /*#__PURE__*/ React.createElement(
        Scene,
        {
          start: 34.6,
          end: 38.4,
        },
        ' ',
        /*#__PURE__*/ React.createElement(SceneResolve, null),
      ),
    ),
    /*#__PURE__*/ React.createElement(Atmosphere, null),
    /*#__PURE__*/ React.createElement(Caption, null),
  );
}
/* ── CAD demo shared helpers ─────────────────────────────────────────────── */
function CADdefs() {
  return /*#__PURE__*/ React.createElement(
    'defs',
    null,
    /*#__PURE__*/ React.createElement(
      'linearGradient',
      {
        id: 'cadGlass',
        x1: '0',
        y1: '0',
        x2: '0.25',
        y2: '1',
      },
      /*#__PURE__*/ React.createElement('stop', {
        offset: '0',
        stopColor: '#cfe6ff',
        stopOpacity: '0.22',
      }),
      /*#__PURE__*/ React.createElement('stop', {
        offset: '0.5',
        stopColor: '#8fb6ee',
        stopOpacity: '0.08',
      }),
      /*#__PURE__*/ React.createElement('stop', {
        offset: '1',
        stopColor: '#a6c8ff',
        stopOpacity: '0.16',
      }),
    ),
    /*#__PURE__*/ React.createElement(
      'radialGradient',
      {
        id: 'cadFloor',
        cx: '0.45',
        cy: '0.4',
        r: '0.7',
      },
      /*#__PURE__*/ React.createElement('stop', {
        offset: '0',
        stopColor: hexA(C.indigo, 0.2),
      }),
      /*#__PURE__*/ React.createElement('stop', {
        offset: '1',
        stopColor: hexA(C.indigo, 0),
      }),
    ),
    /*#__PURE__*/ React.createElement(
      'linearGradient',
      {
        id: 'cadAluV',
        x1: '0',
        y1: '0',
        x2: '1',
        y2: '0',
      },
      /*#__PURE__*/ React.createElement('stop', {
        offset: '0',
        stopColor: '#3c4570',
      }),
      /*#__PURE__*/ React.createElement('stop', {
        offset: '0.45',
        stopColor: '#aab4dd',
      }),
      /*#__PURE__*/ React.createElement('stop', {
        offset: '0.6',
        stopColor: '#c8d2f2',
      }),
      /*#__PURE__*/ React.createElement('stop', {
        offset: '1',
        stopColor: '#363e63',
      }),
    ),
    /*#__PURE__*/ React.createElement(
      'linearGradient',
      {
        id: 'cadAluH',
        x1: '0',
        y1: '0',
        x2: '1',
        y2: '0.3',
      },
      /*#__PURE__*/ React.createElement('stop', {
        offset: '0',
        stopColor: '#9aa6d4',
      }),
      /*#__PURE__*/ React.createElement('stop', {
        offset: '0.5',
        stopColor: '#c3cdf0',
      }),
      /*#__PURE__*/ React.createElement('stop', {
        offset: '1',
        stopColor: '#4a527e',
      }),
    ),
  );
}
function dimSeg(A, B, txt, col, op) {
  const mx = (A[0] + B[0]) / 2,
    my = (A[1] + B[1]) / 2,
    tw = txt.length * 8.4 + 22;
  return /*#__PURE__*/ React.createElement(
    'g',
    {
      opacity: op,
    },
    /*#__PURE__*/ React.createElement('line', {
      x1: A[0],
      y1: A[1],
      x2: B[0],
      y2: B[1],
      stroke: hexA(col, 0.7),
      strokeWidth: '1.3',
    }),
    /*#__PURE__*/ React.createElement('circle', {
      cx: A[0],
      cy: A[1],
      r: '2.6',
      fill: col,
    }),
    /*#__PURE__*/ React.createElement('circle', {
      cx: B[0],
      cy: B[1],
      r: '2.6',
      fill: col,
    }),
    /*#__PURE__*/ React.createElement('rect', {
      x: mx - tw / 2,
      y: my - 13,
      width: tw,
      height: '26',
      rx: '6',
      fill: 'rgba(7,10,22,0.93)',
      stroke: hexA(col, 0.4),
    }),
    /*#__PURE__*/ React.createElement(
      'text',
      {
        x: mx,
        y: my + 4,
        fill: '#cdeeff',
        textAnchor: 'middle',
        style: {
          fontFamily: MONO,
          fontSize: 13,
          fontWeight: 500,
        },
      },
      txt,
    ),
  );
}
function leadTag(tx, ty, cx, cy, txt, col, op) {
  const tw = txt.length * 8.1 + 22;
  return /*#__PURE__*/ React.createElement(
    'g',
    {
      opacity: op,
    },
    /*#__PURE__*/ React.createElement('line', {
      x1: cx,
      y1: cy,
      x2: tx,
      y2: ty,
      stroke: hexA(col, 0.55),
      strokeWidth: '1.1',
    }),
    /*#__PURE__*/ React.createElement('circle', {
      cx: tx,
      cy: ty,
      r: '3',
      fill: col,
    }),
    /*#__PURE__*/ React.createElement('rect', {
      x: cx - tw / 2,
      y: cy - 14,
      width: tw,
      height: '28',
      rx: '7',
      fill: 'rgba(9,12,24,0.95)',
      stroke: hexA(col, 0.45),
    }),
    /*#__PURE__*/ React.createElement(
      'text',
      {
        x: cx,
        y: cy + 5,
        fill: hexA(col, 0.98),
        textAnchor: 'middle',
        style: {
          fontFamily: MONO,
          fontSize: 13,
          fontWeight: 500,
        },
      },
      txt,
    ),
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
    Array.from(
      {
        length: n + 1,
      },
      (_, i) => [lerp(a[0], b[0], i / n), lerp(a[1], b[1], i / n)],
    );
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
    {
      pts: sub([Wn, D], [0, D], 3),
      t0: 1.7,
      sh: 0.42,
    },
    {
      pts: sub([Wn, Dn], [Wn, D], 2),
      t0: 2.0,
      sh: 0.54,
    },
    {
      pts: sub([W, Dn], [Wn, Dn], 2),
      t0: 2.2,
      sh: 0.6,
    },
    {
      pts: sub([W, 0], [W, Dn], 2),
      t0: 1.3,
      sh: 0.72,
    },
    {
      pts: sub([0, D], [0, 0], 3),
      t0: 1.0,
      sh: 0.85,
    },
    {
      pts: sub([0, 0], [W, 0], 4),
      t0: 1.7,
      sh: 1.0,
      front: true,
    },
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
  return /*#__PURE__*/ React.createElement(
    'div',
    {
      style: {
        position: 'absolute',
        inset: 0,
        opacity: op,
      },
    },
    /*#__PURE__*/ React.createElement(
      'svg',
      {
        width: '1100',
        height: '720',
        viewBox: '0 0 1100 720',
        style: {
          position: 'absolute',
          left: '50%',
          top: '49%',
          transform: 'translate(-50%,-50%)',
          overflow: 'visible',
        },
      },
      /*#__PURE__*/ React.createElement(CADdefs, null),
      /*#__PURE__*/ React.createElement('path', {
        d: floorD,
        fill: 'url(#cadFloor)',
        opacity: 0.85 * floorP,
      }),
      /*#__PURE__*/ React.createElement('path', {
        d: floorD,
        fill: 'none',
        stroke: hexA(C.cyan, 0.55),
        strokeWidth: '1.6',
        strokeDasharray: '1700',
        strokeDashoffset: 1700 * (1 - floorP),
      }),
      planPts.map((c, i) => {
        const a = pj(c[0], 0, c[1]),
          b = pj(c[0], H * cornerP, c[1]);
        return /*#__PURE__*/ React.createElement('line', {
          key: i,
          x1: a[0],
          y1: a[1],
          x2: b[0],
          y2: b[1],
          stroke: 'url(#cadAluV)',
          strokeWidth: '7',
        });
      }),
      walls.map((w, wi) => {
        const wp = seg(dl, w.t0, w.t0 + 1.7, eo);
        if (wp <= 0.002) return null;
        const h = H * wp;
        return /*#__PURE__*/ React.createElement(
          'g',
          {
            key: wi,
          },
          w.pts.slice(0, -1).map((p, i) => {
            const q = w.pts[i + 1],
              poly = `${PT(p[0], 0, p[1])} ${PT(q[0], 0, q[1])} ${PT(q[0], h, q[1])} ${PT(p[0], h, p[1])}`;
            return /*#__PURE__*/ React.createElement(
              'g',
              {
                key: i,
              },
              /*#__PURE__*/ React.createElement('polygon', {
                points: poly,
                fill: 'url(#cadGlass)',
              }),
              /*#__PURE__*/ React.createElement('polygon', {
                points: poly,
                fill: '#04060e',
                opacity: (1 - w.sh) * 0.42,
              }),
              /*#__PURE__*/ React.createElement('polygon', {
                points: poly,
                fill: 'none',
                stroke: hexA(C.cyanL, 0.5),
                strokeWidth: '1',
              }),
            );
          }),
          w.pts.map((p, i) => {
            const a = pj(p[0], 0, p[1]),
              b = pj(p[0], h, p[1]);
            return /*#__PURE__*/ React.createElement('line', {
              key: 'm' + i,
              x1: a[0],
              y1: a[1],
              x2: b[0],
              y2: b[1],
              stroke: 'url(#cadAluV)',
              strokeWidth: w.front ? 4 : 5,
            });
          }),
          /*#__PURE__*/ React.createElement('polyline', {
            points: w.pts.map((p) => PT(p[0], h, p[1])).join(' '),
            fill: 'none',
            stroke: 'url(#cadAluH)',
            strokeWidth: '6',
            opacity: wp,
          }),
          w.front &&
            (() => {
              const kp = seg(dl, w.t0 + 0.9, w.t0 + 1.9);
              if (kp <= 0.01) return null;
              return /*#__PURE__*/ React.createElement(
                'g',
                {
                  opacity: kp,
                },
                w.pts.slice(0, -1).map((p, i) => {
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
                  return /*#__PURE__*/ React.createElement(
                    'g',
                    {
                      key: 'k' + i,
                    },
                    /*#__PURE__*/ React.createElement('polygon', {
                      points: `${A[0]},${A[1]} ${B[0]},${B[1]} ${Ct[0]},${Ct[1]} ${Dt[0]},${Dt[1]}`,
                      fill: 'none',
                      stroke: 'url(#cadAluH)',
                      strokeWidth: '3.5',
                    }),
                    /*#__PURE__*/ React.createElement('line', {
                      x1: ml[0],
                      y1: ml[1],
                      x2: mr[0],
                      y2: mr[1],
                      stroke: 'url(#cadAluH)',
                      strokeWidth: '3',
                    }),
                    handle &&
                      /*#__PURE__*/ React.createElement('line', {
                        x1: hk1[0],
                        y1: hk1[1],
                        x2: hk2[0],
                        y2: hk2[1],
                        stroke: 'url(#cadAluV)',
                        strokeWidth: '4',
                        strokeLinecap: 'round',
                      }),
                  );
                }),
              );
            })(),
        );
      }),
      /*#__PURE__*/ React.createElement(
        'g',
        {
          opacity: roofP,
          transform: `translate(0 ${(-34 * (1 - roofP)).toFixed(1)})`,
        },
        /*#__PURE__*/ React.createElement('path', {
          d: roofD,
          fill: 'url(#cadGlass)',
          opacity: '0.72',
        }),
        /*#__PURE__*/ React.createElement('path', {
          d: roofD,
          fill: 'none',
          stroke: 'url(#cadAluH)',
          strokeWidth: '6',
        }),
      ),
      dimSeg([p0[0], p0[1] + 40], [pW[0], pW[1] + 40], '5460 mm', C.cyan, dimP),
      dimSeg([p0[0] - 32, p0[1] + 16], [pD[0] - 32, pD[1] + 16], '3600 mm', C.cyan, dimP),
      dimSeg([p0[0] - 46, p0[1]], [pH[0] - 46, pH[1]], '2400 mm', C.cyan, dimP),
      dimSeg([p0[0], p0[1] + 18], [pf[0], pf[1] + 18], '1365 mm', C.cyanL, seg(dl, 3.1, 4.1)),
      leadTag(gl[0], gl[1], gl[0] + 18, gl[1] - 76, '10 mm temperli', C.violet, seg(dl, 3.3, 4.3)),
      leadTag(dk[0], dk[1], dk[0] + 96, dk[1] - 30, 'dikme 60 mm', C.indigoL, seg(dl, 3.5, 4.5)),
    ),
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
  const pts = Array.from(
    {
      length: NSEG + 1,
    },
    (_, k) => arcPt(k / NSEG),
  );
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
  return /*#__PURE__*/ React.createElement(
    'div',
    {
      style: {
        position: 'absolute',
        inset: 0,
        opacity: op,
      },
    },
    /*#__PURE__*/ React.createElement(
      'svg',
      {
        width: '1100',
        height: '720',
        viewBox: '0 0 1100 720',
        style: {
          position: 'absolute',
          left: '50%',
          top: '49%',
          transform: 'translate(-50%,-50%)',
          overflow: 'visible',
        },
      },
      /*#__PURE__*/ React.createElement(CADdefs, null),
      /*#__PURE__*/ React.createElement('path', {
        d: botPath,
        fill: 'none',
        stroke: hexA(C.cyan, 0.5),
        strokeWidth: '1.6',
        opacity: rise,
      }),
      /*#__PURE__*/ React.createElement('path', {
        d: bandD,
        fill: 'url(#cadGlass)',
        opacity: rise,
      }),
      /*#__PURE__*/ React.createElement('path', {
        d: topPath,
        fill: 'none',
        stroke: hexA(C.cyanL, 0.55),
        strokeWidth: '1.4',
        opacity: rise,
      }),
      /*#__PURE__*/ React.createElement('path', {
        d: topPath,
        fill: 'none',
        stroke: 'url(#cadAluH)',
        strokeWidth: '6',
        opacity: rise,
      }),
      /*#__PURE__*/ React.createElement('path', {
        d: botPath,
        fill: 'none',
        stroke: 'url(#cadAluH)',
        strokeWidth: '5',
        opacity: rise,
      }),
      mulI.map((idx, i) => {
        const p = pts[idx],
          a = pj(p[0], 0, p[1]),
          b = pj(p[0], h, p[1]);
        return /*#__PURE__*/ React.createElement('line', {
          key: i,
          x1: a[0],
          y1: a[1],
          x2: b[0],
          y2: b[1],
          stroke: 'url(#cadAluV)',
          strokeWidth: '5',
          opacity: rise,
        });
      }),
      /*#__PURE__*/ React.createElement(
        'g',
        {
          opacity: dimP,
        },
        /*#__PURE__*/ React.createElement('line', {
          x1: cen[0],
          y1: cen[1],
          x2: mid[0],
          y2: mid[1],
          stroke: hexA(C.amber, 0.6),
          strokeWidth: '1.2',
          strokeDasharray: '6 5',
        }),
        /*#__PURE__*/ React.createElement('path', {
          d: `M${cen[0] - 7} ${cen[1]} h14 M${cen[0]} ${cen[1] - 7} v14`,
          stroke: hexA(C.amber, 0.8),
          strokeWidth: '1.4',
        }),
        /*#__PURE__*/ React.createElement('rect', {
          x: (cen[0] + mid[0]) / 2 - 54,
          y: (cen[1] + mid[1]) / 2 - 13,
          width: '108',
          height: '26',
          rx: '6',
          fill: 'rgba(9,12,24,0.94)',
          stroke: hexA(C.amber, 0.45),
        }),
        /*#__PURE__*/ React.createElement(
          'text',
          {
            x: (cen[0] + mid[0]) / 2,
            y: (cen[1] + mid[1]) / 2 + 4,
            fill: '#f8dca6',
            textAnchor: 'middle',
            style: {
              fontFamily: MONO,
              fontSize: 13,
              fontWeight: 500,
            },
          },
          'R 1820 mm',
        ),
      ),
      dimSeg([pH0[0] - 44, pH0[1]], [pHt[0] - 44, pHt[1]], '2400 mm', C.cyan, dimP),
      dimSeg([fa[0], fa[1] + 34], [fb[0], fb[1] + 34], '780 mm', C.cyanL, seg(dl, 2.2, 3.2)),
      leadTag(
        midTop[0],
        midTop[1],
        midTop[0],
        midTop[1] - 92,
        '8 + 8 lamine',
        C.violet,
        seg(dl, 2.4, 3.4),
      ),
    ),
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
    {
      pts: kemer,
      xb: 18,
      label: 'Kemer',
      t0: 0.4,
    },
    {
      pts: ucgen,
      xb: 300,
      label: 'Üçgen / alınlık',
      t0: 1.3,
    },
    {
      pts: yamuk,
      xb: 560,
      label: 'Yamuk',
      t0: 2.2,
    },
  ];
  const dimP = seg(dl, 2.6, 3.8);
  // arched dims anchors
  const ka = pj(18, 0, 0),
    kb = pj(18 + wa, 0, 0),
    kt = pj(18, hs + ra, 0),
    apex = pj(18 + wa / 2, hs + ra, 0);
  return /*#__PURE__*/ React.createElement(
    'div',
    {
      style: {
        position: 'absolute',
        inset: 0,
        opacity: op,
      },
    },
    /*#__PURE__*/ React.createElement(
      'svg',
      {
        width: '1100',
        height: '720',
        viewBox: '0 0 1100 720',
        style: {
          position: 'absolute',
          left: '50%',
          top: '50%',
          transform: 'translate(-50%,-50%)',
          overflow: 'visible',
        },
      },
      /*#__PURE__*/ React.createElement(CADdefs, null),
      shapes.map((s, si) => {
        const sp = seg(dl, s.t0, s.t0 + 1.0, eo);
        if (sp <= 0.002) return null;
        const poly = s.pts.map((p) => PT(p[0] + s.xb, p[1])).join(' ');
        const baseL = pj(s.xb, 0, 0),
          baseR = pj(s.xb + s.pts[1][0], 0, 0);
        const lbl = pj(s.xb + s.pts[1][0] / 2, 0, 0);
        return /*#__PURE__*/ React.createElement(
          'g',
          {
            key: si,
            opacity: sp,
            transform: `translate(0 ${(1 - sp) * 24})`,
          },
          /*#__PURE__*/ React.createElement('line', {
            x1: baseL[0] - 6,
            y1: baseL[1] + 6,
            x2: baseR[0] + 6,
            y2: baseR[1] + 6,
            stroke: hexA(C.cyan, 0.3),
            strokeWidth: '6',
            strokeLinecap: 'round',
            opacity: '0.5',
          }),
          /*#__PURE__*/ React.createElement('polygon', {
            points: poly,
            fill: 'url(#cadGlass)',
          }),
          /*#__PURE__*/ React.createElement('polygon', {
            points: poly,
            fill: 'none',
            stroke: hexA(C.cyanL, 0.65),
            strokeWidth: '1.4',
          }),
          /*#__PURE__*/ React.createElement('polygon', {
            points: poly,
            fill: 'none',
            stroke: 'url(#cadAluH)',
            strokeWidth: '4',
            opacity: '0.5',
          }),
          /*#__PURE__*/ React.createElement(
            'text',
            {
              x: lbl[0],
              y: lbl[1] + 66,
              fill: C.muted,
              textAnchor: 'middle',
              style: {
                fontFamily: INTER,
                fontSize: 16,
                fontWeight: 600,
              },
            },
            s.label,
          ),
        );
      }),
      dimSeg([ka[0], ka[1] + 34], [kb[0], kb[1] + 34], '1000 mm', C.cyan, dimP),
      dimSeg([ka[0] - 40, ka[1]], [kt[0] - 40, kt[1]], '2400 mm', C.cyan, dimP),
      leadTag(
        apex[0],
        apex[1],
        apex[0] + 8,
        apex[1] - 44,
        'kemer R 500',
        C.amber,
        seg(dl, 3.0, 4.0),
      ),
    ),
  );
}
function floorClamp(v) {
  return clamp(v, 0, 1);
}
function CADDemos() {
  return /*#__PURE__*/ React.createElement(
    React.Fragment,
    null,
    /*#__PURE__*/ React.createElement(DemoLSpace, null),
    /*#__PURE__*/ React.createElement(DemoArc, null),
    /*#__PURE__*/ React.createElement(DemoShaped, null),
  );
}
window.CoreAlignHero = CoreAlignHero;
