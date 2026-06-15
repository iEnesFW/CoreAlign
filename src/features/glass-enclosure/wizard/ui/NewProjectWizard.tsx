import { useCallback, useEffect, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useWizardStore, type WizardStep } from '../model/wizardStore';
import { useCreateProjectMutation } from '../hooks/useCreateProjectMutation';
import { WizardShell } from './WizardShell';
import { Step1Category } from './Step1Category';
import { Step2Template } from './Step2Template';
import { Step3ProjectMeta } from './Step3ProjectMeta';
import { Step4QuickDimensions } from './Step4QuickDimensions';

interface NewProjectWizardProps {
  isOpen: boolean;
  onClose: () => void;
}

const NAME_MIN = 3;
const NAME_MAX = 120;

export const NewProjectWizard = ({ isOpen, onClose }: NewProjectWizardProps) => {
  const { t } = useTranslation();
  const navigate = useNavigate();

  const step = useWizardStore((s) => s.step);
  const setStep = useWizardStore((s) => s.setStep);
  const category = useWizardStore((s) => s.category);
  const meta = useWizardStore((s) => s.meta);
  const quickDims = useWizardStore((s) => s.quickDims);
  const templateId = useWizardStore((s) => s.templateId);
  const resetWizard = useWizardStore((s) => s.reset);

  const createMutation = useCreateProjectMutation();

  useEffect(() => {
    return () => {
      resetWizard();
    };
  }, [resetWizard]);

  const handleClose = useCallback(() => {
    resetWizard();
    onClose();
  }, [onClose, resetWizard]);

  const step3Valid = useMemo(() => {
    const trimmed = meta.name.trim();
    if (trimmed.length < NAME_MIN || trimmed.length > NAME_MAX) return false;
    return Boolean(meta.customerId);
  }, [meta.customerId, meta.name]);

  const handleNext = useCallback(() => {
    if (step === 3 && step3Valid) {
      setStep(4);
    }
  }, [setStep, step, step3Valid]);

  const handleStep4Submit = useCallback(
    (skipDimensions: boolean) => {
      if (!category) return;
      createMutation.mutate(
        {
          category,
          templateId,
          meta,
          runLabelPrefix: t('GlassEnclosure.Designer.DefaultRunLabel', { defaultValue: 'Run' }),
          quickDims: skipDimensions
            ? { runs: [], skipped: true }
            : { runs: quickDims.runs, skipped: false },
        },
        {
          onSuccess: (result) => {
            handleClose();
            navigate(`/dashboard/glass-enclosure/projects/${result.projectId}`);
          },
        },
      );
    },
    [category, createMutation, handleClose, meta, navigate, quickDims.runs, t, templateId],
  );

  if (!isOpen) return null;

  const renderStep = (current: WizardStep) => {
    switch (current) {
      case 1:
        return <Step1Category />;
      case 2:
        return <Step2Template />;
      case 3:
        return <Step3ProjectMeta />;
      case 4:
        return (
          <Step4QuickDimensions
            onSubmit={handleStep4Submit}
            isSubmitting={createMutation.isPending}
          />
        );
      default:
        return null;
    }
  };

  const showFooterNext = step === 3;
  const nextLabel = t('GlassEnclosure.NewProjectWizard.Next', { defaultValue: 'İleri' });

  return (
    <WizardShell
      onClose={handleClose}
      onNext={showFooterNext ? handleNext : undefined}
      nextDisabled={step === 3 && !step3Valid}
      nextLabel={nextLabel}
      hideNext={!showFooterNext}
    >
      {renderStep(step)}
    </WizardShell>
  );
};

export default NewProjectWizard;
