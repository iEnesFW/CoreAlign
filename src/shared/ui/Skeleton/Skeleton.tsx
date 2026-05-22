import { cn } from '@/shared/lib/cn';

interface SkeletonProps {
  className?: string;
}

export const Skeleton = ({ className }: SkeletonProps) => (
  <div className={cn('ca-skeleton rounded-md', className)} />
);

interface TableSkeletonProps {
  rows?: number;
  columns?: number;
  className?: string;
}

export const TableSkeleton = ({ rows = 8, columns = 5, className }: TableSkeletonProps) => (
  <div
    className={cn(
      'overflow-hidden rounded-xl border border-slate-200/70 bg-white dark:border-slate-800/70 dark:bg-slate-900',
      className,
    )}
  >
    <div className="border-b border-slate-200/70 bg-slate-50/60 px-3 py-2 dark:border-slate-800/70 dark:bg-slate-900/40">
      <div className="flex gap-3">
        {Array.from({ length: columns }).map((_, i) => (
          <Skeleton key={i} className="h-3 flex-1" />
        ))}
      </div>
    </div>
    <div className="divide-y divide-slate-200/60 dark:divide-slate-800/60">
      {Array.from({ length: rows }).map((_, i) => (
        <div key={i} className="flex gap-3 px-3 py-2.5">
          {Array.from({ length: columns }).map((__, j) => (
            <Skeleton key={j} className={cn('h-3', j === 0 ? 'w-1/4' : 'flex-1')} />
          ))}
        </div>
      ))}
    </div>
  </div>
);

interface StatStripSkeletonProps {
  count?: number;
  className?: string;
}

export const StatStripSkeleton = ({ count = 4, className }: StatStripSkeletonProps) => (
  <div className={cn('grid grid-cols-2 gap-2 sm:gap-3 lg:grid-cols-4', className)}>
    {Array.from({ length: count }).map((_, i) => (
      <div
        key={i}
        className="rounded-xl border border-slate-200/70 bg-white/80 p-3 dark:border-slate-800/70 dark:bg-slate-900/60"
      >
        <Skeleton className="h-2.5 w-20" />
        <Skeleton className="mt-2 h-5 w-24" />
        <Skeleton className="mt-2 h-2 w-12" />
      </div>
    ))}
  </div>
);
