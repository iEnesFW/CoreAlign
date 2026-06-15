import { useTranslation } from 'react-i18next';
import tr from '@legal/kullanim-kosullari-tr.md?raw';
import en from '@legal/kullanim-kosullari-en.md?raw';
import { LegalLayout } from './LegalLayout';

export const KullanimKosullariPage = () => {
  const { t } = useTranslation();
  return <LegalLayout title={t('legal.kullanimKosullari')} contentTr={tr} contentEn={en} />;
};

export default KullanimKosullariPage;
