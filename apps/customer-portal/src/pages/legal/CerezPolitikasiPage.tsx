import { useTranslation } from 'react-i18next';
import tr from '@legal/cerez-politikasi-tr.md?raw';
import en from '@legal/cerez-politikasi-en.md?raw';
import { LegalLayout } from './LegalLayout';

export const CerezPolitikasiPage = () => {
  const { t } = useTranslation();
  return <LegalLayout title={t('legal.cerezPolitikasi')} contentTr={tr} contentEn={en} />;
};

export default CerezPolitikasiPage;
