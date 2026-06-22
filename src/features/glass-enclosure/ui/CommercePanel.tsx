import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Copy, Factory, Link2, ShoppingCart } from 'lucide-react';
import {
  useConvertToOrderMutation,
  useGenerateShareTokenMutation,
  useNotificationHistoryQuery,
  useReleaseToProductionMutation,
  useShareTokensQuery,
  useWorkOrdersQuery,
} from '../hooks/useGlassProjectQueries';
import { safeRequestWithNotify } from '@/shared/lib/safeRequest';
import type { GlassProjectDto } from '../model/project.types';
import { NotificationHistory } from './NotificationHistory';
import { WorkOrderManager } from './WorkOrderManager';

interface CommercePanelProps {
  project: GlassProjectDto;
}

export function CommercePanel({ project }: CommercePanelProps) {
  const { t, i18n } = useTranslation();
  const shareTokensQuery = useShareTokensQuery(project.id);
  const workOrdersQuery = useWorkOrdersQuery(project.id);
  const generateShareMutation = useGenerateShareTokenMutation();
  const convertMutation = useConvertToOrderMutation();
  const releaseMutation = useReleaseToProductionMutation();
  const notificationsQuery = useNotificationHistoryQuery(project.id);
  const [requestedDate, setRequestedDate] = useState('');

  const tokens = shareTokensQuery.data?.data ?? [];
  const workOrders = workOrdersQuery.data?.data ?? [];
  const dateFormatter = useMemo(
    () => new Intl.DateTimeFormat(i18n.language, { dateStyle: 'short', timeStyle: 'short' }),
    [i18n.language],
  );
  const canShare = project.currentSceneVersion > 0;
  const canConvert =
    project.grandTotal > 0 &&
    (project.status === 'Quoted' || project.status === 'Surveyed' || project.status === 'Draft');
  const canRelease = project.status === 'Confirmed';
  const baseUrl = typeof window !== 'undefined' ? window.location.origin : '';

  const copyShareUrl = async (publicUrl: string) => {
    const fullUrl = `${baseUrl}${publicUrl}`;
    if (navigator.clipboard) await navigator.clipboard.writeText(fullUrl);
  };

  return (
    <section className="space-y-4">
      <section>
        <header className="mb-2 flex items-center justify-between">
          <h3 className="text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">
            {t('GlassEnclosure.Commerce.ShareLinks')}
          </h3>
          <button
            type="button"
            disabled={!canShare || generateShareMutation.isPending}
            onClick={async () => {
              await safeRequestWithNotify(
                generateShareMutation.mutateAsync({ id: project.id, overrideTtlDays: null }),
                { successMessage: t('GlassEnclosure.Commerce.ShareLinkCreated') },
              );
            }}
            className="inline-flex items-center gap-1 rounded-md bg-primary-600 px-2 py-1 text-xs font-medium text-white hover:bg-primary-700 disabled:opacity-50"
          >
            <Link2 size={12} />
            {t('GlassEnclosure.Commerce.CreateShareLink')}
          </button>
        </header>
        {tokens.length === 0 ? (
          <p className="text-xs text-slate-500 dark:text-slate-400">
            {t('GlassEnclosure.Commerce.NoShareLinks')}
          </p>
        ) : (
          <ul className="space-y-1.5">
            {tokens.map((token) => {
              const decided = token.acceptedAtUtc || token.rejectedAtUtc;
              const status = token.acceptedAtUtc
                ? t('GlassEnclosure.Commerce.Accepted')
                : token.rejectedAtUtc
                  ? t('GlassEnclosure.Commerce.Rejected')
                  : t('GlassEnclosure.Commerce.Pending');
              return (
                <li
                  key={token.id}
                  className="rounded border border-slate-200 bg-white p-2 text-xs dark:border-slate-700 dark:bg-slate-800"
                >
                  <div className="flex items-center justify-between">
                    <span
                      className={`rounded px-1.5 py-0.5 text-[10px] font-medium ${
                        token.acceptedAtUtc
                          ? 'bg-success-100 text-success-700 dark:bg-success-950/40 dark:text-success-300'
                          : token.rejectedAtUtc
                            ? 'bg-danger-100 text-danger-700 dark:bg-danger-950/40 dark:text-danger-300'
                            : 'bg-warning-100 text-warning-700 dark:bg-warning-950/40 dark:text-warning-300'
                      }`}
                    >
                      v{token.sceneVersion} · {status}
                    </span>
                    <button
                      type="button"
                      onClick={() => copyShareUrl(token.publicUrl)}
                      className="text-primary-600 hover:underline"
                      title={t('GlassEnclosure.Commerce.CopyUrl')}
                    >
                      <Copy size={12} />
                    </button>
                  </div>
                  <div className="mt-1 truncate text-[10px] text-slate-500 dark:text-slate-400">
                    {token.viewCount} {t('GlassEnclosure.Commerce.Views')} ·{' '}
                    {dateFormatter.format(new Date(token.expiresAtUtc))}
                  </div>
                  {decided && (
                    <div className="mt-1 text-[10px] text-slate-500 dark:text-slate-400">
                      {dateFormatter.format(new Date(decided))}
                    </div>
                  )}
                </li>
              );
            })}
          </ul>
        )}
      </section>

      <section>
        <h3 className="mb-2 text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">
          {t('GlassEnclosure.Commerce.OrderConversion')}
        </h3>
        <button
          type="button"
          disabled={!canConvert || convertMutation.isPending}
          onClick={async () => {
            await safeRequestWithNotify(convertMutation.mutateAsync(project.id), {
              successMessage: t('GlassEnclosure.Commerce.OrderConverted'),
            });
          }}
          className="inline-flex w-full items-center justify-center gap-2 rounded-md bg-success-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-success-700 disabled:opacity-50"
        >
          <ShoppingCart size={14} />
          {t('GlassEnclosure.Commerce.ConvertToOrder')}
        </button>
        {!canConvert && (
          <p className="mt-1 text-[10px] text-slate-500 dark:text-slate-400">
            {t('GlassEnclosure.Commerce.ConvertHint')}
          </p>
        )}
      </section>

      <section>
        <h3 className="mb-2 text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">
          {t('GlassEnclosure.Commerce.Production')}
        </h3>
        <div className="space-y-2">
          <label className="block text-[10px] uppercase tracking-wide text-slate-500 dark:text-slate-400">
            {t('GlassEnclosure.Commerce.RequestedStart')}
          </label>
          <input
            type="datetime-local"
            value={requestedDate}
            onChange={(e) => setRequestedDate(e.target.value)}
            disabled={!canRelease}
            className="w-full rounded border border-slate-300 bg-white px-2 py-1 text-xs dark:border-slate-700 dark:bg-slate-900"
          />
          <button
            type="button"
            disabled={!canRelease || releaseMutation.isPending}
            onClick={async () => {
              const input = {
                requestedStartDateUtc: requestedDate ? new Date(requestedDate).toISOString() : null,
                assignedTeamId: null,
              };
              await safeRequestWithNotify(releaseMutation.mutateAsync({ id: project.id, input }), {
                successMessage: t('GlassEnclosure.Commerce.Released'),
              });
            }}
            className="inline-flex w-full items-center justify-center gap-2 rounded-md bg-primary-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-primary-700 disabled:opacity-50"
          >
            <Factory size={14} />
            {t('GlassEnclosure.Commerce.ReleaseToProduction')}
          </button>
          {!canRelease && (
            <p className="text-[10px] text-slate-500 dark:text-slate-400">
              {t('GlassEnclosure.Commerce.ReleaseHint')}
            </p>
          )}
        </div>

        {workOrders.length > 0 && (
          <div className="mt-2">
            <WorkOrderManager projectId={project.id} />
          </div>
        )}
      </section>

      <section>
        <h3 className="mb-2 text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">
          {t('GlassEnclosure.Notifications.Title')}
        </h3>
        <NotificationHistory
          logs={notificationsQuery.data?.data ?? []}
          isLoading={notificationsQuery.isLoading}
        />
      </section>
    </section>
  );
}
