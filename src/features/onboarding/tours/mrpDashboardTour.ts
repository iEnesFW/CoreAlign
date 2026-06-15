import type { Step } from 'react-joyride';
import type { TourTranslate } from '../model/onboarding.types';

export const buildMrpDashboardTour = (t: TourTranslate): Step[] => [
  {
    target: '[data-tour="mrp-summary"]',
    content: t(
      'Onboarding.Tour.Mrp.Step1.Content',
      'MRP özeti: kritik kalemler, açık talepler ve stok durumu burada.',
    ),
    title: t('Onboarding.Tour.Mrp.Step1.Title', 'MRP Panosu'),
    placement: 'bottom',
    skipBeacon: true,
  },
  {
    target: '[data-tour="mrp-generate"]',
    content: t(
      'Onboarding.Tour.Mrp.Step2.Content',
      'Sistem onaylı sipariş ve stok seviyelerinden talep önerileri üretir. Çalıştırmak için tıkla.',
    ),
    title: t('Onboarding.Tour.Mrp.Step2.Title', 'Önerileri Üret'),
    placement: 'bottom',
  },
  {
    target: '[data-tour="mrp-candidates"]',
    content: t(
      'Onboarding.Tour.Mrp.Step3.Content',
      'Önerilen kalemleri inceleyip satın alma talebine dönüştürebilirsin.',
    ),
    title: t('Onboarding.Tour.Mrp.Step3.Title', 'Aday Talepler'),
    placement: 'top',
  },
  {
    target: '[data-tour="mrp-requisitions"]',
    content: t(
      'Onboarding.Tour.Mrp.Step4.Content',
      'Tüm talepleri burada görüntüle, onayla veya iptal et.',
    ),
    title: t('Onboarding.Tour.Mrp.Step4.Title', 'Talepler'),
    placement: 'left',
  },
];
