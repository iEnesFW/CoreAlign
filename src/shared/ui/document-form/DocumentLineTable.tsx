import type { ReactNode } from 'react';
import { documentSectionWrapperCls } from './documentFormClasses';

export const documentLineHeaderCls =
  'hidden min-w-0 items-center gap-3 border-b border-slate-200 bg-slate-50 px-4 py-3 text-[11px] font-semibold uppercase tracking-wider text-slate-500 lg:grid dark:border-[#2a3143] dark:bg-[#1a1f2c] dark:text-slate-400';

interface Props {
  header: ReactNode;
  headerGridCls: string;
  error?: ReactNode;
  children: ReactNode;
}

export const DocumentLineTable = ({ header, headerGridCls, error, children }: Props) => (
  <div className={`${documentSectionWrapperCls} overflow-visible`}>
    <div className={`${documentLineHeaderCls} ${headerGridCls}`}>{header}</div>
    <div className="divide-y divide-slate-100 dark:divide-[#2a3143]">
      {error && <div className="p-3 text-xs text-danger-500 bg-danger-500/10">{error}</div>}
      {children}
    </div>
  </div>
);
