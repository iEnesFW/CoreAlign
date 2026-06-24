import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Shield, ListChecks, Clock, FileText } from 'lucide-react';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { ListPageTemplate } from '@/shared/ui/PageTemplate/PageTemplate';
import { AdminRequestQueue } from '@/features/privacy/ui/AdminRequestQueue';
import { RetentionPolicyEditor } from '@/features/privacy/ui/RetentionPolicyEditor';

type Tab = 'requests' | 'policies' | 'audit';

export const PrivacyAdminPage = () => {
  const { t } = useTranslation();
  const [tab, setTab] = useState<Tab>('requests');

  return (
    <ListPageTemplate
      header={
        <PageHeader
          icon={<Shield size={20} />}
          title={t('Privacy.Admin.Title')}
          subtitle={t('Privacy.Admin.Subtitle')}
        />
      }
    >
      <nav className="flex gap-2 border-b border-slate-200 dark:border-slate-700">
        <TabButton current={tab} value="requests" onClick={setTab} icon={<ListChecks size={14} />}>
          {t('Privacy.Admin.Tab.Requests')}
        </TabButton>
        <TabButton current={tab} value="policies" onClick={setTab} icon={<Clock size={14} />}>
          {t('Privacy.Admin.Tab.Policies')}
        </TabButton>
        <TabButton current={tab} value="audit" onClick={setTab} icon={<FileText size={14} />}>
          {t('Privacy.Admin.Tab.Audit')}
        </TabButton>
      </nav>

      <div>
        {tab === 'requests' && <AdminRequestQueue />}
        {tab === 'policies' && <RetentionPolicyEditor />}
        {tab === 'audit' && (
          <div className="rounded-lg border border-dashed border-slate-300 bg-slate-50 p-8 text-center text-sm text-slate-500 dark:border-slate-700 dark:bg-slate-800/40 dark:text-slate-400">
            {t('Privacy.Admin.Tab.AuditPlaceholder')}
          </div>
        )}
      </div>
    </ListPageTemplate>
  );
};

interface TabButtonProps {
  current: Tab;
  value: Tab;
  onClick: (t: Tab) => void;
  icon: React.ReactNode;
  children: React.ReactNode;
}

const TabButton = ({ current, value, onClick, icon, children }: TabButtonProps) => {
  const active = current === value;
  return (
    <button
      type="button"
      onClick={() => onClick(value)}
      className={
        active
          ? 'inline-flex items-center gap-2 border-b-2 border-primary-600 px-3 py-2 text-sm font-semibold text-primary-700 dark:border-primary-400 dark:text-primary-300'
          : 'inline-flex items-center gap-2 px-3 py-2 text-sm font-medium text-slate-600 hover:text-slate-900 dark:text-slate-400 dark:hover:text-slate-200'
      }
    >
      {icon}
      {children}
    </button>
  );
};

export default PrivacyAdminPage;
