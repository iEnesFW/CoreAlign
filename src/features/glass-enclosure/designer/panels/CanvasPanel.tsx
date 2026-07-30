import type { ReactNode } from 'react';
import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { Grid3X3, Maximize2, ZoomIn, ZoomOut } from 'lucide-react';
import { cn } from '@/shared/lib/cn';
import { DESIGNER_ROOT_NAME, viewportCamera, ZOOM_STEP } from '@/shared/three-engine';
import { useDesignerUxMode } from '@/shared/lib/persona';
import { DesignerCanvas } from '@/features/glass-enclosure/scene/DesignerCanvas';
import { CanvasErrorBoundary } from '@/features/glass-enclosure/scene/CanvasErrorBoundary';
import { SelectionToolbar } from '@/features/glass-enclosure/designer/panels/SelectionToolbar';
import { ShortcutsHelp } from '@/features/glass-enclosure/designer/panels/ShortcutsHelp';
import { TransformToolbar } from '@/features/glass-enclosure/designer/panels/TransformToolbar';
import { ToolPalette } from '@/features/glass-enclosure/designer/panels/ToolPalette';
import { LayersControl } from '@/features/glass-enclosure/designer/panels/LayersControl';
import { Plan2DCanvas } from '@/features/glass-enclosure/ui/Plan2DCanvas';
import { useDesignerStore } from '@/features/glass-enclosure/model/designerStore';
import type {
  ColorOptionDto,
  GlassTypeDto,
  ProfileSystemDto,
} from '@/features/glass-enclosure/model/glassEnclosure.types';

export type CanvasView = '2d' | '3d' | 'split';

interface CanvasPanelProps {
  view: CanvasView;
  profileSystems: ProfileSystemDto[];
  glassTypes: GlassTypeDto[];
  colors: ColorOptionDto[];
  onAddRunFromPlan: (
    start: { x: number; y: number },
    end: { x: number; y: number },
  ) => void | Promise<void>;
  onUpdateRunGeometry: (
    runId: string,
    geometry: {
      lengthMm: number;
      originX: number;
      originY: number;
      rotationDeg: number;
    },
  ) => void | Promise<void>;
  onSelectConnectionCandidate: (runAId: string, runBId: string) => void | Promise<void>;
  onZoomIn?: () => void;
  onZoomOut?: () => void;
  onFitToScreen?: () => void;
  toolbarSlot?: ReactNode;
  className?: string;
}

export const CanvasPanel = ({
  view,
  profileSystems,
  glassTypes,
  colors,
  onAddRunFromPlan,
  onUpdateRunGeometry,
  onSelectConnectionCandidate,
  onZoomIn,
  onZoomOut,
  onFitToScreen,
  toolbarSlot,
  className,
}: CanvasPanelProps) => {
  const { t } = useTranslation();
  const mode = useDesignerUxMode();
  const isSimple = mode === 'Simple';

  const showAnnotations = useDesignerStore((s) => s.showAnnotations);
  const toggleAnnotations = useDesignerStore((s) => s.toggleAnnotations);
  const selectedRunId = useDesignerStore((s) => s.selection.runId);

  const selectedRunLabel = useDesignerStore((s) => {
    if (!selectedRunId) return null;
    const run = s.scene.runs.find((r) => r.id === selectedRunId);
    return run?.label ?? null;
  });

  const btnClass = useMemo(
    () =>
      cn(
        'inline-flex items-center justify-center gap-1.5 rounded-md border border-slate-300 bg-white font-medium text-slate-700 transition hover:bg-slate-50 disabled:opacity-50 focus-visible:ring-2 focus-visible:ring-primary-500 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800',
        isSimple ? 'h-12 px-3 text-sm' : 'h-8 px-2 text-xs',
      ),
    [isSimple],
  );

  const iconSize = isSimple ? 18 : 14;

  // WHY defaults: these three props were optional and NOTHING ever passed them, so the buttons
  // rendered permanently disabled. A project whose geometry sits away from the origin then opens
  // with the content off-frame and the user has no way back except manually orbiting. The camera
  // itself is the right owner of the behaviour, so fall back to it and keep the props as overrides.
  const is3d = view === '3d';
  const zoomIn = onZoomIn ?? (is3d ? () => viewportCamera.zoomBy(1 / ZOOM_STEP) : undefined);
  const zoomOut = onZoomOut ?? (is3d ? () => viewportCamera.zoomBy(ZOOM_STEP) : undefined);
  const fitToScreen =
    onFitToScreen ?? (is3d ? () => viewportCamera.fitTo(DESIGNER_ROOT_NAME) : undefined);

  return (
    <section
      className={cn('flex h-full flex-col bg-slate-100 dark:bg-slate-950', className)}
      aria-label={t('GlassEnclosure.Designer.Shell.CanvasPanel', { defaultValue: 'Canvas' })}
      data-tour="designer-canvas"
    >
      <div
        className={cn(
          'flex flex-wrap items-center gap-2 border-b border-slate-200 bg-white px-3 dark:border-slate-700 dark:bg-slate-900',
          isSimple ? 'py-2' : 'py-1.5',
        )}
      >
        <button
          type="button"
          onClick={zoomIn}
          disabled={!zoomIn}
          className={btnClass}
          aria-label={t('GlassEnclosure.Designer.Shell.ZoomIn', { defaultValue: 'Zoom in' })}
        >
          <ZoomIn size={iconSize} />
          {isSimple && (
            <span>{t('GlassEnclosure.Designer.Shell.ZoomIn', { defaultValue: 'Yakınlaş' })}</span>
          )}
        </button>
        <button
          type="button"
          onClick={zoomOut}
          disabled={!zoomOut}
          className={btnClass}
          aria-label={t('GlassEnclosure.Designer.Shell.ZoomOut', { defaultValue: 'Zoom out' })}
        >
          <ZoomOut size={iconSize} />
          {isSimple && (
            <span>{t('GlassEnclosure.Designer.Shell.ZoomOut', { defaultValue: 'Uzaklaş' })}</span>
          )}
        </button>
        <button
          type="button"
          onClick={fitToScreen}
          disabled={!fitToScreen}
          className={btnClass}
          aria-label={t('GlassEnclosure.Designer.Shell.FitToScreen', {
            defaultValue: 'Fit to screen',
          })}
        >
          <Maximize2 size={iconSize} />
          {isSimple && (
            <span>{t('GlassEnclosure.Designer.Shell.Fit', { defaultValue: 'Sığdır' })}</span>
          )}
        </button>
        <span className="mx-1 h-5 w-px bg-slate-300 dark:bg-slate-700" />
        <button
          type="button"
          onClick={toggleAnnotations}
          aria-pressed={showAnnotations}
          className={cn(btnClass, showAnnotations && 'text-primary-600 dark:text-primary-400')}
          aria-label={t('GlassEnclosure.Designer.Annotations', { defaultValue: 'Toggle grid' })}
        >
          <Grid3X3 size={iconSize} />
          {isSimple && (
            <span>{t('GlassEnclosure.Designer.Shell.Grid', { defaultValue: 'Izgara' })}</span>
          )}
        </button>
        <span className="mx-1 h-5 w-px bg-slate-300 dark:bg-slate-700" />
        <LayersControl />
        {toolbarSlot && (
          <>
            <span className="mx-1 h-5 w-px bg-slate-300 dark:bg-slate-700" />
            <div className="flex items-center gap-2">{toolbarSlot}</div>
          </>
        )}
        <div className="ml-auto flex min-w-0 items-center gap-2">
          {selectedRunLabel && (
            <div className="truncate text-[11px] text-slate-500 dark:text-slate-400">
              {t('GlassEnclosure.Designer.Shell.SelectedRun', { defaultValue: 'Selected' })}:{' '}
              <span className="font-medium text-slate-700 dark:text-slate-200">
                {selectedRunLabel}
              </span>
            </div>
          )}
          <ShortcutsHelp triggerClassName={btnClass} iconSize={iconSize} />
        </div>
      </div>

      <div className="flex min-h-0 flex-1 overflow-hidden">
        {(view === 'split' || view === '2d') && (
          <div
            className={cn(
              'h-full min-h-0',
              view === 'split' ? 'w-1/2 border-r border-slate-200 dark:border-slate-700' : 'flex-1',
            )}
          >
            <Plan2DCanvas
              onAddRun={onAddRunFromPlan}
              onUpdateRunGeometry={onUpdateRunGeometry}
              onSelectConnectionCandidate={onSelectConnectionCandidate}
            />
          </div>
        )}
        {(view === 'split' || view === '3d') && (
          <div
            className={cn(
              'relative h-full min-h-0',
              view === 'split'
                ? 'w-1/2 bg-slate-100 dark:bg-slate-900'
                : 'flex-1 bg-slate-100 dark:bg-slate-900',
            )}
          >
            <SelectionToolbar glassTypes={glassTypes} />
            <TransformToolbar />
            <ToolPalette />
            <CanvasErrorBoundary
              fallbackLabel={t('GlassEnclosure.Designer.CanvasError', {
                defaultValue:
                  'Görünüm çizilirken bir hata oluştu. Tasarımınız kayıtlı; görünümü yeniden yükleyin.',
              })}
              retryLabel={t('GlassEnclosure.Designer.CanvasErrorRetry', {
                defaultValue: 'Görünümü yeniden yükle',
              })}
            >
              <DesignerCanvas
                profileSystems={profileSystems}
                glassTypes={glassTypes}
                colors={colors}
              />
            </CanvasErrorBoundary>
          </div>
        )}
      </div>
    </section>
  );
};

export default CanvasPanel;
