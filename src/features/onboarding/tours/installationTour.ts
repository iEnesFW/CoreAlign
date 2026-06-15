import type { Step } from 'react-joyride';
import type { TourTranslate } from '../model/onboarding.types';

export const buildInstallationTour = (t: TourTranslate): Step[] => [
  {
    target: '[data-tour="checklist-section"]',
    content: t(
      'Onboarding.Tour.Installation.Step1.Content',
      'Montaj kabul kontrol listesi. Her madde için Geçti/Geçmedi/N/A işaretle.',
    ),
    title: t('Onboarding.Tour.Installation.Step1.Title', 'Kontrol Listesi'),
    placement: 'bottom',
    skipBeacon: true,
  },
  {
    target: '[data-tour="photo-capture"]',
    content: t(
      'Onboarding.Tour.Installation.Step2.Content',
      'Sahadan fotoğraf yükle. Hata varsa görselle belgelendir.',
    ),
    title: t('Onboarding.Tour.Installation.Step2.Title', 'Fotoğraf Çek'),
    placement: 'bottom',
  },
  {
    target: '[data-tour="punch-list"]',
    content: t(
      'Onboarding.Tour.Installation.Step3.Content',
      'Eksik veya hatalı parçaları punch listesine ekle, ekibin gidermesi için işaretle.',
    ),
    title: t('Onboarding.Tour.Installation.Step3.Title', 'Punch List'),
    placement: 'top',
  },
  {
    target: '[data-tour="signature-pad"]',
    content: t(
      'Onboarding.Tour.Installation.Step4.Content',
      'Müşteri imzasını tabletten al. İmza sonrası kabul tutanağı kilitlenir.',
    ),
    title: t('Onboarding.Tour.Installation.Step4.Title', 'Müşteri İmzası'),
    placement: 'top',
  },
];
