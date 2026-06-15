import { useTranslation } from 'react-i18next';
import tr from '@legal/gizlilik-politikasi-tr.md?raw';
import en from '@legal/gizlilik-politikasi-en.md?raw';
import { LegalLayout } from './LegalLayout';

export const GizlilikPolitikasiPage = () => {
  const { t } = useTranslation();
  return <LegalLayout title={t('legal.gizlilikPolitikasi')} contentTr={tr} contentEn={en} />;
};

export default GizlilikPolitikasiPage;
