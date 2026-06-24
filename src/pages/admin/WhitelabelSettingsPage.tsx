import { useTranslation } from 'react-i18next';
import { Palette } from 'lucide-react';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { ListPageTemplate } from '@/shared/ui/PageTemplate/PageTemplate';
import { ThemeEditor } from '@/features/whitelabel/ui/ThemeEditor';

export function WhitelabelSettingsPage() {
  const { t } = useTranslation();
  return (
    <ListPageTemplate
      header={
        <PageHeader
          icon={<Palette size={20} />}
          title={t('Whitelabel.page.title')}
          subtitle={t('Whitelabel.page.subtitle')}
        />
      }
    >
      <ThemeEditor />
    </ListPageTemplate>
  );
}

export default WhitelabelSettingsPage;
