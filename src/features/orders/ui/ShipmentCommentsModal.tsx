import { useTranslation } from 'react-i18next';
import { MessageSquare } from 'lucide-react';
import { Modal } from '@/shared/ui/Modal/Modal';
import { CommentsTab } from '@/features/collaboration/ui/CommentsTab';
import type { Shipment } from '../model/order.types';

interface Props {
  shipment: Shipment;
  onClose: () => void;
}

export const ShipmentCommentsModal = ({ shipment, onClose }: Props) => {
  const { t } = useTranslation();

  return (
    <Modal
      open
      title={t('collab.comments.title')}
      subtitle={shipment.shipmentNumber}
      icon={<MessageSquare size={18} />}
      onClose={onClose}
      size="lg"
      bodyClassName="p-3"
    >
      <CommentsTab entityType="Shipment" entityId={shipment.id} />
    </Modal>
  );
};
