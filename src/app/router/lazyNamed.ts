import type { ComponentType } from 'react';
import { lazy } from 'react';

export const lazyNamed = <M extends Record<string, ComponentType<never>>, K extends keyof M>(
  loader: () => Promise<M>,
  name: K,
) => lazy(() => loader().then((m) => ({ default: m[name] as ComponentType<unknown> })));
