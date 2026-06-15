import type { Step } from 'react-joyride';
import type { TourTranslate } from '../model/onboarding.types';

export const buildDashboardTour = (t: TourTranslate): Step[] => [
  {
    target: '[data-tour="sidebar-projects"]',
    content: t(
      'Onboarding.Tour.Dashboard.Step1.Content',
      'Projeler buradan. Yeni cam mekan tasarımı oluşturmak için tıkla.',
    ),
    title: t('Onboarding.Tour.Dashboard.Step1.Title', 'Cam Projeleri'),
    placement: 'right',
    skipBeacon: true,
  },
  {
    target: '[data-tour="new-project-button"]',
    content: t(
      'Onboarding.Tour.Dashboard.Step2.Content',
      '4 adımda yeni proje sihirbazı: Kategori → Şablon → Bilgi → Ölçü.',
    ),
    title: t('Onboarding.Tour.Dashboard.Step2.Title', 'Yeni Proje Sihirbazı'),
    placement: 'bottom',
  },
  {
    target: '[data-tour="persona-switch"]',
    content: t(
      'Onboarding.Tour.Dashboard.Step3.Content',
      'Bu butonla Kolay (büyük butonlar) veya Pro (teknik detay) modu seçebilirsin.',
    ),
    title: t('Onboarding.Tour.Dashboard.Step3.Title', 'Persona Modu'),
    placement: 'bottom',
  },
  {
    target: '[data-tour="sidebar-mrp"]',
    content: t(
      'Onboarding.Tour.Dashboard.Step4.Content',
      'Üretim planlama, talepler ve stok projeksiyonları buradan yönetilir.',
    ),
    title: t('Onboarding.Tour.Dashboard.Step4.Title', 'Üretim & Satın Alma'),
    placement: 'right',
  },
];
