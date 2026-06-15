import { useState, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { cn } from '@/shared/lib/cn';
import { useDesignerUxMode } from '@/features/persona/hooks/useDesignerUxMode';
import { useDesignerStore } from '@/features/glass-enclosure/model/designerStore';
import { RunInspector } from '@/features/glass-enclosure/ui/RunInspector';
import { PanelInspector } from '@/features/glass-enclosure/ui/PanelInspector';
import { HardwareInspector } from '@/features/glass-enclosure/ui/HardwareInspector';
import { WallInspector } from '@/features/glass-enclosure/ui/WallInspector';
import { WallFeatureInspector } from '@/features/glass-enclosure/ui/WallFeatureInspector';
import { SlabFeatureInspector } from '@/features/glass-enclosure/ui/SlabFeatureInspector';
import { SurfaceInspector } from '@/features/glass-enclosure/ui/SurfaceInspector';
import { SlabInspector } from '@/features/glass-enclosure/ui/SlabInspector';
import { RunConnectionInspector } from '@/features/glass-enclosure/ui/RunConnectionInspector';
import { TechnicalSummary } from '@/features/glass-enclosure/ui/TechnicalSummary';
import { ValidationPanel } from '@/features/glass-enclosure/ui/ValidationPanel';
import { FieldSurveyForm } from '@/features/glass-enclosure/ui/FieldSurveyForm';
import { PitchedRoofInspector } from '@/features/glass-enclosure/ui/PitchedRoofInspector';
import { PolygonInspector } from '@/features/glass-enclosure/ui/PolygonInspector';
import type {
  ColorOptionDto,
  GlassTypeDto,
  InspectorSection,
  ProfileSystemDto,
} from '@/features/glass-enclosure/model/glassEnclosure.types';

export type InspectorTabKey =
  | 'general'
  | 'dimensions'
  | 'hardware'
  | 'glass'
  | 'surveys'
  | 'commerce';

interface InspectorPanelProps {
  projectId: string | null;
  profileSystems: ProfileSystemDto[];
  glassTypes: GlassTypeDto[];
  colors: ColorOptionDto[];
  floorNumber?: number | null;
  buildingHeightM?: number | null;
  costSlot?: ReactNode;
  commerceSlot?: ReactNode;
  className?: string;
}

const PRO_TABS: InspectorTabKey[] = [
  'general',
  'dimensions',
  'hardware',
  'glass',
  'surveys',
  'commerce',
];
const SIMPLE_TABS: InspectorTabKey[] = ['general', 'dimensions'];

export const InspectorPanel = ({
  projectId,
  profileSystems,
  glassTypes,
  colors,
  floorNumber,
  buildingHeightM,
  costSlot,
  commerceSlot,
  className,
}: InspectorPanelProps) => {
  const { t } = useTranslation();
  const mode = useDesignerUxMode();
  const isSimple = mode === 'Simple';
  const tabs = isSimple ? SIMPLE_TABS : PRO_TABS;

  const selection = useDesignerStore((s) => s.selection);
  const project = useDesignerStore((s) => s.project);

  const [activeTab, setActiveTab] = useState<InspectorTabKey>(tabs[0]);
  const [lastTabsKey, setLastTabsKey] = useState(tabs);
  if (lastTabsKey !== tabs) {
    setLastTabsKey(tabs);
    if (!tabs.includes(activeTab)) {
      setActiveTab(tabs[0]);
    }
  }

  const tabLabel = (key: InspectorTabKey) =>
    t(`GlassEnclosure.Designer.Shell.InspectorTab.${key}`, {
      defaultValue: defaultInspectorLabel(key),
    });

  const renderSelection = (selectionSections: InspectorSection[]): ReactNode => {
    if (selection.kind === 'wall') return <WallInspector />;
    if (selection.kind === 'wallFeature') return <WallFeatureInspector />;
    if (selection.kind === 'slabFeature') return <SlabFeatureInspector />;
    if (selection.kind === 'surface') return <SurfaceInspector />;
    if (selection.kind === 'slab') return <SlabInspector />;
    if (selection.kind === 'hardware') return <HardwareInspector />;
    if (selection.kind === 'panel')
      return <PanelInspector glassTypes={glassTypes} sections={selectionSections} />;
    if (selection.kind === 'run')
      return (
        <RunInspector
          profileSystems={profileSystems}
          colors={colors}
          glassTypes={glassTypes}
          sections={selectionSections}
        />
      );
    if (selection.kind === 'connection')
      return <RunConnectionInspector profileSystems={profileSystems} />;
    return (
      <div className="flex h-full flex-col items-center justify-center gap-2 p-6 text-center text-sm text-slate-500 dark:text-slate-400">
        <span className="text-3xl" aria-hidden>
          🪟
        </span>
        <p>{t('GlassEnclosure.Designer.NoSelection')}</p>
      </div>
    );
  };

  return (
    <section
      className={cn('flex h-full min-w-0 flex-col bg-white dark:bg-slate-900', className)}
      aria-label={t('GlassEnclosure.Designer.Inspector')}
      data-tour="designer-inspector"
    >
      <div
        role="tablist"
        aria-label={t('GlassEnclosure.Designer.Inspector')}
        className="flex shrink-0 overflow-x-auto border-b border-slate-200 dark:border-slate-700"
      >
        {tabs.map((tab) => {
          const active = tab === activeTab;
          return (
            <button
              key={tab}
              type="button"
              role="tab"
              aria-selected={active}
              onClick={() => setActiveTab(tab)}
              className={cn(
                'whitespace-nowrap border-b-2 px-3 transition-colors',
                isSimple ? 'h-11 text-sm font-semibold' : 'h-9 text-xs font-medium',
                active
                  ? 'border-blue-600 text-blue-600 dark:text-blue-400'
                  : 'border-transparent text-slate-500 hover:text-slate-700 dark:text-slate-400 dark:hover:text-slate-200',
              )}
            >
              {tabLabel(tab)}
            </button>
          );
        })}
      </div>

      <div className="flex-1 overflow-auto">
        <TabBody
          tab={activeTab}
          isSimple={isSimple}
          projectId={projectId}
          floorNumber={floorNumber}
          buildingHeightM={buildingHeightM}
          glassTypes={glassTypes}
          profileSystems={profileSystems}
          renderSelection={renderSelection}
          costSlot={costSlot}
          commerceSlot={commerceSlot}
          polygonSlot={
            project?.geometryMode === 'FreeForm' ? <PolygonInspector project={project} /> : null
          }
        />
      </div>
    </section>
  );
};

interface TabBodyProps {
  tab: InspectorTabKey;
  isSimple: boolean;
  projectId: string | null;
  floorNumber?: number | null;
  buildingHeightM?: number | null;
  glassTypes: GlassTypeDto[];
  profileSystems: ProfileSystemDto[];
  renderSelection: (sections: InspectorSection[]) => ReactNode;
  costSlot?: ReactNode;
  commerceSlot?: ReactNode;
  polygonSlot?: ReactNode;
}

const TabBody = ({
  tab,
  isSimple,
  projectId,
  floorNumber,
  buildingHeightM,
  glassTypes,
  profileSystems,
  renderSelection,
  costSlot,
  commerceSlot,
  polygonSlot,
}: TabBodyProps) => {
  if (tab === 'surveys' && projectId) {
    return (
      <div className={cn(isSimple ? 'p-4 text-base' : 'p-3 text-sm')}>
        <FieldSurveyForm
          projectId={projectId}
          defaultFloorNumber={floorNumber ?? null}
          defaultBuildingHeightM={buildingHeightM ?? null}
        />
      </div>
    );
  }

  if (tab === 'commerce') {
    return (
      <div className={cn(isSimple ? 'space-y-4 p-4 text-base' : 'space-y-3 p-3 text-sm')}>
        {costSlot}
        {commerceSlot}
      </div>
    );
  }

  const sections = tabSections(tab, isSimple);
  const padding = isSimple ? 'space-y-4 p-4 text-base' : 'space-y-3 p-3 text-sm';

  if (tab === 'dimensions') {
    return (
      <div className={cn(padding)}>
        {polygonSlot}
        <PitchedRoofInspector />
        <div>{renderSelection(sections)}</div>
        <div className="border-t border-slate-200 pt-3 dark:border-slate-700">
          <TechnicalSummary glassTypes={glassTypes} profileSystems={profileSystems} />
        </div>
      </div>
    );
  }

  if (tab === 'hardware' || tab === 'glass') {
    return (
      <div className={cn(padding)}>
        <div className="rounded-md border border-slate-200 bg-slate-50 p-3 dark:border-slate-700 dark:bg-slate-800">
          {renderSelection(sections)}
        </div>
      </div>
    );
  }

  return (
    <div className={cn(padding)}>
      <div>{renderSelection(sections)}</div>
      {!isSimple && (
        <div className="border-t border-slate-200 pt-3 dark:border-slate-700">
          <ValidationPanel />
        </div>
      )}
    </div>
  );
};

const tabSections = (tab: InspectorTabKey, isSimple: boolean): InspectorSection[] => {
  if (isSimple && tab === 'general') return ['general', 'hardware', 'glass'];
  switch (tab) {
    case 'dimensions':
      return ['dimensions'];
    case 'hardware':
      return ['hardware'];
    case 'glass':
      return ['glass'];
    default:
      return ['general'];
  }
};

const defaultInspectorLabel = (key: InspectorTabKey): string => {
  switch (key) {
    case 'general':
      return 'General';
    case 'dimensions':
      return 'Dimensions';
    case 'hardware':
      return 'Hardware';
    case 'glass':
      return 'Glass';
    case 'surveys':
      return 'Surveys';
    case 'commerce':
      return 'Commerce';
    default:
      return key;
  }
};

export default InspectorPanel;
