import { MessageCircle, X } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { cn } from '@/shared/lib/cn';
import { useAiHelperStore } from '../model/aiHelperStore';

export const AiHelperLauncher = () => {
  const isOpen = useAiHelperStore((state) => state.isOpen);
  const toggle = useAiHelperStore((state) => state.toggle);
  const { t } = useTranslation();

  return (
    <button
      type="button"
      onClick={toggle}
      aria-label={t('AiHelper.Launcher')}
      aria-expanded={isOpen}
      className={cn(
        'fixed bottom-4 right-4 z-40 flex h-14 w-14 items-center justify-center rounded-full',
        'bg-primary-600 text-white shadow-lg shadow-primary-500/30 transition-colors hover:bg-primary-700',
        'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 focus-visible:ring-offset-2',
        'dark:focus-visible:ring-offset-slate-900 sm:bottom-6 sm:right-6',
      )}
    >
      {isOpen ? <X className="h-6 w-6" /> : <MessageCircle className="h-6 w-6" />}
    </button>
  );
};
