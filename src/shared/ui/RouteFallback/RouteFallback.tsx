import { Loader2 } from 'lucide-react';

export const RouteFallback = () => (
  <div
    className="flex h-full min-h-[40vh] w-full items-center justify-center"
    role="status"
    aria-live="polite"
  >
    <Loader2 className="size-6 animate-spin text-slate-400 dark:text-slate-500" />
  </div>
);
