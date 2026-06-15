import { useTranslation } from 'react-i18next';
import tr from '@legal/aydinlatma-metni-tr.md?raw';
import en from '@legal/aydinlatma-metni-en.md?raw';
import { LegalLayout } from './LegalLayout';

export const AydinlatmaMetniPage = () => {
  const { t } = useTranslation();
  return <LegalLayout title={t('legal.aydinlatmaMetni')} contentTr={tr} contentEn={en} />;
};

export default AydinlatmaMetniPage;
