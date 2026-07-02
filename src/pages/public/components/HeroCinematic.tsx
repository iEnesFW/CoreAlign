import { useTheme } from '@/app/providers/themeContext';

/**
 * Full-screen cinematic landing hero.
 *
 * Embeds the DC-runtime motion artifact under `public/hero/` in an isolated,
 * theme-aware, full-viewport iframe.
 *   - dark theme  → /hero/hero-dark.html  (corealign-hero.jsx)
 *   - light theme → /hero/hero-light.html (corealign-hero-light.jsx)
 *
 * Full-bleed (negative top margin pulls it under the floating nav) so the
 * animation fills the viewport edge to edge on open.
 *
 * Bottom dissolve: the artifact paints its own background + a perspective
 * "blueprint floor" right down to its bottom edge. To kill the hard horizontal
 * seam where the iframe meets the page content on scroll, we fade the hero's
 * bottom band into the artifact's own base color ON THE PAGE SIDE. The fade is
 * eased + short, so it dissolves only the empty floor strip and never reaches
 * the captions. Theme-aware; the artifact itself stays untouched.
 */
export const HeroCinematic = () => {
  const { theme } = useTheme();
  const isLight = theme === 'light';
  const src = isLight ? '/hero/hero-light.html' : '/hero/hero-dark.html';
  // RGB triplet of the artifact's base bg, so we fade to a *transparent version
  // of the same color* (fading to `transparent` muddies the blend via black).
  const animRgb = isLight ? '238, 242, 250' : '4, 5, 11';

  return (
    <section
      aria-label="CoreAlign — tasarımdan muhasebeye sinematik tanıtım"
      className="relative w-full overflow-hidden"
      style={{ height: '100svh', minHeight: '30rem', marginTop: '-3.5rem' }}
    >
      <iframe
        key={theme}
        src={src}
        title="CoreAlign tanıtım animasyonu"
        loading="eager"
        allow="autoplay"
        className="absolute inset-0 block h-full w-full"
        style={{ border: 0 }}
      />
      {/* Bottom → page dissolve. pointer-events-none + aria-hidden: purely
          decorative, never blocks the iframe. */}
      <div
        aria-hidden="true"
        className="pointer-events-none absolute inset-x-0 bottom-0"
        style={{
          height: '24%',
          background: `linear-gradient(to bottom,
            rgba(${animRgb}, 0) 0%,
            rgba(${animRgb}, 0) 60%,
            rgba(${animRgb}, 0.12) 72%,
            rgba(${animRgb}, 0.42) 84%,
            rgba(${animRgb}, 0.72) 92%,
            rgba(${animRgb}, 1) 100%)`,
        }}
      />
    </section>
  );
};
