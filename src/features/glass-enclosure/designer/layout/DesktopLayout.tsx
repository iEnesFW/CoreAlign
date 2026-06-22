import { useState, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import {
  ArrowLeft,
  ChevronLeft,
  ChevronRight,
  Maximize2,
  Minimize2,
  SlidersHorizontal,
  X,
} from 'lucide-react';
import { cn } from '@/shared/lib/cn';
import { useDesignerUxMode, DESIGNER_SCREEN_KEY } from '@/shared/lib/persona';
import { ScreenPersonaMenu } from '@/features/persona/ui/ScreenPersonaMenu';
import { useDesignerStore } from '@/features/glass-enclosure/model/designerStore';
import { SidePanelRail } from './SidePanelRail';
import { useSidePanelCollapse } from './useSidePanelCollapse';

interface DesktopLayoutProps {
  headerTitle: string;
  headerSubtitle?: string;
  headerRight?: ReactNode;
  toolbarSlot?: ReactNode;
  onBack?: () => void;
  runsSlot: ReactNode;
  canvasSlot: ReactNode;
  inspectorSlot: ReactNode;
  bomSlot?: ReactNode;
  sidePanelsDefaultCollapsed?: boolean;
}

const RAIL_WIDTH = '36px';

export const DesktopLayout = ({
  headerTitle,
  headerSubtitle,
  headerRight,
  toolbarSlot,
  onBack,
  runsSlot,
  canvasSlot,
  inspectorSlot,
  bomSlot,
  sidePanelsDefaultCollapsed = false,
}: DesktopLayoutProps) => {
  const { t } = useTranslation();
  const mode = useDesignerUxMode();
  const isPro = mode === 'Pro';
  const leftPanel = useSidePanelCollapse(
    'collapse:glassDesigner.runsPanel',
    sidePanelsDefaultCollapsed,
  );
  const rightPanel = useSidePanelCollapse(
    'collapse:glassDesigner.inspectorPanel',
    sidePanelsDefaultCollapsed,
  );
  const [inspectorModalOpen, setInspectorModalOpen] = useState(false);
  const [focusMode, setFocusMode] = useState(false);
  const [leftTab, setLeftTab] = useState<'runs' | 'bom'>('runs');
  const runsCount = useDesignerStore((s) => s.scene.runs.length);

  const runsLabel = t(`GlassEnclosure.Designer.Shell.Tab.Runs.${mode}`, { defaultValue: 'Runs' });
  const inspectorLabel = t(`GlassEnclosure.Designer.Shell.Tab.Inspector.${mode}`, {
    defaultValue: 'Inspector',
  });

  const leftWidth = focusMode ? '0px' : leftPanel.collapsed ? RAIL_WIDTH : '320px';
  const rightWidth = !isPro || focusMode ? '0px' : rightPanel.collapsed ? RAIL_WIDTH : '360px';

  return (
    <div className="flex h-full flex-col bg-slate-50 dark:bg-slate-950" role="application">
      <header className="flex items-center gap-3 border-b border-slate-200 bg-white px-4 py-3 dark:border-slate-700 dark:bg-slate-900">
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
          <h1 className="truncate text-lg font-semibold text-slate-900 dark:text-slate-100">
            {headerTitle}
          </h1>
          {headerSubtitle && (
            <p className="truncate text-xs text-slate-500 dark:text-slate-400">{headerSubtitle}</p>
          )}
        </div>
        {headerRight && (
          <div className="text-right text-xs text-slate-500 dark:text-slate-400">{headerRight}</div>
        )}
        <button
          type="button"
          onClick={() => setFocusMode((v) => !v)}
          aria-pressed={focusMode}
          className="rounded-md p-1.5 text-slate-500 hover:bg-slate-100 dark:text-slate-400 dark:hover:bg-slate-800"
          aria-label={t('GlassEnclosure.Designer.Shell.FocusMode', { defaultValue: 'Tam ekran' })}
        >
          {focusMode ? <Minimize2 size={18} /> : <Maximize2 size={18} />}
        </button>
        <ScreenPersonaMenu screenKey={DESIGNER_SCREEN_KEY} />
      </header>

      {toolbarSlot}

      <div
        className="relative grid flex-1 overflow-hidden"
        style={{ gridTemplateColumns: `${leftWidth} 1fr ${rightWidth}` }}
      >
        <aside
          className={cn(
            'relative flex min-w-0 flex-col overflow-hidden border-r border-slate-200 bg-white dark:border-slate-700 dark:bg-slate-900',
            focusMode && 'border-r-0',
          )}
          aria-label={runsLabel}
        >
          {leftPanel.collapsed ? (
            !focusMode && (
              <SidePanelRail
                side="left"
                label={runsLabel}
                expandLabel={t('GlassEnclosure.Designer.Shell.ExpandRunsPanel', {
                  defaultValue: 'Expand runs panel',
                })}
                onExpand={() => leftPanel.setCollapsed(false)}
              />
            )
          ) : (
            <>
              <div className="flex items-stretch justify-between border-b border-slate-200 pr-1 dark:border-slate-700">
                <div role="tablist" className="flex min-w-0">
                  <button
                    type="button"
                    role="tab"
                    aria-selected={leftTab === 'runs'}
                    onClick={() => setLeftTab('runs')}
                    className={cn(
                      'inline-flex items-center gap-1.5 border-b-2 px-3 py-2 text-xs font-semibold uppercase tracking-wide transition-colors',
                      leftTab === 'runs'
                        ? 'border-primary-600 text-primary-600 dark:text-primary-400'
                        : 'border-transparent text-slate-500 hover:text-slate-700 dark:text-slate-400 dark:hover:text-slate-200',
                    )}
                  >
                    {runsLabel}
                    <span className="rounded-full bg-slate-100 px-1.5 text-[10px] font-medium text-slate-600 dark:bg-slate-800 dark:text-slate-300">
                      {runsCount}
                    </span>
                  </button>
                  {bomSlot && (
                    <button
                      type="button"
                      role="tab"
                      aria-selected={leftTab === 'bom'}
                      onClick={() => setLeftTab('bom')}
                      className={cn(
                        'border-b-2 px-3 py-2 text-xs font-semibold uppercase tracking-wide transition-colors',
                        leftTab === 'bom'
                          ? 'border-primary-600 text-primary-600 dark:text-primary-400'
                          : 'border-transparent text-slate-500 hover:text-slate-700 dark:text-slate-400 dark:hover:text-slate-200',
                      )}
                    >
                      {t(`GlassEnclosure.Designer.Shell.Tab.BOM.${mode}`, { defaultValue: 'BOM' })}
                    </button>
                  )}
                </div>
                <button
                  type="button"
                  onClick={() => leftPanel.setCollapsed(true)}
                  className="my-1 shrink-0 rounded p-1 text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800"
                  aria-expanded={!leftPanel.collapsed}
                  aria-label={t('GlassEnclosure.Designer.Shell.CollapseRunsPanel', {
                    defaultValue: 'Collapse runs panel',
                  })}
                >
                  <ChevronLeft size={14} />
                </button>
              </div>
              <div className="min-h-0 flex-1 overflow-auto">
                {leftTab === 'bom' && bomSlot ? bomSlot : runsSlot}
              </div>
            </>
          )}
        </aside>

        <main className="relative flex min-h-0 flex-col overflow-hidden">{canvasSlot}</main>

        {isPro && !focusMode && (
          <aside
            className="flex min-w-0 flex-col overflow-hidden border-l border-slate-200 bg-white dark:border-slate-700 dark:bg-slate-900"
            aria-label={inspectorLabel}
          >
            {rightPanel.collapsed ? (
              <SidePanelRail
                side="right"
                label={inspectorLabel}
                expandLabel={t('GlassEnclosure.Designer.Shell.ExpandInspectorPanel', {
                  defaultValue: 'Expand inspector panel',
                })}
                onExpand={() => rightPanel.setCollapsed(false)}
              />
            ) : (
              <>
                <div className="flex items-center justify-end border-b border-slate-200 px-1 py-1 dark:border-slate-700">
                  <button
                    type="button"
                    onClick={() => rightPanel.setCollapsed(true)}
                    className="rounded p-1 text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800"
                    aria-expanded={!rightPanel.collapsed}
                    aria-label={t('GlassEnclosure.Designer.Shell.CollapseInspectorPanel', {
                      defaultValue: 'Collapse inspector panel',
                    })}
                  >
                    <ChevronRight size={14} />
                  </button>
                </div>
                <div className="flex-1 overflow-auto">{inspectorSlot}</div>
              </>
            )}
          </aside>
        )}
      </div>

      {!isPro && !focusMode && (
        <button
          type="button"
          onClick={() => setInspectorModalOpen(true)}
          className="fixed bottom-6 right-6 z-20 inline-flex items-center gap-2 rounded-full bg-primary-600 px-4 py-2 text-sm font-semibold text-white shadow-lg hover:bg-primary-700"
        >
          <SlidersHorizontal size={16} />
          {t('GlassEnclosure.Designer.Shell.OpenInspector', { defaultValue: 'Detay' })}
        </button>
      )}

      {!isPro && inspectorModalOpen && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-end"
          role="dialog"
          aria-modal="true"
        >
          <button
            type="button"
            className="absolute inset-0 bg-slate-900/40 backdrop-blur-sm"
            aria-label={t('GlassEnclosure.Designer.Shell.CloseDrawer', { defaultValue: 'Close' })}
            onClick={() => setInspectorModalOpen(false)}
          />
          <div className="relative flex h-full w-full max-w-md flex-col border-l border-slate-200 bg-white shadow-2xl dark:border-slate-700 dark:bg-slate-900">
            <div className="flex items-center justify-between border-b border-slate-200 px-4 py-3 dark:border-slate-700">
              <span className="text-sm font-semibold text-slate-900 dark:text-slate-100">
                {inspectorLabel}
              </span>
              <button
                type="button"
                onClick={() => setInspectorModalOpen(false)}
                className="rounded-md p-1 text-slate-500 hover:bg-slate-100 dark:text-slate-400 dark:hover:bg-slate-800"
              >
                <X size={16} />
              </button>
            </div>
            <div className="flex-1 overflow-auto">{inspectorSlot}</div>
          </div>
        </div>
      )}
    </div>
  );
};

export default DesktopLayout;
