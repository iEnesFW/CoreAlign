import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { CheckCircle2, ExternalLink, Package, Truck, XCircle } from 'lucide-react';
import { toast } from 'sonner';
import { toastApiError } from '@/shared/lib/mutationToast';
import {
  useCancelShipment,
  useDeliverShipment,
  useDispatchShipment,
  usePackShipment,
  usePickShipment,
  useShipmentsByOrderQuery,
} from '../hooks/useOrderQueries';
import type { Order, Shipment, ShipmentStatus } from '../model/order.types';
import { CreateShipmentModal } from './CreateShipmentModal';

interface Props {
  order: Order;
  showCreateModal: boolean;
  onCloseCreateModal: () => void;
}

const STATUS_STYLES: Record<ShipmentStatus, string> = {
  Draft: 'bg-slate-100 text-slate-700 dark:bg-slate-700/40 dark:text-slate-300',
  Picked: 'bg-sky-100 text-sky-700 dark:bg-sky-500/20 dark:text-sky-300',
  Packed: 'bg-violet-100 text-violet-700 dark:bg-violet-500/20 dark:text-violet-300',
  Dispatched: 'bg-amber-100 text-amber-800 dark:bg-amber-500/20 dark:text-amber-300',
  Delivered: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/20 dark:text-emerald-300',
  Cancelled: 'bg-red-100 text-red-700 dark:bg-red-500/20 dark:text-red-300',
  Returned: 'bg-rose-100 text-rose-700 dark:bg-rose-500/20 dark:text-rose-300',
};

const fmtDateTime = (iso: string | null, locale: string) => {
  if (!iso) return '—';
  try {
    return new Intl.DateTimeFormat(locale, { dateStyle: 'short', timeStyle: 'short' }).format(
      new Date(iso),
    );
  } catch {
    return iso;
  }
};

const fmtNumber = (n: number, locale: string) =>
  new Intl.NumberFormat(locale, { minimumFractionDigits: 2, maximumFractionDigits: 4 }).format(n);

export const ShipmentsTab = ({ order, showCreateModal, onCloseCreateModal }: Props) => {
  const { t, i18n } = useTranslation();
  const locale = i18n.language;
  const shipmentsQuery = useShipmentsByOrderQuery(order.id);
  const [dispatchTarget, setDispatchTarget] = useState<Shipment | null>(null);

  const shipments = shipmentsQuery.data?.data ?? [];

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-semibold text-slate-700 dark:text-slate-200">
          {t('orders.shipments.title')}
        </h3>
        <div className="text-[11px] text-slate-500 dark:text-slate-400">
          {shipments.length} {t('orders.shipments.title').toLowerCase()}
        </div>
      </div>

      {shipmentsQuery.isPending ? (
        <div className="px-3 py-6 text-center text-sm text-slate-500">{t('common.loading')}</div>
      ) : shipments.length === 0 ? (
        <div className="rounded border border-dashed border-slate-300 p-6 text-center text-sm text-slate-500 dark:border-slate-700">
          {t('orders.shipments.empty')}
        </div>
      ) : (
        <ul className="space-y-2">
          {shipments.map((s) => (
            <ShipmentCard
              key={s.id}
              shipment={s}
              locale={locale}
              onDispatchClick={() => setDispatchTarget(s)}
            />
          ))}
        </ul>
      )}

      {showCreateModal && <CreateShipmentModal order={order} onClose={onCloseCreateModal} />}
      {dispatchTarget && (
        <DispatchShipmentModal
          shipment={dispatchTarget}
          locale={locale}
          onClose={() => setDispatchTarget(null)}
        />
      )}
    </div>
  );

  function ShipmentCard({
    shipment,
    locale,
    onDispatchClick,
  }: {
    shipment: Shipment;
    locale: string;
    onDispatchClick: () => void;
  }) {
    const pickMutation = usePickShipment();
    const packMutation = usePackShipment();
    const deliverMutation = useDeliverShipment();
    const cancelMutation = useCancelShipment();
    const [showDeliver, setShowDeliver] = useState(false);
    const [receivedBy, setReceivedBy] = useState('');

    const run = async (action: 'pick' | 'pack' | 'deliver' | 'cancel') => {
      try {
        if (action === 'pick') await pickMutation.mutateAsync(shipment.id);
        if (action === 'pack') await packMutation.mutateAsync(shipment.id);
        if (action === 'cancel') await cancelMutation.mutateAsync({ id: shipment.id });
        if (action === 'deliver') {
          await deliverMutation.mutateAsync({ id: shipment.id, receivedBy: receivedBy || null });
          setShowDeliver(false);
          setReceivedBy('');
        }
        toast.success(t('common.success'));
      } catch (err) {
        toastApiError(err);
      }
    };

    return (
      <li className="rounded-lg border border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-900">
        <div className="flex items-start justify-between gap-3">
          <div>
            <div className="flex items-center gap-2">
              <Package size={14} className="text-slate-500" />
              <span className="font-mono text-sm font-semibold text-slate-900 dark:text-slate-100">
                {shipment.shipmentNumber}
              </span>
              <span
                className={`rounded px-1.5 py-0.5 text-[10px] font-semibold ${STATUS_STYLES[shipment.status]}`}
              >
                {t(`orders.shipmentStatus.${shipment.status}` as never)}
              </span>
            </div>
            <div className="mt-1 text-[11px] text-slate-500 dark:text-slate-400">
              {shipment.warehouseName ?? '—'} · {fmtDateTime(shipment.createdDate, locale)}
            </div>
          </div>
          <div className="flex flex-wrap gap-1">
            {shipment.status === 'Draft' && (
              <ActionButton onClick={() => run('pick')} label={t('orders.actions.pickShipment')} />
            )}
            {shipment.status === 'Picked' && (
              <ActionButton onClick={() => run('pack')} label={t('orders.actions.packShipment')} />
            )}
            {shipment.status === 'Packed' && (
              <ActionButton
                onClick={onDispatchClick}
                label={t('orders.actions.dispatchShipment')}
                icon={<Truck size={11} />}
                primary
              />
            )}
            {shipment.status === 'Dispatched' && (
              <ActionButton
                onClick={() => setShowDeliver(true)}
                label={t('orders.actions.deliverShipment')}
                icon={<CheckCircle2 size={11} />}
                primary
              />
            )}
            {(shipment.status === 'Draft' ||
              shipment.status === 'Picked' ||
              shipment.status === 'Packed') && (
              <ActionButton
                onClick={() => run('cancel')}
                label={t('orders.actions.cancelShipment')}
                icon={<XCircle size={11} />}
                danger
              />
            )}
          </div>
        </div>

        <div className="mt-2 grid grid-cols-2 gap-2 text-[11px] sm:grid-cols-4">
          {shipment.carrierName && (
            <InfoChip label={t('orders.shipments.carrier')} value={shipment.carrierName} />
          )}
          {shipment.trackingNumber && (
            <InfoChip
              label={t('orders.shipments.trackingNumber')}
              value={shipment.trackingNumber}
              link={shipment.trackingUrl}
            />
          )}
          {shipment.shippingCost !== null && shipment.shippingCost !== undefined && (
            <InfoChip
              label={t('orders.shipments.shippingCost')}
              value={fmtNumber(shipment.shippingCost, locale)}
            />
          )}
          {shipment.receivedBy && (
            <InfoChip label={t('orders.shipments.receivedBy')} value={shipment.receivedBy} />
          )}
        </div>

        <details className="mt-2">
          <summary className="cursor-pointer text-[11px] font-semibold text-slate-600 dark:text-slate-400">
            {shipment.lines.length} {t('orders.fields.lines').toLowerCase()}
          </summary>
          <ul className="mt-2 space-y-1 text-xs">
            {shipment.lines.map((l) => (
              <li
                key={l.id}
                className="flex items-center justify-between rounded border border-slate-100 bg-slate-50 px-2 py-1 dark:border-slate-800 dark:bg-slate-800/40"
              >
                <span className="text-slate-700 dark:text-slate-200">
                  {l.productSku} · {l.productName}
                </span>
                <span className="font-mono text-slate-600 dark:text-slate-400">
                  {fmtNumber(l.quantity, locale)}
                </span>
              </li>
            ))}
          </ul>
        </details>

        {showDeliver && (
          <div className="mt-2 rounded border border-emerald-200 bg-emerald-50/60 p-2 dark:border-emerald-500/30 dark:bg-emerald-500/10">
            <label className="block text-[11px] font-medium text-slate-700 dark:text-slate-300">
              {t('orders.shipments.receivedBy')}
            </label>
            <input
              value={receivedBy}
              onChange={(e) => setReceivedBy(e.target.value)}
              className="mt-1 w-full rounded border border-slate-200 px-2 py-1 text-xs dark:border-slate-700 dark:bg-slate-900"
            />
            <div className="mt-2 flex justify-end gap-1">
              <button
                type="button"
                onClick={() => setShowDeliver(false)}
                className="rounded px-2 py-1 text-[11px] text-slate-600 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-800"
              >
                {t('common.cancel')}
              </button>
              <button
                type="button"
                onClick={() => run('deliver')}
                className="rounded bg-emerald-600 px-2 py-1 text-[11px] font-medium text-white hover:bg-emerald-700"
              >
                {t('orders.actions.deliverShipment')}
              </button>
            </div>
          </div>
        )}
      </li>
    );
  }
};

interface ActionButtonProps {
  onClick: () => void;
  label: string;
  icon?: React.ReactNode;
  primary?: boolean;
  danger?: boolean;
}

const ActionButton = ({ onClick, label, icon, primary, danger }: ActionButtonProps) => (
  <button
    type="button"
    onClick={onClick}
    className={`inline-flex items-center gap-1 rounded border px-2 py-1 text-[11px] font-medium transition ${
      danger
        ? 'border-red-200 bg-white text-red-700 hover:bg-red-50 dark:border-red-500/30 dark:bg-slate-900 dark:text-red-300 dark:hover:bg-red-500/10'
        : primary
          ? 'border-indigo-300 bg-indigo-50 text-indigo-700 hover:bg-indigo-100 dark:border-indigo-500/40 dark:bg-indigo-500/20 dark:text-indigo-300'
          : 'border-slate-200 bg-white text-slate-700 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800'
    }`}
  >
    {icon}
    {label}
  </button>
);

const InfoChip = ({
  label,
  value,
  link,
}: {
  label: string;
  value: string;
  link?: string | null;
}) => (
  <div className="rounded border border-slate-200 bg-slate-50 px-2 py-1 dark:border-slate-800 dark:bg-slate-800/40">
    <div className="text-[10px] uppercase tracking-wider text-slate-500 dark:text-slate-400">
      {label}
    </div>
    <div className="truncate text-xs font-medium text-slate-800 dark:text-slate-200">
      {link ? (
        <a
          href={link}
          target="_blank"
          rel="noopener noreferrer"
          className="inline-flex items-center gap-1 text-indigo-600 hover:underline dark:text-indigo-400"
        >
          {value} <ExternalLink size={10} />
        </a>
      ) : (
        value
      )}
    </div>
  </div>
);

interface DispatchModalProps {
  shipment: Shipment;
  locale: string;
  onClose: () => void;
}

const DispatchShipmentModal = ({ shipment, onClose }: DispatchModalProps) => {
  const { t } = useTranslation();
  const dispatchMutation = useDispatchShipment();
  const [carrierName, setCarrierName] = useState(shipment.carrierName ?? '');
  const [trackingNumber, setTrackingNumber] = useState(shipment.trackingNumber ?? '');
  const [trackingUrl, setTrackingUrl] = useState(shipment.trackingUrl ?? '');
  const [shippingCost, setShippingCost] = useState(
    shipment.shippingCost !== null && shipment.shippingCost !== undefined
      ? String(shipment.shippingCost)
      : '',
  );

  const handleSubmit = async () => {
    try {
      await dispatchMutation.mutateAsync({
        id: shipment.id,
        carrierName: carrierName || null,
        trackingNumber: trackingNumber || null,
        trackingUrl: trackingUrl || null,
        shippingCost: shippingCost ? Number(shippingCost) : null,
      });
      toast.success(t('orders.actions.dispatchShipment'));
      onClose();
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4"
      onClick={onClose}
      role="presentation"
    >
      <div
        className="w-full max-w-md rounded-lg bg-white p-5 shadow-xl dark:bg-slate-900"
        onClick={(e) => e.stopPropagation()}
      >
        <h3 className="text-sm font-semibold text-slate-900 dark:text-slate-100">
          {t('orders.actions.dispatchShipment')} — {shipment.shipmentNumber}
        </h3>
        <div className="mt-4 space-y-3">
          <Field
            label={t('orders.shipments.carrier')}
            value={carrierName}
            onChange={setCarrierName}
          />
          <Field
            label={t('orders.shipments.trackingNumber')}
            value={trackingNumber}
            onChange={setTrackingNumber}
          />
          <Field
            label={t('orders.shipments.trackingUrl')}
            value={trackingUrl}
            onChange={setTrackingUrl}
          />
          <Field
            label={t('orders.shipments.shippingCost')}
            value={shippingCost}
            onChange={setShippingCost}
            type="number"
            step="0.01"
          />
        </div>
        <div className="mt-4 flex justify-end gap-2">
          <button
            type="button"
            onClick={onClose}
            className="rounded px-3 py-1.5 text-sm text-slate-700 hover:bg-slate-100 dark:text-slate-200 dark:hover:bg-slate-800"
          >
            {t('common.cancel')}
          </button>
          <button
            type="button"
            onClick={handleSubmit}
            disabled={dispatchMutation.isPending}
            className="inline-flex items-center gap-1.5 rounded bg-indigo-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-indigo-700 disabled:opacity-50"
          >
            <Truck size={14} />
            {t('orders.actions.dispatchShipment')}
          </button>
        </div>
      </div>
    </div>
  );
};

const Field = ({
  label,
  value,
  onChange,
  type = 'text',
  step,
}: {
  label: string;
  value: string;
  onChange: (v: string) => void;
  type?: string;
  step?: string;
}) => (
  <div>
    <label className="mb-1 block text-xs font-medium text-slate-700 dark:text-slate-300">
      {label}
    </label>
    <input
      value={value}
      type={type}
      step={step}
      onChange={(e) => onChange(e.target.value)}
      className="w-full rounded border border-slate-200 px-3 py-2 text-sm focus:border-indigo-500 focus:outline-none dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
    />
  </div>
);
