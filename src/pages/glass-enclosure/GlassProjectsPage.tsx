import { useMemo, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Plus, Search } from 'lucide-react';
import { useGlassProjectsQuery } from '@/features/glass-enclosure/hooks/useGlassProjectQueries';
import type { GlassProjectStatus } from '@/features/glass-enclosure/model/project.types';

const STATUS_VALUES: GlassProjectStatus[] = [
  'Draft',
  'Surveyed',
  'Quoted',
  'Confirmed',
  'InProduction',
  'Ready',
  'Installed',
  'Cancelled',
];

const STATUS_BADGE: Record<GlassProjectStatus, string> = {
  Draft: 'bg-slate-200 text-slate-700 dark:bg-slate-700 dark:text-slate-200',
  Surveyed: 'bg-cyan-100 text-cyan-700 dark:bg-cyan-900/40 dark:text-cyan-300',
  Quoted: 'bg-primary-100 text-primary-700 dark:bg-primary-900/40 dark:text-primary-300',
  Confirmed: 'bg-violet-100 text-violet-700 dark:bg-violet-900/40 dark:text-violet-300',
  InProduction: 'bg-warning-100 text-warning-700 dark:bg-warning-900/40 dark:text-warning-300',
  Ready: 'bg-teal-100 text-teal-700 dark:bg-teal-900/40 dark:text-teal-300',
  InTransit: 'bg-warning-100 text-warning-700 dark:bg-warning-900/40 dark:text-warning-300',
  Installed: 'bg-success-100 text-success-700 dark:bg-success-900/40 dark:text-success-300',
  Defective: 'bg-danger-100 text-danger-700 dark:bg-danger-900/40 dark:text-danger-300',
  Cancelled: 'bg-slate-100 text-slate-500 line-through dark:bg-slate-800',
};

export function GlassProjectsPage() {
  const { t, i18n } = useTranslation();
  const navigate = useNavigate();
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState<GlassProjectStatus | ''>('');
  const [page, setPage] = useState(1);
  const pageSize = 20;

  const params = useMemo(
    () => ({
      search: search || undefined,
      status: status || undefined,
      page,
      pageSize,
    }),
    [search, status, page],
  );

  const { data, isLoading } = useGlassProjectsQuery(params);
  const items = data?.data?.items ?? [];
  const total = data?.data?.total ?? 0;
  const totalPages = Math.max(1, Math.ceil(total / pageSize));
  const dateFormatter = new Intl.DateTimeFormat(i18n.language, {
    dateStyle: 'short',
    timeStyle: 'short',
  });

  return (
    <div className="flex h-full flex-col gap-4 p-4 sm:p-6">
      <header className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold text-slate-900 dark:text-slate-100">
            {t('GlassEnclosure.Projects.Title')}
          </h1>
          <p className="text-sm text-slate-500 dark:text-slate-400">
            {t('GlassEnclosure.Projects.Subtitle')}
          </p>
        </div>
        <button
          type="button"
          onClick={() => navigate('/dashboard/glass-enclosure/projects/new')}
          data-tour="new-project-button"
          className="inline-flex items-center gap-1.5 rounded-md bg-primary-600 px-3 py-2 text-sm font-medium text-white hover:bg-primary-700"
        >
          <Plus size={16} /> {t('GlassEnclosure.Projects.New')}
        </button>
      </header>

      <div className="flex flex-wrap items-center gap-2">
        <div className="relative flex-1 min-w-[220px]">
          <Search
            size={16}
            className="pointer-events-none absolute left-2 top-1/2 -translate-y-1/2 text-slate-400"
          />
          <input
            type="text"
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setPage(1);
            }}
            placeholder={t('GlassEnclosure.Projects.SearchPlaceholder')}
            className="w-full rounded-md border border-slate-300 bg-white pl-8 pr-3 py-2 text-sm dark:border-slate-700 dark:bg-slate-900"
          />
        </div>
        <select
          value={status}
          onChange={(e) => {
            setStatus(e.target.value as GlassProjectStatus | '');
            setPage(1);
          }}
          className="rounded-md border border-slate-300 bg-white px-3 py-2 text-sm dark:border-slate-700 dark:bg-slate-900"
        >
          <option value="">{t('GlassEnclosure.Status.All')}</option>
          {STATUS_VALUES.map((s) => (
            <option key={s} value={s}>
              {t(`GlassEnclosure.Status.${s}` as never)}
            </option>
          ))}
        </select>
      </div>

      <div className="flex-1 overflow-auto rounded-lg border border-slate-200 bg-white shadow-sm dark:border-slate-700 dark:bg-slate-800">
        <table className="min-w-full divide-y divide-slate-200 dark:divide-slate-700">
          <thead className="bg-slate-50 dark:bg-slate-900/50">
            <tr>
              <Th>{t('GlassEnclosure.Field.Code')}</Th>
              <Th>{t('GlassEnclosure.Projects.ProjectName')}</Th>
              <Th>{t('GlassEnclosure.Projects.Customer')}</Th>
              <Th>{t('GlassEnclosure.Projects.Status')}</Th>
              <Th>{t('GlassEnclosure.Projects.Panels')}</Th>
              <Th>{t('GlassEnclosure.Projects.Area')}</Th>
              <Th>{t('GlassEnclosure.Projects.GrandTotal')}</Th>
              <Th>{t('GlassEnclosure.Projects.Updated')}</Th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-200 dark:divide-slate-700">
            {isLoading && (
              <tr>
                <td colSpan={8} className="p-6 text-center text-sm text-slate-500">
                  {t('Common.Loading')}
                </td>
              </tr>
            )}
            {!isLoading && items.length === 0 && (
              <tr>
                <td colSpan={8} className="p-6 text-center text-sm text-slate-500">
                  {t('GlassEnclosure.Projects.Empty')}
                </td>
              </tr>
            )}
            {items.map((item) => (
              <tr
                key={item.id}
                className="cursor-pointer hover:bg-slate-50 dark:hover:bg-slate-900/30"
                onClick={() => navigate(`/dashboard/glass-enclosure/projects/${item.id}`)}
              >
                <Td>
                  <Link
                    to={`/dashboard/glass-enclosure/projects/${item.id}`}
                    className="font-mono text-primary-600 hover:underline"
                    onClick={(e) => e.stopPropagation()}
                  >
                    {item.code}
                  </Link>
                </Td>
                <Td>{item.projectName}</Td>
                <Td>{item.customerName ?? '—'}</Td>
                <Td>
                  <span
                    className={`rounded px-2 py-0.5 text-xs font-medium ${STATUS_BADGE[item.status]}`}
                  >
                    {t(`GlassEnclosure.Status.${item.status}` as never)}
                  </span>
                </Td>
                <Td>{item.totalPanels}</Td>
                <Td>{item.totalAreaM2.toFixed(2)} m²</Td>
                <Td>{`${item.grandTotal.toFixed(2)} ${item.currency}`}</Td>
                <Td>{dateFormatter.format(new Date(item.updatedAtUtc))}</Td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="flex items-center justify-between text-sm text-slate-600 dark:text-slate-400">
        <span>
          {t('Common.PageOf', {
            current: page,
            total: totalPages,
            defaultValue: `${page}/${totalPages}`,
          })}
        </span>
        <div className="flex gap-2">
          <button
            type="button"
            onClick={() => setPage((p) => Math.max(1, p - 1))}
            disabled={page <= 1}
            className="rounded border border-slate-300 px-2 py-1 text-sm disabled:opacity-50 dark:border-slate-700"
          >
            {t('Common.Previous')}
          </button>
          <button
            type="button"
            onClick={() => setPage((p) => p + 1)}
            disabled={page >= totalPages}
            className="rounded border border-slate-300 px-2 py-1 text-sm disabled:opacity-50 dark:border-slate-700"
          >
            {t('Common.Next')}
          </button>
        </div>
      </div>
    </div>
  );
}

export default GlassProjectsPage;

const Th = ({ children }: { children: React.ReactNode }) => (
  <th className="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-slate-500 dark:text-slate-400">
    {children}
  </th>
);
const Td = ({ children }: { children: React.ReactNode }) => (
  <td className="px-4 py-3 text-sm text-slate-700 dark:text-slate-300">{children}</td>
);
