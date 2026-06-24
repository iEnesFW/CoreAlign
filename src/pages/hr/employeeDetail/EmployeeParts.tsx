import { type ReactNode } from 'react';
import { Pencil, Trash2 } from 'lucide-react';

export const Info = ({ label, value, mono }: { label: string; value: string; mono?: boolean }) => (
  <div>
    <span className="text-slate-500">{label}:</span>{' '}
    <span className={mono ? 'font-mono' : undefined}>{value}</span>
  </div>
);

export const Field = ({
  label,
  children,
  full,
}: {
  label: string;
  children: ReactNode;
  full?: boolean;
}) => (
  <div className={full ? 'sm:col-span-2' : undefined}>
    <dt className="text-[10px] font-semibold uppercase text-slate-500">{label}</dt>
    <dd className="text-sm text-slate-900 dark:text-slate-100">{children}</dd>
  </div>
);

export const RowActions = ({
  onEdit,
  onDelete,
  editLabel,
  deleteLabel,
}: {
  onEdit: () => void;
  onDelete: () => void;
  editLabel: string;
  deleteLabel: string;
}) => (
  <div className="inline-flex items-center gap-1">
    <button
      type="button"
      onClick={onEdit}
      className="rounded p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-700 dark:hover:bg-slate-800 dark:hover:text-slate-200"
      title={editLabel}
    >
      <Pencil size={13} />
    </button>
    <button
      type="button"
      onClick={onDelete}
      className="rounded p-1 text-slate-400 hover:bg-danger-50 hover:text-danger-700 dark:hover:bg-danger-500/10"
      title={deleteLabel}
    >
      <Trash2 size={13} />
    </button>
  </div>
);
