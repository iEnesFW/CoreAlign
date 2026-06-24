import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ShoppingCart } from 'lucide-react';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { Select } from '@/shared/ui/Select/Select';
import { useVendorsQuery } from '@/features/vendors/hooks/useVendorQueries';
import type { ConvertRequisitionInput } from '../model/mrp.types';

interface Props {
  requisitionId: string;
  requisitionNumber: string;
  defaultVendorId?: string | null;
  isSubmitting?: boolean;
  onConfirm: (input: ConvertRequisitionInput) => void;
  onCancel: () => void;
}

const CURRENCIES = ['TRY', 'USD', 'EUR', 'GBP'];

export const ConvertRequisitionDialog = ({
  requisitionId,
  requisitionNumber,
  defaultVendorId,
  isSubmitting = false,
  onConfirm,
  onCancel,
}: Props) => {
  const { t } = useTranslation();
  const vendors = useVendorsQuery({ page: 1, pageSize: 100 });
  const [vendorId, setVendorId] = useState<string>(defaultVendorId ?? '');
  const [currency, setCurrency] = useState<string>('TRY');
  const [expectedDate, setExpectedDate] = useState<string>('');

  const canSubmit = vendorId.length > 0 && !isSubmitting;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!canSubmit) return;
    onConfirm({
      id: requisitionId,
      vendorId,
      currency,
      expectedDate: expectedDate ? new Date(expectedDate).toISOString() : null,
    });
  };

  return (
    <Modal
      open={true}
      title={t('Mrp.Convert.Title', { number: requisitionNumber })}
      icon={<ShoppingCart size={18} />}
      onClose={onCancel}
      size="md"
      footer={
        <>
          <Button variant="ghost" type="button" onClick={onCancel}>
            {t('Common.Cancel')}
          </Button>
          <Button
            type="submit"
            form="convert-requisition-form"
            isLoading={isSubmitting}
            disabled={!canSubmit}
          >
            {t('Mrp.Action.Convert')}
          </Button>
        </>
      }
    >
      <form id="convert-requisition-form" onSubmit={handleSubmit} className="space-y-3">
        <Select
          label={t('Mrp.Convert.Vendor')}
          value={vendorId}
          onChange={(e) => setVendorId(e.target.value)}
        >
          <option value="">{t('Mrp.Convert.SelectVendor')}</option>
          {(vendors.data?.data?.items ?? []).map((v) => (
            <option key={v.id} value={v.id}>
              {v.name}
            </option>
          ))}
        </Select>
        <div className="grid grid-cols-2 gap-3">
          <Select
            label={t('Mrp.Convert.Currency')}
            value={currency}
            onChange={(e) => setCurrency(e.target.value)}
          >
            {CURRENCIES.map((c) => (
              <option key={c} value={c}>
                {c}
              </option>
            ))}
          </Select>
          <Input
            label={t('Mrp.Convert.ExpectedDate')}
            type="date"
            value={expectedDate}
            onChange={(e) => setExpectedDate(e.target.value)}
          />
        </div>
      </form>
    </Modal>
  );
};
