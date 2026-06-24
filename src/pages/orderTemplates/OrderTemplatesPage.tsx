import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { ClipboardList, Plus, Play, Trash2, Edit2 } from 'lucide-react';
import { formatDateTime } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { ListPageTemplate } from '@/shared/ui/PageTemplate/PageTemplate';
import { Button } from '@/shared/ui/Button/Button';
import { Badge } from '@/shared/ui/Badge/Badge';
import {
  useDeleteOrderTemplateMutation,
  useOrderTemplatesQuery,
  useRunOrderTemplateNowMutation,
} from '@/features/orderTemplates/hooks/useOrderTemplateQueries';
import type { OrderTemplate } from '@/features/orderTemplates/model/orderTemplate.types';
import { safeRequestWithNotify } from '@/shared/lib/safeRequest';

const PAGE_SIZE = 20;

export const OrderTemplatesPage = () => {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const locale = useFormatLocale();
  const [page, setPage] = useState(1);
  const query = useOrderTemplatesQuery({ page, pageSize: PAGE_SIZE });
  const remove = useDeleteOrderTemplateMutation();
  const runNow = useRunOrderTemplateNowMutation();
  const result = query.data?.data;
  const items = result?.items ?? [];

  const onDelete = async (id: string) => {
    const [data] = await safeRequestWithNotify(remove.mutateAsync(id));
    if (data) toast.success(t('OrderTemplates.Form.Deleted'));
  };

  const onRun = async (id: string) => {
    const [data] = await safeRequestWithNotify(runNow.mutateAsync(id));
    if (data) toast.success(t('OrderTemplates.Form.Saved'));
  };

  return (
    <ListPageTemplate
      header={
        <PageHeader
          icon={<ClipboardList size={20} />}
          title={t('OrderTemplates.Title')}
          subtitle={t('OrderTemplates.Subtitle')}
          actions={
            <Button size="sm" onClick={() => navigate('/order-templates/new')}>
              <Plus size={14} />
              {t('OrderTemplates.New')}
            </Button>
          }
        />
      }
      pagination={
        (result?.total ?? 0) > PAGE_SIZE ? (
          <div className="flex justify-end gap-2 text-xs">
            <Button
              type="button"
              variant="outline"
              size="sm"
              disabled={page <= 1}
              onClick={() => setPage((p) => Math.max(1, p - 1))}
            >
              &lt;
            </Button>
            <span className="self-center">
              {page} / {Math.max(1, Math.ceil((result?.total ?? 0) / PAGE_SIZE))}
            </span>
            <Button
              type="button"
              variant="outline"
              size="sm"
              disabled={items.length < PAGE_SIZE}
              onClick={() => setPage((p) => p + 1)}
            >
              &gt;
            </Button>
          </div>
        ) : undefined
      }
    >
      {items.length === 0 ? (
        <div className="rounded-lg border border-slate-200 bg-white p-8 text-center text-sm text-slate-500 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-400">
          {t('OrderTemplates.Empty')}
        </div>
      ) : (
        <div className="overflow-x-auto rounded-lg border border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-900">
          <table className="w-full text-left text-sm">
            <thead className="bg-slate-50 text-xs uppercase tracking-wider text-slate-500 dark:bg-slate-800/50 dark:text-slate-400">
              <tr>
                <Th>{t('OrderTemplates.Columns.Name')}</Th>
                <Th>{t('OrderTemplates.Columns.Frequency')}</Th>
                <Th>{t('OrderTemplates.Columns.NextRun')}</Th>
                <Th>{t('OrderTemplates.Columns.LastRun')}</Th>
                <Th>{t('OrderTemplates.Columns.Active')}</Th>
                <Th className="text-right">{t('OrderTemplates.Columns.Actions')}</Th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
              {items.map((tpl) => (
                <Row
                  key={tpl.id}
                  tpl={tpl}
                  locale={locale}
                  onDelete={() => onDelete(tpl.id)}
                  onRun={() => onRun(tpl.id)}
                />
              ))}
            </tbody>
          </table>
        </div>
      )}
    </ListPageTemplate>
  );
};

const Th = ({ children, className }: { children: React.ReactNode; className?: string }) => (
  <th className={`px-3 py-2 font-semibold ${className ?? ''}`}>{children}</th>
);

const Row = ({
  tpl,
  locale,
  onDelete,
  onRun,
}: {
  tpl: OrderTemplate;
  locale: string;
  onDelete: () => void;
  onRun: () => void;
}) => {
  const { t } = useTranslation();
  return (
    <tr className="hover:bg-slate-50 dark:hover:bg-slate-800/50">
      <td className="px-3 py-2">
        <Link
          to={`/order-templates/${tpl.id}`}
          className="font-medium text-primary-600 hover:underline dark:text-primary-400"
        >
          {tpl.name}
        </Link>
      </td>
      <td className="px-3 py-2 text-xs text-slate-600 dark:text-slate-400">
        {t(`OrderTemplates.Form.FrequencyOptions.${tpl.frequency}`)}
      </td>
      <td className="px-3 py-2 text-xs text-slate-600 dark:text-slate-400">
        {tpl.nextRunAtUtc ? formatDateTime(tpl.nextRunAtUtc, locale) : '—'}
      </td>
      <td className="px-3 py-2 text-xs text-slate-600 dark:text-slate-400">
        {tpl.lastRunAtUtc ? formatDateTime(tpl.lastRunAtUtc, locale) : '—'}
      </td>
      <td className="px-3 py-2">
        <Badge variant={tpl.isActive ? 'success' : 'neutral'}>{tpl.isActive ? '✓' : '—'}</Badge>
      </td>
      <td className="px-3 py-2">
        <div className="flex justify-end gap-1">
          <button
            type="button"
            onClick={onRun}
            className="rounded border border-slate-200 p-1 text-slate-600 hover:bg-slate-100 dark:border-slate-700 dark:text-slate-300 dark:hover:bg-slate-800"
            title={t('OrderTemplates.Form.RunNow')}
          >
            <Play size={14} />
          </button>
          <Link
            to={`/order-templates/${tpl.id}`}
            className="rounded border border-slate-200 p-1 text-slate-600 hover:bg-slate-100 dark:border-slate-700 dark:text-slate-300 dark:hover:bg-slate-800"
          >
            <Edit2 size={14} />
          </Link>
          <button
            type="button"
            onClick={onDelete}
            className="rounded border border-danger-200 p-1 text-danger-600 hover:bg-danger-50 dark:border-danger-800 dark:text-danger-400 dark:hover:bg-danger-900/30"
          >
            <Trash2 size={14} />
          </button>
        </div>
      </td>
    </tr>
  );
};

export default OrderTemplatesPage;
