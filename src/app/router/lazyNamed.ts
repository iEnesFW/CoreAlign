import type { ComponentType, LazyExoticComponent } from 'react';
import { lazy } from 'react';

export type LazyNamedComponent<P = unknown> = LazyExoticComponent<ComponentType<P>> & {
  preload: () => Promise<unknown>;
};

const cache = new Map<string, Promise<unknown>>();

export const lazyNamed = <M extends Record<string, ComponentType<never>>, K extends keyof M>(
  loader: () => Promise<M>,
  name: K,
): LazyNamedComponent => {
  const key = String(name);
  const memoizedLoader = () => {
    let pending = cache.get(key);
    if (!pending) {
      pending = loader();
      cache.set(key, pending);
    }
    return pending as Promise<M>;
  };

  const Component = lazy(() =>
    memoizedLoader().then((m) => ({ default: m[name] as ComponentType<unknown> })),
  ) as LazyNamedComponent;

  Component.preload = () => memoizedLoader();
  return Component;
};
