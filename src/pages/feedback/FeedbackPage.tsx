import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { MessageSquarePlus, Plus } from 'lucide-react';
import { toastApiError } from '@/shared/lib/mutationToast';
import { formatDateTime } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { ListPageTemplate } from '@/shared/ui/PageTemplate/PageTemplate';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { Select } from '@/shared/ui/Select/Select';
import { useAuthStore } from '@/shared/lib/store/authStore';
import {
  useFeedbackListQuery,
  useUpdateFeedbackStatus,
} from '@/features/feedback/hooks/useFeedback';
import { FeedbackFormModal } from '@/features/feedback/ui/FeedbackFormModal';
import { FeedbackThread } from '@/features/feedback/ui/FeedbackThread';
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
  Bug: 'bg-danger-100 text-danger-700 dark:bg-danger-500/20 dark:text-danger-300',
  Feature: 'bg-primary-100 text-primary-700 dark:bg-primary-500/20 dark:text-primary-300',
  Improvement: 'bg-info-100 text-info-700 dark:bg-info-500/20 dark:text-info-300',
  Question: 'bg-warning-100 text-warning-700 dark:bg-warning-500/20 dark:text-warning-300',
  Other: 'bg-slate-200 text-slate-600 dark:bg-slate-700 dark:text-slate-300',
};

const PRIORITY_TONE: Record<string, string> = {
  Low: 'bg-slate-100 text-slate-600 dark:bg-slate-700/40 dark:text-slate-300',
  Medium: 'bg-info-100 text-info-700 dark:bg-info-500/20 dark:text-info-300',
  High: 'bg-warning-100 text-warning-800 dark:bg-warning-500/20 dark:text-warning-300',
  Critical: 'bg-danger-100 text-danger-700 dark:bg-danger-500/20 dark:text-danger-300',
};

const STATUS_TONE: Record<FeedbackStatus, string> = {
  Open: 'bg-success-100 text-success-700 dark:bg-success-500/20 dark:text-success-300',
  InProgress: 'bg-primary-100 text-primary-700 dark:bg-primary-500/20 dark:text-primary-300',
  Resolved: 'bg-teal-100 text-teal-700 dark:bg-teal-500/20 dark:text-teal-300',
  Closed: 'bg-slate-200 text-slate-600 dark:bg-slate-700 dark:text-slate-300',
  Rejected: 'bg-danger-100 text-danger-700 dark:bg-danger-500/20 dark:text-danger-300',
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
  const isPlatformAdmin = useAuthStore((s) => s.user?.roles?.includes('PlatformAdmin') ?? false);

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
    <ListPageTemplate
      header={
        <PageHeader
          icon={<MessageSquarePlus size={20} />}
          title={t('feedback.page.title', { defaultValue: 'Geri Bildirim & Hata Bildirimi' })}
          subtitle={t('feedback.page.subtitle', {
            defaultValue:
              'Hata bildir, yeni özellik öner veya soru sor. Tüm bildirimler buradan takip edilir.',
          })}
          actions={
            <Button size="sm" onClick={() => setModalOpen(true)}>
              <Plus size={14} />
              {t('feedback.page.new', { defaultValue: 'Yeni Bildirim' })}
            </Button>
          }
        />
      }
      toolbar={
        <div className="flex flex-wrap items-center gap-2">
          <Select
            value={typeFilter}
            onChange={(e) => setTypeFilter(e.target.value as FeedbackType | '')}
            className="w-full sm:w-48"
          >
            <option value="">
              {t('feedback.filter.allTypes', { defaultValue: 'Tüm türler' })}
            </option>
            {(Object.keys(TYPE_LABEL) as FeedbackType[]).map((tp) => (
              <option key={tp} value={tp}>
                {TYPE_LABEL[tp]}
              </option>
            ))}
          </Select>
          <Select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value as FeedbackStatus | '')}
            className="w-full sm:w-48"
          >
            <option value="">
              {t('feedback.filter.allStatuses', { defaultValue: 'Tüm durumlar' })}
            </option>
            {STATUS_OPTIONS.map((s) => (
              <option key={s} value={s}>
                {STATUS_LABEL[s]}
              </option>
            ))}
          </Select>
          <span className="ml-auto text-[11px] text-slate-500 dark:text-slate-400">
            {t('feedback.count', { defaultValue: '{{count}} bildirim', count: tickets.length })}
          </span>
        </div>
      }
    >
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
                  <AgeBadge ticket={ticket} />
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
                      <div className="rounded border border-primary-200 bg-primary-50 px-2 py-1.5 text-xs text-primary-800 dark:border-primary-500/30 dark:bg-primary-500/10 dark:text-primary-200">
                        <span className="font-semibold">
                          {t('feedback.adminResponse', { defaultValue: 'Yönetici yanıtı' })}:{' '}
                        </span>
                        {ticket.adminResponse}
                      </div>
                    )}
                    {isAdmin && <AdminStatusEditor ticket={ticket} onApply={changeStatus} />}
                    <FeedbackThread ticketId={ticket.id} canWriteInternal={isPlatformAdmin} />
                  </div>
                )}
              </li>
            ))}
          </ul>
        )}
      </div>

      {modalOpen && <FeedbackFormModal onClose={() => setModalOpen(false)} />}
    </ListPageTemplate>
  );
};

const TERMINAL_STATUSES: FeedbackStatus[] = ['Resolved', 'Closed', 'Rejected'];
const AGE_WARNING_DAYS = 3;
const AGE_DANGER_DAYS = 7;

const AgeBadge = ({ ticket }: { ticket: FeedbackTicket }) => {
  const { t } = useTranslation();
  const days = useMemo(
    () => Math.floor((new Date().getTime() - new Date(ticket.createdAtUtc).getTime()) / 86_400_000),
    [ticket.createdAtUtc],
  );
  if (TERMINAL_STATUSES.includes(ticket.status) || days < AGE_WARNING_DAYS) return null;
  const tone =
    days >= AGE_DANGER_DAYS
      ? 'bg-danger-100 text-danger-700 dark:bg-danger-500/15 dark:text-danger-300'
      : 'bg-warning-100 text-warning-700 dark:bg-warning-500/15 dark:text-warning-300';
  return (
    <span className={`rounded px-1.5 py-0.5 text-[10px] font-semibold ${tone}`}>
      {t('feedback.ageDays', { defaultValue: '{{n}} gündür açık', n: days })}
    </span>
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
  // WHY: the aggregate rejects an illegal transition with a 409, so offering one is a dead end.
  const allowed = ticket.allowedNextStatuses?.length
    ? ticket.allowedNextStatuses
    : STATUS_OPTIONS.filter((s) => s !== ticket.status);
  const [status, setStatus] = useState<FeedbackStatus>(allowed[0] ?? ticket.status);
  const [response, setResponse] = useState(ticket.adminResponse ?? '');

  return (
    <div className="flex flex-wrap items-end gap-2 border-t border-slate-200 pt-3 dark:border-slate-800">
      <Select
        label={t('feedback.status', { defaultValue: 'Durum' })}
        value={status}
        onChange={(e) => setStatus(e.target.value as FeedbackStatus)}
        className="w-full sm:w-48"
      >
        {allowed.map((s) => (
          <option key={s} value={s}>
            {STATUS_LABEL[s]}
          </option>
        ))}
      </Select>
      <Input
        type="text"
        value={response}
        onChange={(e) => setResponse(e.target.value)}
        placeholder={t('feedback.responsePlaceholder', { defaultValue: 'Yanıt (opsiyonel)' })}
        className="min-w-[180px] flex-1"
      />
      <Button type="button" size="sm" onClick={() => onApply(ticket, status, response)}>
        {t('feedback.apply', { defaultValue: 'Uygula' })}
      </Button>
    </div>
  );
};

export default FeedbackPage;
