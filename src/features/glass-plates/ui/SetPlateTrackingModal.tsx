import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Layers } from 'lucide-react';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useProductsQuery } from '@/features/products/hooks/useProductQueries';
import { ProductPicker } from '@/shared/ui/ProductPicker';
import type { Product } from '@/shared/model/product.types';
import { useSetGlassPlateTracking } from '../hooks/useGlassPlateQueries';

interface Props {
  onClose: () => void;
  product?: Product;
}

const numOrNull = (v: string): number | null => {
  const n = Number(v);
  return v.trim() !== '' && Number.isFinite(n) && n > 0 ? n : null;
};

export const SetPlateTrackingModal = ({ onClose, product }: Props) => {
  const { t } = useTranslation();
  const productsQuery = useProductsQuery({ page: 1, pageSize: 200, isActive: true });
  const setTrackingMutation = useSetGlassPlateTracking();

  const products = productsQuery.data?.data?.items ?? [];

  const [productId, setProductId] = useState(product?.id ?? '');
  const [isPlateTracked, setIsPlateTracked] = useState(product?.isPlateTracked ?? true);
  const [minRemnantAreaMm2, setMinRemnantAreaMm2] = useState(str(product?.minRemnantAreaMm2));
  const [minRemnantWidthMm, setMinRemnantWidthMm] = useState(str(product?.minRemnantWidthMm));
  const [minRemnantHeightMm, setMinRemnantHeightMm] = useState(str(product?.minRemnantHeightMm));
  const [minPlateCount, setMinPlateCount] = useState(str(product?.minPlateCount));
  const [standardWidthMm, setStandardWidthMm] = useState(str(product?.standardWidthMm));
  const [standardHeightMm, setStandardHeightMm] = useState(str(product?.standardHeightMm));
  const [submitting, setSubmitting] = useState(false);

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!productId) {
      toast.error(t('GlassPlates.trackingForm.productRequired'));
      return;
    }

    setSubmitting(true);
    const result = await setTrackingMutation
      .mutateAsync({
        productId,
        isPlateTracked,
        minRemnantAreaMm2: numOrNull(minRemnantAreaMm2),
        minRemnantWidthMm: numOrNull(minRemnantWidthMm),
        minRemnantHeightMm: numOrNull(minRemnantHeightMm),
        minPlateCount: numOrNull(minPlateCount),
        standardWidthMm: numOrNull(standardWidthMm),
        standardHeightMm: numOrNull(standardHeightMm),
      })
      .catch((err) => {
        toastApiError(err);
        return null;
      });
    setSubmitting(false);

    if (result?.isSuccess) {
      toast.success(t('GlassPlates.trackingForm.saved'));
      onClose();
    } else if (result && !result.isSuccess) {
      toast.error(result.errors?.[0] ?? t('GlassPlates.trackingForm.failed'));
    }
  };

  return (
    <Modal
      open={true}
      title={t('GlassPlates.trackingForm.title')}
      icon={<Layers size={18} />}
      onClose={onClose}
      size="lg"
      footer={
        <>
          <Button variant="ghost" type="button" onClick={onClose}>
            {t('GlassPlates.actions.cancel')}
          </Button>
          <Button type="submit" form="glass-tracking-form" isLoading={submitting}>
            {t('GlassPlates.actions.save')}
          </Button>
        </>
      }
    >
      <form id="glass-tracking-form" onSubmit={submit} className="space-y-3">
        <div>
          <label className="mb-1 block text-sm font-medium text-slate-700 dark:text-slate-300">
            {t('GlassPlates.trackingForm.product')}
          </label>
          <ProductPicker
            products={products}
            value={productId}
            disabled={Boolean(product)}
            onSelect={(id) => setProductId(id)}
          />
        </div>

        <label className="flex items-center gap-2 text-sm text-slate-700 dark:text-slate-300">
          <input
            type="checkbox"
            checked={isPlateTracked}
            onChange={(e) => setIsPlateTracked(e.target.checked)}
            className="h-4 w-4 rounded border-slate-300 text-primary-600 focus:ring-primary-500"
          />
          {t('GlassPlates.trackingForm.isPlateTracked')}
        </label>

        <fieldset disabled={!isPlateTracked} className="space-y-3 disabled:opacity-50">
          <p className="text-xs font-semibold uppercase text-slate-500">
            {t('GlassPlates.trackingForm.remnantSection')}
          </p>
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
            <Input
              label={t('GlassPlates.trackingForm.minRemnantArea')}
              type="number"
              min={0}
              step="any"
              value={minRemnantAreaMm2}
              onChange={(e) => setMinRemnantAreaMm2(e.target.value)}
            />
            <Input
              label={t('GlassPlates.trackingForm.minRemnantWidth')}
              type="number"
              min={0}
              step="any"
              value={minRemnantWidthMm}
              onChange={(e) => setMinRemnantWidthMm(e.target.value)}
            />
            <Input
              label={t('GlassPlates.trackingForm.minRemnantHeight')}
              type="number"
              min={0}
              step="any"
              value={minRemnantHeightMm}
              onChange={(e) => setMinRemnantHeightMm(e.target.value)}
            />
          </div>

          <p className="text-xs font-semibold uppercase text-slate-500">
            {t('GlassPlates.trackingForm.stockSection')}
          </p>
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
            <Input
              label={t('GlassPlates.trackingForm.minPlateCount')}
              type="number"
              min={0}
              step="1"
              value={minPlateCount}
              onChange={(e) => setMinPlateCount(e.target.value)}
            />
            <Input
              label={t('GlassPlates.trackingForm.standardWidth')}
              type="number"
              min={0}
              step="any"
              value={standardWidthMm}
              onChange={(e) => setStandardWidthMm(e.target.value)}
            />
            <Input
              label={t('GlassPlates.trackingForm.standardHeight')}
              type="number"
              min={0}
              step="any"
              value={standardHeightMm}
              onChange={(e) => setStandardHeightMm(e.target.value)}
            />
          </div>
        </fieldset>
      </form>
    </Modal>
  );
};

function str(v: number | null | undefined): string {
  return v === null || v === undefined ? '' : String(v);
}
