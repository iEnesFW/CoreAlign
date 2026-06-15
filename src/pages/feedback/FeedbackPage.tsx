import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { MessageSquarePlus, Plus } from 'lucide-react';
import { toastApiError } from '@/shared/lib/mutationToast';
import { formatDateTime } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { useAuthStore } from '@/features/auth/model/authStore';
import {
  useFeedbackListQuery,
  useUpdateFeedbackStatus,
} from '@/features/feedback/hooks/useFeedback';
import { FeedbackFormModal } from '@/features/feedback/ui/FeedbackFormModal';
import type {
  FeedbackStatus,
  FeedbackTicket,
  FeedbackType,
} from '@/features/feedback/model/feedback.types';

const TYPE_LABEL: Record<FeedbackType, string> = {
  Bug: 'Hata',
  Feature: 'Özellik',
  Improvement: 'İyileştirme',
  Question: 'Soru',
  Other: 'Diğer',
};

const TYPE_TONE: Record<FeedbackType, string> = {
  Bug: 'bg-rose-100 text-rose-700 dark:bg-rose-500/20 dark:text-rose-300',
  Feature: 'bg-indigo-100 text-indigo-700 dark:bg-indigo-500/20 dark:text-indigo-300',
  Improvement: 'bg-sky-100 text-sky-700 dark:bg-sky-500/20 dark:text-sky-300',
  Question: 'bg-amber-100 text-amber-700 dark:bg-amber-500/20 dark:text-amber-300',
  Other: 'bg-slate-200 text-slate-600 dark:bg-slate-700 dark:text-slate-300',
};

const PRIORITY_TONE: Record<string, string> = {
  Low: 'bg-slate-100 text-slate-600 dark:bg-slate-700/40 dark:text-slate-300',
  Medium: 'bg-sky-100 text-sky-700 dark:bg-sky-500/20 dark:text-sky-300',
  High: 'bg-amber-100 text-amber-800 dark:bg-amber-500/20 dark:text-amber-300',
  Critical: 'bg-rose-100 text-rose-700 dark:bg-rose-500/20 dark:text-rose-300',
};

const STATUS_TONE: Record<FeedbackStatus, string> = {
  Open: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/20 dark:text-emerald-300',
  InProgress: 'bg-indigo-100 text-indigo-700 dark:bg-indigo-500/20 dark:text-indigo-300',
  Resolved: 'bg-teal-100 text-teal-700 dark:bg-teal-500/20 dark:text-teal-300',
  Closed: 'bg-slate-200 text-slate-600 dark:bg-slate-700 dark:text-slate-300',
  Rejected: 'bg-rose-100 text-rose-700 dark:bg-rose-500/20 dark:text-rose-300',
};

const STATUS_LABEL: Record<FeedbackStatus, string> = {
  Open: 'Açık',
  InProgress: 'İşlemde',
  Resolved: 'Çözüldü',
  Closed: 'Kapandı',
  Rejected: 'Reddedildi',
};

const STATUS_OPTIONS: FeedbackStatus[] = ['Open', 'InProgress', 'Resolved', 'Closed', 'Rejected'];

const fmtDate = (iso: string, locale: string) => formatDateTime(iso, locale);

export const FeedbackPage = () => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const isAdmin = useAuthStore((s) => s.user?.roles?.includes('TenantAdmin') ?? false);

  const [typeFilter, setTypeFilter] = useState<FeedbackType | ''>('');
  const [statusFilter, setStatusFilter] = useState<FeedbackStatus | ''>('');
  const [modalOpen, setModalOpen] = useState(false);
  const [expandedId, setExpandedId] = useState<string | null>(null);

  const listQuery = useFeedbackListQuery({
    type: typeFilter || undefined,
    status: statusFilter || undefined,
  });
  const updateStatus = useUpdateFeedbackStatus();

  const tickets = useMemo(() => listQuery.data?.data ?? [], [listQuery.data]);

  const changeStatus = async (ticket: FeedbackTicket, status: FeedbackStatus, response: string) => {
    try {
      await updateStatus.mutateAsync({ id: ticket.id, status, adminResponse: response || null });
      toast.success(t('feedback.statusUpdated', { defaultValue: 'Durum güncellendi.' }));
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <div className="space-y-4 p-4">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-xl font-bold text-slate-900 dark:text-slate-100">
            {t('feedback.page.title', { defaultValue: 'Geri Bildirim & Hata Bildirimi' })}
          </h1>
          <p className="mt-0.5 text-sm text-slate-500 dark:text-slate-400">
            {t('feedback.page.subtitle', {
              defaultValue:
                'Hata bildir, yeni özellik öner veya soru sor. Tüm bildirimler buradan takip edilir.',
            })}
          </p>
        </div>
        <button
          type="button"
          onClick={() => setModalOpen(true)}
          className="inline-flex items-center gap-1.5 rounded bg-indigo-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-indigo-700"
        >
          <Plus size={13} />
          {t('feedback.page.new', { defaultValue: 'Yeni Bildirim' })}
        </button>
      </div>

      <div className="flex flex-wrap items-center gap-2">
        <select
          value={typeFilter}
          onChange={(e) => setTypeFilter(e.target.value as FeedbackType | '')}
          className="rounded border border-slate-200 bg-white px-2 py-1 text-xs dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
        >
          <option value="">{t('feedback.filter.allTypes', { defaultValue: 'Tüm türler' })}</option>
          {(Object.keys(TYPE_LABEL) as FeedbackType[]).map((tp) => (
            <option key={tp} value={tp}>
              {TYPE_LABEL[tp]}
            </option>
          ))}
        </select>
        <select
          value={statusFilter}
          onChange={(e) => setStatusFilter(e.target.value as FeedbackStatus | '')}
          className="rounded border border-slate-200 bg-white px-2 py-1 text-xs dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
        >
          <option value="">
            {t('feedback.filter.allStatuses', { defaultValue: 'Tüm durumlar' })}
          </option>
          {STATUS_OPTIONS.map((s) => (
            <option key={s} value={s}>
              {STATUS_LABEL[s]}
            </option>
          ))}
        </select>
        <span className="ml-auto text-[11px] text-slate-500 dark:text-slate-400">
          {t('feedback.count', { defaultValue: '{{count}} bildirim', count: tickets.length })}
        </span>
      </div>

      <div className="overflow-hidden rounded-lg border border-slate-200 dark:border-slate-800">
        {listQuery.isPending ? (
          <div className="px-3 py-8 text-center text-sm text-slate-500">
            {t('common.loading', { defaultValue: 'Yükleniyor…' })}
          </div>
        ) : tickets.length === 0 ? (
          <div className="flex flex-col items-center gap-2 px-3 py-10 text-center text-sm text-slate-500 dark:text-slate-400">
            <MessageSquarePlus size={28} className="text-slate-300 dark:text-slate-600" />
            {t('feedback.empty', { defaultValue: 'Henüz bildirim yok. İlk bildirimini oluştur.' })}
          </div>
        ) : (
          <ul className="divide-y divide-slate-200 dark:divide-slate-800">
            {tickets.map((ticket) => (
              <li key={ticket.id}>
                <button
                  type="button"
                  onClick={() => setExpandedId((id) => (id === ticket.id ? null : ticket.id))}
                  className="flex w-full items-center gap-2 px-3 py-2.5 text-left hover:bg-slate-50/60 dark:hover:bg-slate-800/30"
                >
                  <span
                    className={`rounded px-1.5 py-0.5 text-[10px] font-semibold ${TYPE_TONE[ticket.type]}`}
                  >
                    {TYPE_LABEL[ticket.type]}
                  </span>
                  <span className="min-w-0 flex-1 truncate text-sm font-medium text-slate-800 dark:text-slate-100">
                    {ticket.title}
                  </span>
                  {ticket.module && (
                    <span className="hidden text-[10px] text-slate-400 sm:inline">
                      {ticket.module}
                    </span>
                  )}
                  <span
                    className={`rounded px-1.5 py-0.5 text-[10px] font-semibold ${PRIORITY_TONE[ticket.priority]}`}
                  >
                    {ticket.priority}
                  </span>
                  <span
                    className={`rounded px-1.5 py-0.5 text-[10px] font-semibold ${STATUS_TONE[ticket.status]}`}
                  >
                    {STATUS_LABEL[ticket.status]}
                  </span>
                  <span className="hidden w-28 shrink-0 text-right text-[10px] text-slate-400 md:inline">
                    {fmtDate(ticket.createdAtUtc, locale)}
                  </span>
                </button>

                {expandedId === ticket.id && (
                  <div className="space-y-3 border-t border-slate-100 bg-slate-50/40 px-4 py-3 text-sm dark:border-slate-800 dark:bg-slate-900/30">
                    <p className="whitespace-pre-wrap text-slate-700 dark:text-slate-200">
                      {ticket.description}
                    </p>
                    {ticket.stepsToReproduce && (
                      <div>
                        <div className="text-[11px] font-semibold text-slate-500 dark:text-slate-400">
                          {t('feedback.form.steps', { defaultValue: 'Tekrar Üretme Adımları' })}
                        </div>
                        <p className="whitespace-pre-wrap text-xs text-slate-600 dark:text-slate-300">
                          {ticket.stepsToReproduce}
                        </p>
                      </div>
                    )}
                    <div className="flex flex-wrap gap-x-4 gap-y-1 text-[11px] text-slate-500 dark:text-slate-400">
                      {ticket.pageUrl && <span className="font-mono">{ticket.pageUrl}</span>}
                      {ticket.createdByName && <span>{ticket.createdByName}</span>}
                    </div>
                    {ticket.adminResponse && (
                      <div className="rounded border border-indigo-200 bg-indigo-50 px-2 py-1.5 text-xs text-indigo-800 dark:border-indigo-500/30 dark:bg-indigo-500/10 dark:text-indigo-200">
                        <span className="font-semibold">
                          {t('feedback.adminResponse', { defaultValue: 'Yönetici yanıtı' })}:{' '}
                        </span>
                        {ticket.adminResponse}
                      </div>
                    )}
                    {isAdmin && <AdminStatusEditor ticket={ticket} onApply={changeStatus} />}
                  </div>
                )}
              </li>
            ))}
          </ul>
        )}
      </div>

      {modalOpen && <FeedbackFormModal onClose={() => setModalOpen(false)} />}
    </div>
  );
};

const AdminStatusEditor = ({
  ticket,
  onApply,
}: {
  ticket: FeedbackTicket;
  onApply: (ticket: FeedbackTicket, status: FeedbackStatus, response: string) => void;
}) => {
  const { t } = useTranslation();
  const [status, setStatus] = useState<FeedbackStatus>(ticket.status);
  const [response, setResponse] = useState(ticket.adminResponse ?? '');

  return (
    <div className="flex flex-wrap items-end gap-2 border-t border-slate-200 pt-3 dark:border-slate-800">
      <div>
        <label className="block text-[10px] font-semibold uppercase text-slate-400">
          {t('feedback.status', { defaultValue: 'Durum' })}
        </label>
        <select
          value={status}
          onChange={(e) => setStatus(e.target.value as FeedbackStatus)}
          className="mt-0.5 rounded border border-slate-200 bg-white px-2 py-1 text-xs dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
        >
          {STATUS_OPTIONS.map((s) => (
            <option key={s} value={s}>
              {STATUS_LABEL[s]}
            </option>
          ))}
        </select>
      </div>
      <input
        type="text"
        value={response}
        onChange={(e) => setResponse(e.target.value)}
        placeholder={t('feedback.responsePlaceholder', { defaultValue: 'Yanıt (opsiyonel)' })}
        className="min-w-[180px] flex-1 rounded border border-slate-200 bg-white px-2 py-1 text-xs dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
      />
      <button
        type="button"
        onClick={() => onApply(ticket, status, response)}
        className="rounded bg-indigo-600 px-3 py-1 text-xs font-semibold text-white hover:bg-indigo-700"
      >
        {t('feedback.apply', { defaultValue: 'Uygula' })}
      </button>
    </div>
  );
};

export default FeedbackPage;
