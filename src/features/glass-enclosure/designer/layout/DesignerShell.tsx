import { useEffect, useRef, useState, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { Maximize2, Minimize2 } from 'lucide-react';
import { useFullscreen } from '@/shared/hooks/useFullscreen';
import { MobileLayout } from './MobileLayout';
import { TabletLayout } from './TabletLayout';
import { DesktopLayout } from './DesktopLayout';
import type { DesignerTabKey } from './DesignerTabBar';

type LayoutMode = 'mobile' | 'tablet' | 'desktop';

const useResponsiveLayout = (): LayoutMode => {
  const [layout, setLayout] = useState<LayoutMode>(() => {
    if (typeof window === 'undefined') return 'desktop';
    const w = window.innerWidth;
    if (w < 640) return 'mobile';
    if (w < 1024) return 'tablet';
    return 'desktop';
  });

  useEffect(() => {
    const compute = () => {
      const w = window.innerWidth;
      if (w < 640) setLayout('mobile');
      else if (w < 1024) setLayout('tablet');
      else setLayout('desktop');
    };
    compute();
    window.addEventListener('resize', compute);
    return () => window.removeEventListener('resize', compute);
  }, []);

  return layout;
};

export interface DesignerShellProps {
  headerTitle: string;
  headerSubtitle?: string;
  headerRight?: ReactNode;
  toolbarSlot?: ReactNode;
  onBack?: () => void;
  runsSlot: ReactNode;
  canvasSlot: ReactNode;
  inspectorSlot: ReactNode;
  bomSlot: ReactNode;
  initialTab?: DesignerTabKey;
  sidePanelsDefaultCollapsed?: boolean;
}

export const DesignerShell = ({
  headerTitle,
  headerSubtitle,
  headerRight,
  toolbarSlot,
  onBack,
  runsSlot,
  canvasSlot,
  inspectorSlot,
  bomSlot,
  initialTab = 'canvas',
  sidePanelsDefaultCollapsed = false,
}: DesignerShellProps) => {
  const layout = useResponsiveLayout();
  const [activeTab, setActiveTab] = useState<DesignerTabKey>(initialTab);
  const { t } = useTranslation();
  // WHY the shell owns this and not the page: fullscreen needs the element that actually holds the
  // whole designer. The page only supplies slots — it has no handle on the container, and putting
  // the button in a slot would fullscreen the browser tab (app chrome and all) instead of the
  // designer itself.
  const rootRef = useRef<HTMLDivElement>(null);
  const { isFullscreen, toggle, supported } = useFullscreen(rootRef);

  const fullscreenButton = supported ? (
    <button
      type="button"
      onClick={toggle}
      title={
        isFullscreen
          ? t('GlassEnclosure.Designer.Fullscreen.Exit', { defaultValue: 'Tam ekrandan çık' })
          : t('GlassEnclosure.Designer.Fullscreen.Enter', { defaultValue: 'Tam ekran' })
      }
      aria-label={
        isFullscreen
          ? t('GlassEnclosure.Designer.Fullscreen.Exit', { defaultValue: 'Tam ekrandan çık' })
          : t('GlassEnclosure.Designer.Fullscreen.Enter', { defaultValue: 'Tam ekran' })
      }
      className="rounded border border-slate-300 p-1 text-slate-600 hover:bg-slate-100 dark:border-slate-600 dark:text-slate-300 dark:hover:bg-slate-800"
    >
      {isFullscreen ? <Minimize2 size={14} /> : <Maximize2 size={14} />}
    </button>
  ) : null;

  const composedHeaderRight = (
    <span className="flex items-center gap-2">
      {headerRight}
      {fullscreenButton}
    </span>
  );

  const shell = (content: ReactNode) => (
    // bg is required: a fullscreened element has NO page background behind it, so without one the
    // designer renders on the browser's default black.
    <div ref={rootRef} className="h-full min-h-0 bg-white dark:bg-slate-950">
      {content}
    </div>
  );

  if (layout === 'mobile') {
    return shell(
      <MobileLayout
        headerTitle={headerTitle}
        headerSubtitle={headerSubtitle}
        headerRight={composedHeaderRight}
        toolbarSlot={toolbarSlot}
        onBack={onBack}
        runsSlot={runsSlot}
        canvasSlot={canvasSlot}
        inspectorSlot={inspectorSlot}
        bomSlot={bomSlot}
        activeTab={activeTab}
        onActiveTabChange={setActiveTab}
      />,
    );
  }

  if (layout === 'tablet') {
    return shell(
      <TabletLayout
        headerTitle={headerTitle}
        headerSubtitle={headerSubtitle}
        headerRight={composedHeaderRight}
        toolbarSlot={toolbarSlot}
        onBack={onBack}
        runsSlot={runsSlot}
        canvasSlot={canvasSlot}
        inspectorSlot={inspectorSlot}
        bomSlot={bomSlot}
        activeTab={activeTab}
        onActiveTabChange={setActiveTab}
        sidePanelsDefaultCollapsed={sidePanelsDefaultCollapsed}
      />,
    );
  }

  return shell(
    <DesktopLayout
      headerTitle={headerTitle}
      headerSubtitle={headerSubtitle}
      headerRight={composedHeaderRight}
      toolbarSlot={toolbarSlot}
      onBack={onBack}
      runsSlot={runsSlot}
      canvasSlot={canvasSlot}
      inspectorSlot={inspectorSlot}
      bomSlot={bomSlot}
      sidePanelsDefaultCollapsed={sidePanelsDefaultCollapsed}
    />,
  );
};

export default DesignerShell;
