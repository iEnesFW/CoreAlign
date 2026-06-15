import { useEffect, useState, type ReactNode } from 'react';
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

  if (layout === 'mobile') {
    return (
      <MobileLayout
        headerTitle={headerTitle}
        headerSubtitle={headerSubtitle}
        headerRight={headerRight}
        toolbarSlot={toolbarSlot}
        onBack={onBack}
        runsSlot={runsSlot}
        canvasSlot={canvasSlot}
        inspectorSlot={inspectorSlot}
        bomSlot={bomSlot}
        activeTab={activeTab}
        onActiveTabChange={setActiveTab}
      />
    );
  }

  if (layout === 'tablet') {
    return (
      <TabletLayout
        headerTitle={headerTitle}
        headerSubtitle={headerSubtitle}
        headerRight={headerRight}
        toolbarSlot={toolbarSlot}
        onBack={onBack}
        runsSlot={runsSlot}
        canvasSlot={canvasSlot}
        inspectorSlot={inspectorSlot}
        bomSlot={bomSlot}
        activeTab={activeTab}
        onActiveTabChange={setActiveTab}
        sidePanelsDefaultCollapsed={sidePanelsDefaultCollapsed}
      />
    );
  }

  return (
    <DesktopLayout
      headerTitle={headerTitle}
      headerSubtitle={headerSubtitle}
      headerRight={headerRight}
      toolbarSlot={toolbarSlot}
      onBack={onBack}
      runsSlot={runsSlot}
      canvasSlot={canvasSlot}
      inspectorSlot={inspectorSlot}
      bomSlot={bomSlot}
      sidePanelsDefaultCollapsed={sidePanelsDefaultCollapsed}
    />
  );
};

export default DesignerShell;
