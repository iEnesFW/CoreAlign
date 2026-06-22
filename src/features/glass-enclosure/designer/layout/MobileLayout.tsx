import { useState, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { ArrowLeft, Boxes, ClipboardList, FileSpreadsheet, SlidersHorizontal } from 'lucide-react';
import { cn } from '@/shared/lib/cn';
import { useDesignerUxMode, DESIGNER_SCREEN_KEY } from '@/shared/lib/persona';
import { ScreenPersonaMenu } from '@/features/persona/ui/ScreenPersonaMenu';
import { DesignerTabBar, type DesignerTabItem, type DesignerTabKey } from './DesignerTabBar';

interface MobileLayoutProps {
  headerTitle: string;
  headerSubtitle?: string;
  headerRight?: ReactNode;
  toolbarSlot?: ReactNode;
  onBack?: () => void;
  runsSlot: ReactNode;
  canvasSlot: ReactNode;
  inspectorSlot: ReactNode;
  bomSlot: ReactNode;
  activeTab: DesignerTabKey;
  onActiveTabChange: (tab: DesignerTabKey) => void;
}

export const MobileLayout = ({
  headerTitle,
  headerSubtitle,
  headerRight,
  toolbarSlot,
  onBack,
  runsSlot,
  canvasSlot,
  inspectorSlot,
  bomSlot,
  activeTab,
  onActiveTabChange,
}: MobileLayoutProps) => {
  const { t } = useTranslation();
  const mode = useDesignerUxMode();
  const [drawerOpen, setDrawerOpen] = useState(activeTab === 'inspector');
  const [lastActiveTab, setLastActiveTab] = useState(activeTab);
  if (lastActiveTab !== activeTab) {
    setLastActiveTab(activeTab);
    setDrawerOpen(activeTab === 'inspector');
  }

  const tabs: DesignerTabItem[] = [
    {
      key: 'runs',
      icon: <ClipboardList size={18} />,
      label: t(`GlassEnclosure.Designer.Shell.Tab.Runs.${mode}`, { defaultValue: 'Runs' }),
      emoji: '📋',
    },
    {
      key: 'canvas',
      icon: <Boxes size={18} />,
      label: t(`GlassEnclosure.Designer.Shell.Tab.Canvas.${mode}`, { defaultValue: 'Canvas' }),
      emoji: '🪟',
    },
    {
      key: 'inspector',
      icon: <SlidersHorizontal size={18} />,
      label: t(`GlassEnclosure.Designer.Shell.Tab.Inspector.${mode}`, {
        defaultValue: 'Inspector',
      }),
      emoji: '🔧',
    },
    {
      key: 'bom',
      icon: <FileSpreadsheet size={18} />,
      label: t(`GlassEnclosure.Designer.Shell.Tab.BOM.${mode}`, { defaultValue: 'BOM' }),
      emoji: '📊',
    },
  ];

  const handleTabSelect = (key: DesignerTabKey) => {
    onActiveTabChange(key);
  };

  const handleDrawerClose = () => {
    setDrawerOpen(false);
    if (activeTab === 'inspector') onActiveTabChange('canvas');
  };

  return (
    <div className="flex h-full flex-col bg-slate-50 dark:bg-slate-950" role="application">
      <header className="sticky top-0 z-30 flex items-center gap-2 border-b border-slate-200 bg-white px-3 py-2 dark:border-slate-700 dark:bg-slate-900">
        {onBack && (
          <button
            type="button"
            onClick={onBack}
            className="rounded-md p-1.5 text-slate-500 hover:bg-slate-100 dark:text-slate-400 dark:hover:bg-slate-800"
            aria-label={t('GlassEnclosure.Designer.Shell.Back', { defaultValue: 'Back' })}
          >
            <ArrowLeft size={18} />
          </button>
        )}
        <div className="min-w-0 flex-1">
          <h1 className="truncate text-sm font-semibold text-slate-900 dark:text-slate-100">
            {headerTitle}
          </h1>
          {headerSubtitle && (
            <p className="truncate text-[11px] text-slate-500 dark:text-slate-400">
              {headerSubtitle}
            </p>
          )}
        </div>
        {headerRight && (
          <div className="ml-1 flex shrink-0 items-center text-[11px] text-slate-500 dark:text-slate-400">
            {headerRight}
          </div>
        )}
        <ScreenPersonaMenu screenKey={DESIGNER_SCREEN_KEY} />
      </header>

      {toolbarSlot && (
        <div className="sticky top-[44px] z-20 overflow-x-auto border-b border-slate-200 bg-white dark:border-slate-700 dark:bg-slate-900">
          {toolbarSlot}
        </div>
      )}

      <main className="relative flex-1 overflow-hidden pb-16" role="main">
        <div
          role="tabpanel"
          id="designer-tabpanel-runs"
          aria-labelledby="designer-tab-runs"
          hidden={activeTab !== 'runs'}
          className={cn('h-full overflow-auto', activeTab === 'runs' ? 'block' : 'hidden')}
        >
          {runsSlot}
        </div>
        <div
          role="tabpanel"
          id="designer-tabpanel-canvas"
          aria-labelledby="designer-tab-canvas"
          hidden={activeTab !== 'canvas'}
          className={cn('h-full', activeTab === 'canvas' ? 'block' : 'hidden')}
        >
          {canvasSlot}
        </div>
        <div
          role="tabpanel"
          id="designer-tabpanel-bom"
          aria-labelledby="designer-tab-bom"
          hidden={activeTab !== 'bom'}
          className={cn('h-full overflow-auto', activeTab === 'bom' ? 'block' : 'hidden')}
        >
          {bomSlot}
        </div>
        {activeTab === 'canvas' && !drawerOpen && (
          <button
            type="button"
            onClick={() => {
              setDrawerOpen(true);
              onActiveTabChange('inspector');
            }}
            className={cn(
              'absolute bottom-4 right-4 z-20 rounded-full bg-primary-600 px-4 py-2 text-xs font-semibold text-white shadow-lg',
              mode === 'Simple' && 'px-5 py-3 text-sm',
            )}
          >
            {mode === 'Simple'
              ? `🔧 ${t('GlassEnclosure.Designer.Shell.OpenInspector', { defaultValue: 'Detay' })}`
              : t('GlassEnclosure.Designer.Shell.OpenInspector', { defaultValue: 'Detay' })}
          </button>
        )}
      </main>

      {drawerOpen && (
        <div
          className="fixed inset-0 z-40 flex items-end"
          role="dialog"
          aria-modal="true"
          aria-labelledby="designer-inspector-drawer-title"
        >
          <button
            type="button"
            className="absolute inset-0 bg-slate-900/40 backdrop-blur-sm"
            aria-label={t('GlassEnclosure.Designer.Shell.CloseDrawer', { defaultValue: 'Close' })}
            onClick={handleDrawerClose}
          />
          <div
            role="tabpanel"
            id="designer-tabpanel-inspector"
            aria-labelledby="designer-tab-inspector"
            className={cn(
              'relative w-full max-h-[80vh] flex-col overflow-hidden rounded-t-2xl border-t border-slate-200 bg-white shadow-2xl dark:border-slate-700 dark:bg-slate-900',
              'animate-in slide-in-from-bottom duration-200',
            )}
          >
            <div className="flex items-center justify-between border-b border-slate-200 px-4 py-2 dark:border-slate-700">
              <span
                id="designer-inspector-drawer-title"
                className="text-sm font-semibold text-slate-900 dark:text-slate-100"
              >
                {t(`GlassEnclosure.Designer.Shell.Tab.Inspector.${mode}`, {
                  defaultValue: 'Inspector',
                })}
              </span>
              <button
                type="button"
                onClick={handleDrawerClose}
                className="rounded-md px-2 py-1 text-xs text-slate-500 hover:bg-slate-100 dark:text-slate-400 dark:hover:bg-slate-800"
              >
                {t('GlassEnclosure.Designer.Shell.Close', { defaultValue: 'Kapat' })}
              </button>
            </div>
            <div className="max-h-[70vh] overflow-auto">{inspectorSlot}</div>
          </div>
        </div>
      )}

      <div className="fixed bottom-0 left-0 right-0 z-30 border-t border-slate-200 bg-white dark:border-slate-700 dark:bg-slate-900">
        <DesignerTabBar tabs={tabs} activeKey={activeTab} onSelect={handleTabSelect} />
      </div>
    </div>
  );
};

export default MobileLayout;
