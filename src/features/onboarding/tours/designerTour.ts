import type { Step } from 'react-joyride';
import type { TourTranslate } from '../model/onboarding.types';

export const buildDesignerTour = (t: TourTranslate): Step[] => [
  {
    target: '[data-tour="designer-canvas"]',
    content: t(
      'Onboarding.Tour.Designer.Step1.Content',
      'Cam mekanını burada tasarlarsın. Cephe ekle, panel böl, donanım seç.',
    ),
    title: t('Onboarding.Tour.Designer.Step1.Title', 'Tasarım Tuvali'),
    placement: 'left',
    skipBeacon: true,
  },
  {
    target: '[data-tour="designer-runs"]',
    content: t(
      'Onboarding.Tour.Designer.Step2.Content',
      'Cepheleri (runs) sol panelden ekle. Her cephenin uzunluğu, açısı ve panel sayısı vardır.',
    ),
    title: t('Onboarding.Tour.Designer.Step2.Title', 'Cepheler'),
    placement: 'right',
  },
  {
    target: '[data-tour="designer-inspector"]',
    content: t(
      'Onboarding.Tour.Designer.Step3.Content',
      'Seçili parçanın detayları sağ panelde. Cam türü, renk ve donanım buradan değişir.',
    ),
    title: t('Onboarding.Tour.Designer.Step3.Title', 'Detay Paneli'),
    placement: 'left',
  },
  {
    target: '[data-tour="designer-bom"]',
    content: t(
      'Onboarding.Tour.Designer.Step4.Content',
      'BOM ve maliyet özeti otomatik hesaplanır. Validasyonu çalıştırıp kesim planı çıkarabilirsin.',
    ),
    title: t('Onboarding.Tour.Designer.Step4.Title', 'BOM ve Maliyet'),
    placement: 'top',
  },
];
