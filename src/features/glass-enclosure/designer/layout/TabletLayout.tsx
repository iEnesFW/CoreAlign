import { useState, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import {
  ArrowLeft,
  Boxes,
  ClipboardList,
  FileSpreadsheet,
  SlidersHorizontal,
  X,
} from 'lucide-react';
import { cn } from '@/shared/lib/cn';
import { useDesignerUxMode, DESIGNER_SCREEN_KEY } from '@/features/persona/hooks/useDesignerUxMode';
import { ScreenPersonaMenu } from '@/features/persona/ui/ScreenPersonaMenu';
import { DesignerTabBar, type DesignerTabItem, type DesignerTabKey } from './DesignerTabBar';

interface TabletLayoutProps {
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
  sidePanelsDefaultCollapsed?: boolean;
}

export const TabletLayout = ({
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
  sidePanelsDefaultCollapsed = false,
}: TabletLayoutProps) => {
  const { t } = useTranslation();
  const mode = useDesignerUxMode();
  const isPro = mode === 'Pro';
  const autoInspectorOpen = isPro && !sidePanelsDefaultCollapsed;
  const [inspectorOpen, setInspectorOpen] = useState<boolean>(autoInspectorOpen);
  const [lastAutoInspectorOpen, setLastAutoInspectorOpen] = useState(autoInspectorOpen);

  if (lastAutoInspectorOpen !== autoInspectorOpen) {
    setLastAutoInspectorOpen(autoInspectorOpen);
    setInspectorOpen(autoInspectorOpen);
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
    if (key === 'inspector') {
      setInspectorOpen(true);
      return;
    }
    onActiveTabChange(key);
  };

  const mainTabId = activeTab === 'runs' ? 'runs' : activeTab === 'bom' ? 'bom' : 'canvas';
  const mainSlot = activeTab === 'runs' ? runsSlot : activeTab === 'bom' ? bomSlot : canvasSlot;

  return (
    <div className="flex h-full flex-col bg-slate-50 dark:bg-slate-950" role="application">
      <header className="flex items-center gap-2 border-b border-slate-200 bg-white px-4 py-2 dark:border-slate-700 dark:bg-slate-900">
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
          <div className="ml-1 flex shrink-0 items-center text-xs text-slate-500 dark:text-slate-400">
            {headerRight}
          </div>
        )}
        <ScreenPersonaMenu screenKey={DESIGNER_SCREEN_KEY} />
      </header>

      {toolbarSlot && (
        <div className="border-b border-slate-200 bg-white dark:border-slate-700 dark:bg-slate-900">
          {toolbarSlot}
        </div>
      )}

      <div className="relative flex flex-1 overflow-hidden">
        <aside
          className={cn(
            'flex shrink-0 flex-col border-r border-slate-200 bg-white dark:border-slate-700 dark:bg-slate-900',
            isPro ? 'w-20' : 'w-16',
          )}
          aria-label={t('GlassEnclosure.Designer.Shell.SideRail', { defaultValue: 'Navigation' })}
        >
          <DesignerTabBar
            tabs={tabs}
            activeKey={activeTab === 'inspector' && inspectorOpen ? 'inspector' : activeTab}
            onSelect={handleTabSelect}
            orientation="vertical"
          />
        </aside>

        <main
          className="relative flex-1 overflow-hidden"
          role="tabpanel"
          id={`designer-tabpanel-${mainTabId}`}
          aria-labelledby={`designer-tab-${mainTabId}`}
        >
          {mainSlot}
        </main>

        <aside
          role="tabpanel"
          id="designer-tabpanel-inspector"
          aria-labelledby="designer-tab-inspector"
          className={cn(
            'absolute right-0 top-0 z-30 flex h-full w-80 flex-col border-l border-slate-200 bg-white shadow-xl transition-transform duration-200 dark:border-slate-700 dark:bg-slate-900',
            inspectorOpen ? 'translate-x-0' : 'translate-x-full',
          )}
          aria-hidden={!inspectorOpen}
        >
          <div className="flex items-center justify-between border-b border-slate-200 px-4 py-2 dark:border-slate-700">
            <span className="text-sm font-semibold text-slate-900 dark:text-slate-100">
              {t(`GlassEnclosure.Designer.Shell.Tab.Inspector.${mode}`, {
                defaultValue: 'Inspector',
              })}
            </span>
            <button
              type="button"
              onClick={() => {
                setInspectorOpen(false);
                if (activeTab === 'inspector') onActiveTabChange('canvas');
              }}
              className="rounded-md p-1 text-slate-500 hover:bg-slate-100 dark:text-slate-400 dark:hover:bg-slate-800"
              aria-label={t('GlassEnclosure.Designer.Shell.CloseDrawer', { defaultValue: 'Close' })}
            >
              <X size={16} />
            </button>
          </div>
          <div className="flex-1 overflow-auto">{inspectorSlot}</div>
        </aside>
      </div>
    </div>
  );
};

export default TabletLayout;
