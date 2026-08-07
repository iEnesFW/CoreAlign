import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { AlarmClock } from 'lucide-react';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { useAuthStore } from '@/shared/lib/store/authStore';
import { useIsTenantAdmin } from '@/shared/lib/auth/useIsTenantAdmin';
import { createUserScopedSlot } from '@/shared/storage/userScopedSlot';
import { useActiveModulesQuery } from '../hooks/useBilling';
import { dismissalKey, expiringSoon, type ExpiringSoonModule } from '../model/expiryWarning';

const dismissalSlot = createUserScopedSlot<string>({
  feature: 'expiryPopup',
  pageKey: 'billing',
  schema: (raw) => (typeof raw === 'string' ? raw : null),
});

const formatDate = (value: string, locale: string): string =>
  new Date(value).toLocaleDateString(locale, { day: '2-digit', month: 'long', year: 'numeric' });

export const ModuleExpiryPopupHost = () => {
  const { t, i18n } = useTranslation();
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const isTenantAdmin = useIsTenantAdmin();
  const enabled = isAuthenticated && isTenantAdmin;

  const query = useActiveModulesQuery({ enabled });
  const [dismissed, setDismissed] = useState<string | null>(() => dismissalSlot.get());

  const { modules, key } = useMemo(() => {
    const list: ExpiringSoonModule[] = enabled
      ? expiringSoon(query.data?.data ?? [], new Date())
      : [];
    return { modules: list, key: list.length > 0 ? dismissalKey(list, new Date()) : null };
  }, [enabled, query.data]);

  if (!enabled || modules.length === 0 || key === null || dismissed === key) {
    return null;
  }

  const close = () => {
    dismissalSlot.set(key);
    setDismissed(key);
  };

  return (
    <Modal
      open
      onClose={close}
      size="md"
      icon={<AlarmClock size={18} />}
      title={t('billing.expiryPopup.title')}
      subtitle={t('billing.expiryPopup.subtitle', { count: modules.length })}
      footer={
        <>
          <Button variant="ghost" onClick={close}>
            {t('billing.expiryPopup.later')}
          </Button>
          <Link to="/dashboard/billing/store" onClick={close}>
            <Button>{t('billing.expiryPopup.renew')}</Button>
          </Link>
        </>
      }
    >
      <ul className="space-y-2">
        {modules.map((m) => (
          <li
            key={m.moduleId}
            className="flex items-center justify-between gap-3 rounded-2xl border border-amber-200/70 bg-amber-50/60 px-4 py-3 dark:border-amber-500/20 dark:bg-amber-500/10"
          >
            <div className="min-w-0">
              <p className="truncate text-sm font-semibold text-slate-800 dark:text-slate-100">
                {m.name}
              </p>
              <p className="text-[11px] text-slate-500 dark:text-slate-400">
                {t('billing.expiryPopup.expiresOn', {
                  date: formatDate(m.endUtc, i18n.language),
                })}
              </p>
            </div>
            <span className="shrink-0 rounded-full bg-amber-500/15 px-3 py-1 text-[11px] font-semibold text-amber-700 dark:text-amber-300">
              {t('billing.expiryPopup.daysLeft', { count: m.daysLeft })}
            </span>
          </li>
        ))}
      </ul>
    </Modal>
  );
};
