import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Scissors } from 'lucide-react';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useConsumeGlassPlate } from '../hooks/useGlassPlateQueries';
import type { GlassPlate } from '../model/glassPlate.types';

interface Props {
  plate: GlassPlate;
  onClose: () => void;
}

const numOrNull = (v: string): number | null => {
  const n = Number(v);
  return v.trim() !== '' && Number.isFinite(n) && n > 0 ? n : null;
};

const m2 = (mm2: number) => (mm2 / 1_000_000).toFixed(3);

export const ConsumeGlassPlateModal = ({ plate, onClose }: Props) => {
  const { t } = useTranslation();
  const consumeMutation = useConsumeGlassPlate();

  const [cutWidthMm, setCutWidthMm] = useState('');
  const [cutHeightMm, setCutHeightMm] = useState('');
  const [pieces, setPieces] = useState('1');
  const [cutAreaMm2, setCutAreaMm2] = useState('');
  const [remnantWidthMm, setRemnantWidthMm] = useState('');
  const [remnantHeightMm, setRemnantHeightMm] = useState('');
  const [remnantPlateNumber, setRemnantPlateNumber] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const w = Number(cutWidthMm);
  const h = Number(cutHeightMm);
  const p = Number(pieces);
  const derivedArea = w > 0 && h > 0 && p > 0 ? w * h * p : 0;
  const effectiveArea = derivedArea > 0 ? derivedArea : Number(cutAreaMm2) || 0;

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!(effectiveArea > 0)) {
      toast.error(t('GlassPlates.consumeForm.areaRequired'));
      return;
    }
    if (effectiveArea > plate.remainingAreaMm2) {
      toast.error(t('GlassPlates.consumeForm.areaExceeds'));
      return;
    }

    setSubmitting(true);
    const result = await consumeMutation
      .mutateAsync({
        plateId: plate.id,
        cutAreaMm2: effectiveArea,
        pieces: p > 0 ? p : 1,
        cutWidthMm: numOrNull(cutWidthMm),
        cutHeightMm: numOrNull(cutHeightMm),
        remnantWidthMm: numOrNull(remnantWidthMm),
        remnantHeightMm: numOrNull(remnantHeightMm),
        remnantPlateNumber: remnantPlateNumber.trim() || null,
      })
      .catch((err) => {
        toastApiError(err);
        return null;
      });
    setSubmitting(false);

    if (result?.isSuccess) {
      const remnant = result.data?.remnantPlateId;
      toast.success(
        remnant
          ? t('GlassPlates.consumeForm.consumedWithRemnant')
          : t('GlassPlates.consumeForm.consumed'),
      );
      onClose();
    } else if (result && !result.isSuccess) {
      toast.error(result.errors?.[0] ?? t('GlassPlates.consumeForm.failed'));
    }
  };

  return (
    <Modal
      open={true}
      title={t('GlassPlates.consumeForm.title', { plate: plate.plateNumber })}
      icon={<Scissors size={18} />}
      onClose={onClose}
      size="lg"
      footer={
        <>
          <Button variant="ghost" type="button" onClick={onClose}>
            {t('GlassPlates.actions.cancel')}
          </Button>
          <Button type="submit" form="glass-consume-form" isLoading={submitting}>
            {t('GlassPlates.actions.save')}
          </Button>
        </>
      }
    >
      <form id="glass-consume-form" onSubmit={submit} className="space-y-3">
        <div className="rounded-lg bg-slate-50 px-3 py-2 text-sm text-slate-600 dark:bg-slate-800/50 dark:text-slate-300">
          {t('GlassPlates.consumeForm.remaining')}: {m2(plate.remainingAreaMm2)} m² ({plate.widthMm}
          ×{plate.heightMm} mm)
        </div>

        <p className="text-xs font-semibold uppercase text-slate-500">
          {t('GlassPlates.consumeForm.cutSection')}
        </p>
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
          <Input
            label={t('GlassPlates.consumeForm.cutWidth')}
            type="number"
            min={0}
            step="any"
            value={cutWidthMm}
            onChange={(e) => setCutWidthMm(e.target.value)}
          />
          <Input
            label={t('GlassPlates.consumeForm.cutHeight')}
            type="number"
            min={0}
            step="any"
            value={cutHeightMm}
            onChange={(e) => setCutHeightMm(e.target.value)}
          />
          <Input
            label={t('GlassPlates.consumeForm.pieces')}
            type="number"
            min={1}
            step="1"
            value={pieces}
            onChange={(e) => setPieces(e.target.value)}
          />
        </div>
        <Input
          label={t('GlassPlates.consumeForm.cutArea')}
          type="number"
          min={0}
          step="any"
          value={derivedArea > 0 ? String(derivedArea) : cutAreaMm2}
          onChange={(e) => setCutAreaMm2(e.target.value)}
          disabled={derivedArea > 0}
        />
        {effectiveArea > 0 && (
          <p className="text-xs text-slate-500">
            {t('GlassPlates.consumeForm.willConsume', { area: m2(effectiveArea) })}
          </p>
        )}

        <p className="text-xs font-semibold uppercase text-slate-500">
          {t('GlassPlates.consumeForm.remnantSection')}
        </p>
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
          <Input
            label={t('GlassPlates.consumeForm.remnantWidth')}
            type="number"
            min={0}
            step="any"
            value={remnantWidthMm}
            onChange={(e) => setRemnantWidthMm(e.target.value)}
          />
          <Input
            label={t('GlassPlates.consumeForm.remnantHeight')}
            type="number"
            min={0}
            step="any"
            value={remnantHeightMm}
            onChange={(e) => setRemnantHeightMm(e.target.value)}
          />
          <Input
            label={t('GlassPlates.consumeForm.remnantPlateNumber')}
            value={remnantPlateNumber}
            onChange={(e) => setRemnantPlateNumber(e.target.value)}
            maxLength={60}
          />
        </div>
      </form>
    </Modal>
  );
};
