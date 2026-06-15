import { useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { X } from 'lucide-react';
import { CommentsTab } from '@/features/collaboration/ui/CommentsTab';
import type { Shipment } from '../model/order.types';

interface Props {
  shipment: Shipment;
  onClose: () => void;
}

/**
 * Lightweight modal that hosts the shared CommentsTab for a single shipment.
 * Shipments don't have a full detail panel today; this is the smallest
 * affordance that lets users converse on a shipment row.
 */
export const ShipmentCommentsModal = ({ shipment, onClose }: Props) => {
  const { t } = useTranslation();

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose();
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [onClose]);

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-labelledby="shipment-comments-title"
      className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 p-4 backdrop-blur-sm"
      onClick={onClose}
    >
      <div
        className="flex w-full max-w-lg flex-col rounded-xl bg-white shadow-2xl ring-1 ring-slate-200 dark:bg-slate-900 dark:ring-slate-700"
        onClick={(e) => e.stopPropagation()}
      >
        <header className="flex items-center justify-between border-b border-slate-100 px-4 py-2.5 dark:border-slate-800">
          <div>
            <h2
              id="shipment-comments-title"
              className="text-sm font-semibold text-slate-900 dark:text-slate-100"
            >
              {t('collab.comments.title')}
            </h2>
            <p className="font-mono text-[11px] text-slate-500 dark:text-slate-400">
              {shipment.shipmentNumber}
            </p>
          </div>
          <button
            type="button"
            onClick={onClose}
            aria-label={t('common.close')}
            className="rounded p-1.5 text-slate-400 hover:bg-slate-100 hover:text-slate-700 dark:hover:bg-slate-800 dark:hover:text-slate-200"
          >
            <X size={16} />
          </button>
        </header>
        <div className="max-h-[70vh] overflow-y-auto p-3">
          <CommentsTab entityType="Shipment" entityId={shipment.id} />
        </div>
      </div>
    </div>
  );
};
