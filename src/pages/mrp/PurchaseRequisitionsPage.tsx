import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { CheckCircle2, XCircle, Send, Ban, FileOutput } from 'lucide-react';
import {
  useApprovePurchaseRequisition,
  useCancelPurchaseRequisition,
  useConvertRequisition,
  useCreatePurchaseRequisition,
  usePurchaseRequisitionsQuery,
  useRejectPurchaseRequisition,
  useSubmitPurchaseRequisition,
} from '@/features/mrp/hooks/usePurchaseRequisitions';
import { PurchaseRequisitionForm } from '@/features/mrp/ui/PurchaseRequisitionForm';
import { ConvertRequisitionDialog } from '@/features/mrp/ui/ConvertRequisitionDialog';
import { ReasonPromptDialog } from '@/features/mrp/ui/ReasonPromptDialog';
import type {
  ConvertRequisitionInput,
  CreatePurchaseRequisitionInput,
  PurchaseRequisition,
  PurchaseRequisitionStatus,
} from '@/features/mrp/model/mrp.types';
import { safeRequest } from '@/shared/lib/safeRequest';
import { formatNumber } from '@/shared/lib/format';

const STATUS_FILTERS: (PurchaseRequisitionStatus | 'All')[] = [
  'All',
  'Draft',
  'Submitted',
  'Approved',
  'Rejected',
  'Converted',
  'Cancelled',
];

const statusTone = (status: PurchaseRequisitionStatus) => {
  switch (status) {
    case 'Draft':
      return 'bg-slate-100 text-slate-700 dark:bg-slate-700 dark:text-slate-200';
    case 'Submitted':
      return 'bg-indigo-100 text-indigo-700 dark:bg-indigo-500/20 dark:text-indigo-300';
    case 'Approved':
      return 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/20 dark:text-emerald-300';
    case 'Rejected':
      return 'bg-rose-100 text-rose-700 dark:bg-rose-500/20 dark:text-rose-300';
    case 'Converted':
      return 'bg-sky-100 text-sky-700 dark:bg-sky-500/20 dark:text-sky-300';
    case 'Cancelled':
      return 'bg-slate-200 text-slate-600 dark:bg-slate-700 dark:text-slate-300';
    default:
      return 'bg-slate-100 text-slate-700';
  }
};

type ReasonAction = { kind: 'reject' | 'cancel'; requisition: PurchaseRequisition };

export const PurchaseRequisitionsPage = () => {
  const { t, i18n } = useTranslation();
  const [statusFilter, setStatusFilter] = useState<PurchaseRequisitionStatus | 'All'>('All');
  const [showForm, setShowForm] = useState(false);
  const [convertTarget, setConvertTarget] = useState<PurchaseRequisition | null>(null);
  const [reasonAction, setReasonAction] = useState<ReasonAction | null>(null);
  const list = usePurchaseRequisitionsQuery({
    status: statusFilter === 'All' ? undefined : statusFilter,
    page: 1,
    pageSize: 50,
  });
  const create = useCreatePurchaseRequisition();
  const submit = useSubmitPurchaseRequisition();
  const approve = useApprovePurchaseRequisition();
  const reject = useRejectPurchaseRequisition();
  const cancel = useCancelPurchaseRequisition();
  const convert = useConvertRequisition();

  const requisitions = list.data?.data?.items ?? [];

  const handleCreate = async (input: CreatePurchaseRequisitionInput) => {
    await create.mutateAsync(input);
    setShowForm(false);
  };

  const handleConvert = async (input: ConvertRequisitionInput) => {
    const [, error] = await safeRequest(convert.mutateAsync(input));
    if (!error) setConvertTarget(null);
  };

  const handleReasonConfirm = async (reason: string | null) => {
    if (!reasonAction) return;
    const action =
      reasonAction.kind === 'reject'
        ? reject.mutateAsync({ id: reasonAction.requisition.id, reason })
        : cancel.mutateAsync({ id: reasonAction.requisition.id, reason });
    const [, error] = await safeRequest(action);
    if (!error) setReasonAction(null);
  };

  return (
    <div className="space-y-6 p-4 sm:p-6">
      <header className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold text-slate-800 dark:text-slate-100">
            {t('Mrp.Requisition.PageTitle')}
          </h1>
          <p className="text-sm text-slate-500 dark:text-slate-400">
            {t('Mrp.Requisition.PageSubtitle')}
          </p>
        </div>
        <button
          type="button"
          onClick={() => setShowForm((v) => !v)}
          className="rounded-md bg-indigo-600 px-3 py-2 text-sm font-medium text-white hover:bg-indigo-500"
        >
          {showForm ? t('Common.Cancel') : t('Mrp.Requisition.NewRequisition')}
        </button>
      </header>

      {showForm && (
        <section className="rounded-lg border border-slate-200 bg-white p-4 shadow-sm dark:border-slate-700 dark:bg-slate-900">
          <h2 className="mb-3 text-sm font-semibold text-slate-700 dark:text-slate-200">
            {t('Mrp.Requisition.NewRequisition')}
          </h2>
          <PurchaseRequisitionForm
            onSubmit={handleCreate}
            onCancel={() => setShowForm(false)}
            isSubmitting={create.isPending}
          />
        </section>
      )}

      <nav className="flex flex-wrap gap-2">
        {STATUS_FILTERS.map((s) => (
          <button
            key={s}
            type="button"
            onClick={() => setStatusFilter(s)}
            className={
              statusFilter === s
                ? 'rounded-full bg-indigo-600 px-3 py-1 text-xs font-semibold text-white'
                : 'rounded-full border border-slate-300 px-3 py-1 text-xs text-slate-600 hover:bg-slate-100 dark:border-slate-600 dark:text-slate-300 dark:hover:bg-slate-800'
            }
          >
            {s === 'All' ? t('Common.All') : t(`Mrp.Requisition.Status.${s}`)}
          </button>
        ))}
      </nav>

      {list.isLoading && (
        <p className="text-sm text-slate-500 dark:text-slate-400">{t('Common.Loading')}</p>
      )}

      <div className="space-y-2">
        {requisitions.map((req) => (
          <RequisitionRow
            key={req.id}
            requisition={req}
            locale={i18n.language}
            onSubmit={() => submit.mutate(req.id)}
            onApprove={() => approve.mutate(req.id)}
            onReject={() => setReasonAction({ kind: 'reject', requisition: req })}
            onCancel={() => setReasonAction({ kind: 'cancel', requisition: req })}
            onConvert={() => setConvertTarget(req)}
          />
        ))}
        {!list.isLoading && requisitions.length === 0 && (
          <div className="rounded-lg border border-dashed border-slate-300 bg-white p-6 text-center text-sm text-slate-500 dark:border-slate-700 dark:bg-slate-900">
            {t('Mrp.Requisition.Empty')}
          </div>
        )}
      </div>

      {convertTarget && (
        <ConvertRequisitionDialog
          requisitionId={convertTarget.id}
          requisitionNumber={convertTarget.number}
          defaultVendorId={convertTarget.lines[0]?.preferredSupplierId ?? null}
          isSubmitting={convert.isPending}
          onConfirm={handleConvert}
          onCancel={() => setConvertTarget(null)}
        />
      )}

      {reasonAction && (
        <ReasonPromptDialog
          title={
            reasonAction.kind === 'reject'
              ? t('Mrp.Requisition.RejectTitle', { number: reasonAction.requisition.number })
              : t('Mrp.Requisition.CancelTitle', { number: reasonAction.requisition.number })
          }
          confirmLabel={
            reasonAction.kind === 'reject' ? t('Mrp.Action.Reject') : t('Mrp.Action.Cancel')
          }
          confirmTone={reasonAction.kind === 'reject' ? 'rose' : 'slate'}
          isSubmitting={reject.isPending || cancel.isPending}
          onConfirm={handleReasonConfirm}
          onCancel={() => setReasonAction(null)}
        />
      )}
    </div>
  );
};

interface RowProps {
  requisition: PurchaseRequisition;
  locale: string;
  onSubmit: () => void;
  onApprove: () => void;
  onReject: () => void;
  onCancel: () => void;
  onConvert: () => void;
}

const RequisitionRow = ({
  requisition,
  locale,
  onSubmit,
  onApprove,
  onReject,
  onCancel,
  onConvert,
}: RowProps) => {
  const { t } = useTranslation();
  const tone = statusTone(requisition.status);

  return (
    <article className="rounded-lg border border-slate-200 bg-white p-4 shadow-sm dark:border-slate-700 dark:bg-slate-900">
      <header className="flex flex-wrap items-center justify-between gap-2">
        <div className="flex items-center gap-3">
          <span className="font-mono text-sm font-semibold text-slate-800 dark:text-slate-100">
            {requisition.number}
          </span>
          <span className={`rounded-full px-2 py-0.5 text-xs font-semibold ${tone}`}>
            {t(`Mrp.Requisition.Status.${requisition.status}`)}
          </span>
          <span className="text-xs text-slate-500 dark:text-slate-400">
            {t(`Mrp.Requisition.Reason.${requisition.reason}`)}
          </span>
        </div>
        <div className="flex items-center gap-2">
          {requisition.status === 'Draft' && (
            <ActionButton icon={Send} label={t('Mrp.Action.Submit')} onClick={onSubmit} />
          )}
          {requisition.status === 'Submitted' && (
            <>
              <ActionButton
                icon={CheckCircle2}
                label={t('Mrp.Action.Approve')}
                onClick={onApprove}
                tone="emerald"
              />
              <ActionButton
                icon={XCircle}
                label={t('Mrp.Action.Reject')}
                onClick={onReject}
                tone="rose"
              />
            </>
          )}
          {requisition.status === 'Approved' && (
            <ActionButton
              icon={FileOutput}
              label={t('Mrp.Action.Convert')}
              onClick={onConvert}
              tone="indigo"
            />
          )}
          {requisition.status !== 'Converted' && requisition.status !== 'Cancelled' && (
            <ActionButton
              icon={Ban}
              label={t('Mrp.Action.Cancel')}
              onClick={onCancel}
              tone="slate"
            />
          )}
        </div>
      </header>
      <p className="mt-2 text-xs text-slate-500 dark:text-slate-400">
        {new Date(requisition.requestedAtUtc).toLocaleString(locale)} · {requisition.lines.length}{' '}
        {t('Mrp.Requisition.LineCount')} · {t('Mrp.Requisition.EstimatedTotal')}:{' '}
        {formatNumber(requisition.estimatedTotal, locale)}
      </p>
      {requisition.notes && (
        <p className="mt-1 text-xs italic text-slate-500 dark:text-slate-400">
          {requisition.notes}
        </p>
      )}
    </article>
  );
};

interface ActionButtonProps {
  icon: React.ComponentType<{ className?: string }>;
  label: string;
  onClick: () => void;
  tone?: 'indigo' | 'emerald' | 'rose' | 'slate';
}

const ActionButton = ({ icon: Icon, label, onClick, tone = 'indigo' }: ActionButtonProps) => {
  const palette: Record<NonNullable<ActionButtonProps['tone']>, string> = {
    indigo:
      'border-indigo-300 bg-indigo-50 text-indigo-700 hover:bg-indigo-100 dark:border-indigo-700 dark:bg-indigo-500/10 dark:text-indigo-300',
    emerald:
      'border-emerald-300 bg-emerald-50 text-emerald-700 hover:bg-emerald-100 dark:border-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-300',
    rose: 'border-rose-300 bg-rose-50 text-rose-700 hover:bg-rose-100 dark:border-rose-700 dark:bg-rose-500/10 dark:text-rose-300',
    slate:
      'border-slate-300 bg-white text-slate-700 hover:bg-slate-100 dark:border-slate-600 dark:bg-slate-800 dark:text-slate-200',
  };
  return (
    <button
      type="button"
      onClick={onClick}
      className={`flex items-center gap-1 rounded-md border px-2 py-1 text-xs font-medium ${palette[tone]}`}
    >
      <Icon className="h-3.5 w-3.5" />
      {label}
    </button>
  );
};

export default PurchaseRequisitionsPage;
