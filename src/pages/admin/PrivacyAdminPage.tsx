import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Shield, ListChecks, Clock, FileText } from 'lucide-react';
import { AdminRequestQueue } from '@/features/privacy/ui/AdminRequestQueue';
import { RetentionPolicyEditor } from '@/features/privacy/ui/RetentionPolicyEditor';

type Tab = 'requests' | 'policies' | 'audit';

export const PrivacyAdminPage = () => {
  const { t } = useTranslation();
  const [tab, setTab] = useState<Tab>('requests');

  return (
    <div className="mx-auto max-w-6xl space-y-6 p-4 sm:p-6">
      <header className="flex items-center gap-3">
        <Shield className="text-indigo-600 dark:text-indigo-400" size={20} />
        <div>
          <h1 className="text-xl font-semibold text-slate-900 dark:text-slate-100">
            {t('Privacy.Admin.Title')}
          </h1>
          <p className="text-sm text-slate-500 dark:text-slate-400">
            {t('Privacy.Admin.Subtitle')}
          </p>
        </div>
      </header>

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
    </div>
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
          ? 'inline-flex items-center gap-2 border-b-2 border-indigo-600 px-3 py-2 text-sm font-semibold text-indigo-700 dark:border-indigo-400 dark:text-indigo-300'
          : 'inline-flex items-center gap-2 px-3 py-2 text-sm font-medium text-slate-600 hover:text-slate-900 dark:text-slate-400 dark:hover:text-slate-200'
      }
    >
      {icon}
      {children}
    </button>
  );
};

export default PrivacyAdminPage;
