import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Trash2 } from 'lucide-react';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';
import { Select } from '@/shared/ui/Select/Select';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useReasonCodesQuery } from '@/features/inventory/hooks/useInventoryQueries';
import { useScrapGlassPlate } from '../hooks/useGlassPlateQueries';
import type { GlassPlate, GlassScrapMode } from '../model/glassPlate.types';

interface Props {
  plate: GlassPlate;
  onClose: () => void;
}

const m2 = (mm2: number) => (mm2 / 1_000_000).toFixed(3);

export const ScrapGlassPlateModal = ({ plate, onClose }: Props) => {
  const { t } = useTranslation();
  const reasonsQuery = useReasonCodesQuery('Scrap', true);
  const scrapMutation = useScrapGlassPlate();

  const reasons = reasonsQuery.data?.data ?? [];

  const [mode, setMode] = useState<GlassScrapMode>('Count');
  const [areaMm2, setAreaMm2] = useState('');
  const [reasonCodeId, setReasonCodeId] = useState('');
  const [notes, setNotes] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!reasonCodeId) {
      toast.error(t('GlassPlates.scrapForm.reasonRequired'));
      return;
    }
    if (mode === 'Area') {
      const area = Number(areaMm2);
      if (!(area > 0)) {
        toast.error(t('GlassPlates.scrapForm.areaRequired'));
        return;
      }
      if (area > plate.remainingAreaMm2) {
        toast.error(t('GlassPlates.scrapForm.areaExceeds'));
        return;
      }
    }

    setSubmitting(true);
    const result = await scrapMutation
      .mutateAsync({
        plateId: plate.id,
        mode,
        areaMm2: mode === 'Area' ? Number(areaMm2) : null,
        reasonCodeId,
        notes: notes.trim() || null,
      })
      .catch((err) => {
        toastApiError(err);
        return null;
      });
    setSubmitting(false);

    if (result?.isSuccess) {
      toast.success(t('GlassPlates.scrapForm.scrapped'));
      onClose();
    } else if (result && !result.isSuccess) {
      toast.error(result.errors?.[0] ?? t('GlassPlates.scrapForm.failed'));
    }
  };

  return (
    <Modal
      open={true}
      title={t('GlassPlates.scrapForm.title', { plate: plate.plateNumber })}
      icon={<Trash2 size={18} />}
      onClose={onClose}
      size="md"
      footer={
        <>
          <Button variant="ghost" type="button" onClick={onClose}>
            {t('GlassPlates.actions.cancel')}
          </Button>
          <Button variant="danger" type="submit" form="glass-scrap-form" isLoading={submitting}>
            {t('GlassPlates.scrapForm.confirm')}
          </Button>
        </>
      }
    >
      <form id="glass-scrap-form" onSubmit={submit} className="space-y-3">
        <div className="rounded-lg bg-slate-50 px-3 py-2 text-sm text-slate-600 dark:bg-slate-800/50 dark:text-slate-300">
          {t('GlassPlates.consumeForm.remaining')}: {m2(plate.remainingAreaMm2)} m²
        </div>

        <Select
          label={t('GlassPlates.scrapForm.mode')}
          value={mode}
          onChange={(e) => setMode(e.target.value as GlassScrapMode)}
        >
          <option value="Count">{t('GlassPlates.scrapForm.modeCount')}</option>
          <option value="Area">{t('GlassPlates.scrapForm.modeArea')}</option>
        </Select>

        {mode === 'Area' && (
          <Input
            label={t('GlassPlates.scrapForm.area')}
            type="number"
            min={0}
            step="any"
            value={areaMm2}
            onChange={(e) => setAreaMm2(e.target.value)}
          />
        )}

        <Select
          label={t('GlassPlates.scrapForm.reason')}
          required
          value={reasonCodeId}
          onChange={(e) => setReasonCodeId(e.target.value)}
        >
          <option value="">{t('GlassPlates.scrapForm.selectReason')}</option>
          {reasons.map((r) => (
            <option key={r.id} value={r.id}>
              {r.code} — {r.name}
            </option>
          ))}
        </Select>

        <Input
          label={t('GlassPlates.scrapForm.notes')}
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          maxLength={200}
        />
      </form>
    </Modal>
  );
};
