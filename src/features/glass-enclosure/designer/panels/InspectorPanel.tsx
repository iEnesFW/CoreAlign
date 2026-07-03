import { useState, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { cn } from '@/shared/lib/cn';
import { useDesignerUxMode } from '@/shared/lib/persona';
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
  | 'properties'
  | 'general'
  | 'dimensions'
  | 'hardware'
  | 'glass'
  | 'surveys'
  | 'commerce';

const ALL_SECTIONS: InspectorSection[] = ['general', 'dimensions', 'hardware', 'glass'];

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

// Only run/panel inspectors actually split their content by section; every other inspector
// (wall, slab, hardware, surface, features, connection) renders one cohesive editor, so it gets
// a single "Properties" tab instead of four identical ones (the old disorganised/duplicated UX).
const selectionTabKeys = (
  kind: string | null | undefined,
  isSimple: boolean,
): InspectorTabKey[] => {
  if (isSimple) return ['properties'];
  if (kind === 'run') return ['general', 'dimensions', 'hardware', 'glass'];
  // 'hardware' MUST stay in the panel tabs: PanelInspector's hardware section hosts the
  // hasHandle/hasLock toggles AND HardwareManager — the ONLY add-hardware flow in the app.
  if (kind === 'panel') return ['general', 'dimensions', 'hardware', 'glass'];
  return ['properties'];
};

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

  const selection = useDesignerStore((s) => s.selection);
  const project = useDesignerStore((s) => s.project);

  const selectionTabs = selectionTabKeys(selection.kind, isSimple);
  const projectTabs: InspectorTabKey[] = isSimple ? [] : ['surveys', 'commerce'];
  const tabs: InspectorTabKey[] = [...selectionTabs, ...projectTabs];
  const tabsKey = tabs.join('|');

  const [activeTab, setActiveTab] = useState<InspectorTabKey>(tabs[0]);
  const [lastTabsKey, setLastTabsKey] = useState(tabsKey);
  if (lastTabsKey !== tabsKey) {
    setLastTabsKey(tabsKey);
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
        className="flex shrink-0 gap-1 overflow-x-auto border-b border-slate-200 px-2 py-1.5 dark:border-slate-800"
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
                'whitespace-nowrap rounded-md px-3 font-medium transition-colors',
                isSimple ? 'h-9 text-sm' : 'h-7 text-xs',
                active
                  ? 'bg-primary-600 text-white shadow-sm shadow-primary-600/30'
                  : 'text-slate-500 hover:bg-slate-100 hover:text-slate-700 dark:text-slate-400 dark:hover:bg-white/5 dark:hover:text-slate-200',
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
  const padding = isSimple ? 'space-y-4 p-4 text-base' : 'space-y-3 p-3 text-sm';

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
      <div className={cn(padding)}>
        {costSlot}
        {commerceSlot}
      </div>
    );
  }

  // Single cohesive "Properties" view (wall/slab/hardware/surface/features/connection/none, or
  // any selection in Simple mode): the full editor plus the design-wide helper panels.
  if (tab === 'properties') {
    return (
      <div className={cn(padding)}>
        {polygonSlot}
        <PitchedRoofInspector />
        <div>{renderSelection(ALL_SECTIONS)}</div>
        <div className="border-t border-slate-200 pt-3 dark:border-slate-700">
          <TechnicalSummary glassTypes={glassTypes} profileSystems={profileSystems} />
        </div>
        {!isSimple && (
          <div className="border-t border-slate-200 pt-3 dark:border-slate-700">
            <ValidationPanel />
          </div>
        )}
      </div>
    );
  }

  // Section tabs for run/panel (their inspectors filter content by section).
  if (tab === 'dimensions') {
    return (
      <div className={cn(padding)}>
        {polygonSlot}
        <PitchedRoofInspector />
        <div>{renderSelection(['dimensions'])}</div>
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
          {renderSelection([tab])}
        </div>
      </div>
    );
  }

  return (
    <div className={cn(padding)}>
      <div>{renderSelection(['general'])}</div>
      {!isSimple && (
        <div className="border-t border-slate-200 pt-3 dark:border-slate-700">
          <ValidationPanel />
        </div>
      )}
    </div>
  );
};

const defaultInspectorLabel = (key: InspectorTabKey): string => {
  switch (key) {
    case 'properties':
      return 'Özellikler';
    case 'general':
      return 'Genel';
    case 'dimensions':
      return 'Ölçüler';
    case 'hardware':
      return 'Donanım';
    case 'glass':
      return 'Cam';
    case 'surveys':
      return 'Keşif';
    case 'commerce':
      return 'Ticaret';
    default:
      return key;
  }
};

export default InspectorPanel;
