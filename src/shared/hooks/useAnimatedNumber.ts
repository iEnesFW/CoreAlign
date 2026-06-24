import { useEffect, useRef, useState } from 'react';

const easeOut = (t: number) => 1 - Math.pow(1 - t, 3);

export const useAnimatedNumber = (target: number, durationMs = 700): number => {
  const [value, setValue] = useState(target);
  const fromRef = useRef(target);
  const frameRef = useRef<number | null>(null);

  useEffect(() => {
    const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    if (reducedMotion || target === fromRef.current) {
      frameRef.current = requestAnimationFrame(() => {
        setValue(target);
        fromRef.current = target;
        frameRef.current = null;
      });
      return () => {
        if (frameRef.current !== null) cancelAnimationFrame(frameRef.current);
      };
    }
    const start = performance.now();
    const from = fromRef.current;
    const delta = target - from;
    const step = (now: number) => {
      const elapsed = now - start;
      const t = Math.min(1, elapsed / durationMs);
      setValue(from + delta * easeOut(t));
      if (t < 1) {
        frameRef.current = requestAnimationFrame(step);
      } else {
        fromRef.current = target;
        frameRef.current = null;
      }
    };
    frameRef.current = requestAnimationFrame(step);
    return () => {
      if (frameRef.current !== null) cancelAnimationFrame(frameRef.current);
    };
  }, [target, durationMs]);

  return value;
};
