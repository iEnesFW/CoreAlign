import { useTranslation } from 'react-i18next';
import tr from '@legal/kvkk-basvuru-formu-tr.md?raw';
import { LegalLayout } from './LegalLayout';

export const KvkkBasvuruFormuPage = () => {
  const { t } = useTranslation();
  return <LegalLayout title={t('legal.kvkkBasvuruFormu')} contentTr={tr} contentEn={tr} />;
};

export default KvkkBasvuruFormuPage;
