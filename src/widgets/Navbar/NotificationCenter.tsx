import { useEffect, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import { Bell, BellOff } from 'lucide-react';
import { useTranslation } from 'react-i18next';

export const NotificationCenter = () => {
  const { t } = useTranslation();
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const onClickOutside = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false);
    };
    document.addEventListener('mousedown', onClickOutside);
    return () => document.removeEventListener('mousedown', onClickOutside);
  }, []);

  return (
    <div className="relative" ref={ref}>
      <button
        onClick={() => setOpen((v) => !v)}
        aria-label={t('notifications.title', { defaultValue: 'Notifications' })}
        aria-expanded={open}
        className="p-1.5 text-slate-500 hover:text-slate-700 dark:text-slate-400 dark:hover:text-slate-200 rounded-[5px] hover:bg-slate-100 dark:hover:bg-slate-800 transition-all relative focus:outline-none focus:ring-1 focus:ring-indigo-500"
      >
        <Bell size={16} />
      </button>

      {open && (
        <div className="absolute right-0 mt-2 w-72 rounded-lg bg-white shadow-lg ring-1 ring-slate-200 dark:bg-slate-800 dark:ring-slate-700">
          <div className="flex items-center justify-between border-b border-slate-100 px-3 py-2 dark:border-slate-700/60">
            <span className="text-xs font-semibold text-slate-900 dark:text-slate-100">
              {t('notifications.title', { defaultValue: 'Notifications' })}
            </span>
          </div>

          <div className="flex flex-col items-center gap-1 px-3 py-8 text-center">
            <BellOff size={20} className="text-slate-300 dark:text-slate-600" />
            <p className="text-xs text-slate-500 dark:text-slate-400">
              {t('notifications.empty', { defaultValue: 'You’re all caught up' })}
            </p>
          </div>

          <div className="border-t border-slate-100 px-3 py-2 dark:border-slate-700/60">
            <Link
              to="/dashboard/activity"
              onClick={() => setOpen(false)}
              className="block text-center text-[11px] font-medium text-indigo-600 hover:text-indigo-700 dark:text-indigo-400"
            >
              {t('notifications.viewAll', { defaultValue: 'View all activity' })}
            </Link>
          </div>
        </div>
      )}
    </div>
  );
};
