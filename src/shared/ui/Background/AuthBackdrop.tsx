import { useEffect, useRef } from 'react';

/**
 * AuthBackdrop — lightweight 2D-canvas brand animation for the auth surface.
 *
 * Replaces the old three.js login background. No WebGL, no heavy deps: it draws
 * an isometric "glass curtain wall" blueprint (echoing the product domain) with
 * a sweeping sheen, a travelling data pulse, drifting dust and subtle pointer
 * parallax. Theme-aware and fully `prefers-reduced-motion` safe.
 *
 * Render it as the first child of a `position: relative; overflow: hidden`
 * container — it fills the parent (absolute inset-0).
 */
interface AuthBackdropProps {
  theme: 'light' | 'dark';
  className?: string;
}

type Pal = {
  line: string;
  line2: string;
  glass: string;
  sheen: string;
  dot: string;
  accent: string;
  node: string;
};

const PALETTES: Record<'light' | 'dark', Pal> = {
  dark: {
    line: 'rgba(155,185,255,0.55)',
    line2: 'rgba(120,140,250,0.42)',
    glass: 'rgba(150,185,255,0.07)',
    sheen: 'rgba(190,215,255,0.20)',
    dot: 'rgba(180,205,255,0.95)',
    accent: 'rgba(34,211,238,0.85)',
    node: 'rgba(34,211,238,0.90)',
  },
  light: {
    line: 'rgba(70,92,180,0.50)',
    line2: 'rgba(99,102,241,0.38)',
    glass: 'rgba(95,125,215,0.06)',
    sheen: 'rgba(120,150,235,0.16)',
    dot: 'rgba(70,95,185,0.55)',
    accent: 'rgba(14,155,184,0.80)',
    node: 'rgba(14,155,184,0.85)',
  },
};

type Mote = { x: number; y: number; r: number; s: number; ph: number };

function drawScene(
  ctx: CanvasRenderingContext2D,
  W: number,
  H: number,
  t: number,
  px: { x: number; y: number },
  dust: Mote[],
  p: Pal,
  reduce: boolean,
) {
  if (!W || !H) return;
  const tt = reduce ? 0 : t;
  ctx.clearRect(0, 0, W, H);

  // ── isometric glass curtain wall, anchored toward the lower-right ──
  const par = reduce ? { x: 0, y: 0 } : px;
  const ox = W * 0.66 - par.x * 30;
  const oy = H * 0.54 - par.y * 22;
  const u = Math.min(W, H) * 0.052 + 16; // iso cell size
  const cols = 5;
  const rows = 4;
  const iso = (cx: number, cy: number, cz: number): [number, number] => [
    ox + (cx - cz) * u * 0.92,
    oy + ((cx + cz) * 0.5 - cy) * u,
  ];

  // panels (glass fill) + diagonal sheen sweep
  const sweep = ((tt * 0.12) % 1.4) - 0.2;
  ctx.lineWidth = 1;
  for (let c = 0; c < cols; c++) {
    for (let r = 0; r < rows; r++) {
      const a = iso(c, r + 1, 0);
      const b = iso(c + 1, r + 1, 0);
      const d = iso(c + 1, r, 0);
      const e = iso(c, r, 0);
      ctx.beginPath();
      ctx.moveTo(a[0], a[1]);
      ctx.lineTo(b[0], b[1]);
      ctx.lineTo(d[0], d[1]);
      ctx.lineTo(e[0], e[1]);
      ctx.closePath();
      ctx.fillStyle = p.glass;
      ctx.fill();
      const diag = (c + r) / (cols + rows);
      const prox = 1 - Math.min(1, Math.abs(diag - sweep) * 4.5);
      if (prox > 0) {
        ctx.fillStyle = p.sheen;
        ctx.globalAlpha = prox * 0.9;
        ctx.fill();
        ctx.globalAlpha = 1;
      }
      ctx.strokeStyle = p.line2;
      ctx.stroke();
    }
  }

  // mullions (vertical) + transoms (horizontal)
  ctx.lineWidth = 1.6;
  ctx.strokeStyle = p.line;
  for (let c = 0; c <= cols; c++) {
    const s = iso(c, 0, 0);
    const eP = iso(c, rows, 0);
    ctx.beginPath();
    ctx.moveTo(s[0], s[1]);
    ctx.lineTo(eP[0], eP[1]);
    ctx.stroke();
  }
  for (let r = 0; r <= rows; r++) {
    const s = iso(0, r, 0);
    const eP = iso(cols, r, 0);
    ctx.beginPath();
    ctx.moveTo(s[0], s[1]);
    ctx.lineTo(eP[0], eP[1]);
    ctx.stroke();
  }

  // side return wall (depth)
  ctx.lineWidth = 1;
  ctx.strokeStyle = p.line2;
  for (let r = 0; r < rows; r++) {
    const a = iso(cols, r + 1, 0);
    const b = iso(cols, r + 1, 1);
    const d = iso(cols, r, 1);
    const e = iso(cols, r, 0);
    ctx.beginPath();
    ctx.moveTo(a[0], a[1]);
    ctx.lineTo(b[0], b[1]);
    ctx.lineTo(d[0], d[1]);
    ctx.lineTo(e[0], e[1]);
    ctx.closePath();
    ctx.fillStyle = p.glass;
    ctx.fill();
    ctx.stroke();
  }
  const zc0 = iso(cols, 0, 1);
  const zc1 = iso(cols, rows, 1);
  ctx.lineWidth = 1.6;
  ctx.strokeStyle = p.line;
  ctx.beginPath();
  ctx.moveTo(zc0[0], zc0[1]);
  ctx.lineTo(zc1[0], zc1[1]);
  ctx.stroke();

  // base ground line + corner nodes
  ctx.strokeStyle = p.accent;
  ctx.lineWidth = 1.4;
  ctx.globalAlpha = 0.5;
  const g0 = iso(-0.3, rows, 0);
  const g1 = iso(cols + 0.3, rows, 0);
  ctx.beginPath();
  ctx.moveTo(g0[0], g0[1]);
  ctx.lineTo(g1[0], g1[1]);
  ctx.stroke();
  ctx.globalAlpha = 1;
  (
    [
      [0, 0],
      [cols, 0],
      [0, rows],
      [cols, rows],
    ] as const
  ).forEach((n) => {
    const q = iso(n[0], n[1], 0);
    ctx.beginPath();
    ctx.arc(q[0], q[1], 2.6, 0, 6.28);
    ctx.fillStyle = p.node;
    ctx.fill();
  });

  // data pulse travelling along the top rail
  if (!reduce) {
    const fr = (tt * 0.18) % 1;
    const tp = iso(fr * cols, 0, 0);
    ctx.beginPath();
    ctx.arc(tp[0], tp[1], 3.4, 0, 6.28);
    ctx.fillStyle = p.dot;
    ctx.fill();
    ctx.beginPath();
    ctx.arc(tp[0], tp[1], 7, 0, 6.28);
    ctx.fillStyle = p.accent;
    ctx.globalAlpha = 0.18;
    ctx.fill();
    ctx.globalAlpha = 1;
  }

  // drifting dust
  for (let i = 0; i < dust.length; i++) {
    const m = dust[i];
    let y = m.y - tt * 0.012 * m.s;
    y = y - Math.floor(y);
    const x = m.x + Math.sin(tt * m.s + m.ph) * 0.012;
    const sx = (x - Math.floor(x)) * W;
    const sy = y * H;
    ctx.beginPath();
    ctx.arc(sx, sy, m.r, 0, 6.28);
    ctx.fillStyle = p.dot;
    ctx.globalAlpha = 0.1 + 0.1 * Math.sin(tt * 1.3 + m.ph);
    ctx.fill();
    ctx.globalAlpha = 1;
  }
}

export const AuthBackdrop = ({ theme, className }: AuthBackdropProps) => {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const themeRef = useRef(theme);

  useEffect(() => {
    themeRef.current = theme;
  }, [theme]);

  useEffect(() => {
    const cv = canvasRef.current;
    if (!cv) return;
    const ctx = cv.getContext('2d');
    if (!ctx) return;

    const dpr = Math.min(2, window.devicePixelRatio || 1);
    const reduce = window.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false;
    let W = 0;
    let H = 0;
    let raf = 0;
    const px = { x: 0, y: 0 };
    const target = { x: 0, y: 0 };
    const dust: Mote[] = Array.from({ length: 50 }, () => ({
      x: Math.random(),
      y: Math.random(),
      r: Math.random() * 1.7 + 0.5,
      s: Math.random() * 0.45 + 0.18,
      ph: Math.random() * 6.28,
    }));

    const resize = () => {
      const r = cv.getBoundingClientRect();
      W = r.width;
      H = r.height;
      cv.width = Math.max(1, Math.round(W * dpr));
      cv.height = Math.max(1, Math.round(H * dpr));
      ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    };
    resize();
    const ro = new ResizeObserver(resize);
    ro.observe(cv);

    const moveEl: HTMLElement | Window = cv.parentElement ?? window;
    const onMove = (ev: Event) => {
      const e = ev as PointerEvent;
      const r = cv.getBoundingClientRect();
      target.x = (e.clientX - r.left) / Math.max(1, r.width) - 0.5;
      target.y = (e.clientY - r.top) / Math.max(1, r.height) - 0.5;
    };
    moveEl.addEventListener('pointermove', onMove);

    const t0 = performance.now();
    const draw = (now: number) => {
      const t = (now - t0) / 1000;
      px.x += (target.x - px.x) * 0.045;
      px.y += (target.y - px.y) * 0.045;
      drawScene(ctx, W, H, t, px, dust, PALETTES[themeRef.current], reduce);
      raf = requestAnimationFrame(draw);
    };
    raf = requestAnimationFrame(draw);

    return () => {
      cancelAnimationFrame(raf);
      ro.disconnect();
      moveEl.removeEventListener('pointermove', onMove);
    };
  }, []);

  return (
    <canvas
      ref={canvasRef}
      aria-hidden
      className={className}
      style={{ position: 'absolute', inset: 0, width: '100%', height: '100%', display: 'block' }}
    />
  );
};
