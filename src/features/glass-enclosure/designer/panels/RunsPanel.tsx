import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { Plus } from 'lucide-react';
import { cn } from '@/shared/lib/cn';
import { useDesignerUxMode } from '@/shared/lib/persona';
import { useDesignerStore } from '@/features/glass-enclosure/model/designerStore';
import type { SceneRunState } from '@/features/glass-enclosure/model/project.types';

interface RunsPanelProps {
  onAddRun?: () => void;
  isAdding?: boolean;
  className?: string;
  embedded?: boolean;
}

export const RunsPanel = ({ onAddRun, isAdding, className, embedded }: RunsPanelProps) => {
  const { t } = useTranslation();
  const mode = useDesignerUxMode();
  const isSimple = mode === 'Simple';

  const runs = useDesignerStore((s) => s.scene.runs);
  const selection = useDesignerStore((s) => s.selection);
  const setSelection = useDesignerStore((s) => s.setSelection);

  const orderedRuns = useMemo(() => [...runs].sort((a, b) => a.orderIndex - b.orderIndex), [runs]);

  const handleSelect = (runId: string) => {
    setSelection({ kind: 'run', runId, panelId: null, connectionId: null });
  };

  const addLabel = t(`GlassEnclosure.Designer.Shell.Panel.Runs.Add${mode}`, {
    defaultValue: 'Add Run',
  });

  return (
    <section
      className={cn('flex h-full flex-col bg-white dark:bg-slate-900', className)}
      aria-label={t('GlassEnclosure.Designer.Shell.RunsPanel', { defaultValue: 'Runs' })}
      data-tour="designer-runs"
    >
      {!embedded && (
        <header className="flex items-center justify-between border-b border-slate-200 px-3 py-2 dark:border-slate-700">
          <h2
            className={cn(
              'font-semibold text-slate-900 dark:text-slate-100',
              isSimple ? 'text-base' : 'text-sm',
            )}
          >
            {t(`GlassEnclosure.Designer.Shell.Tab.Runs.${mode}`, {
              defaultValue: isSimple ? 'Cepheler' : 'Runs',
            })}
          </h2>
          <span className="rounded-full bg-slate-100 px-2 py-0.5 text-[10px] font-medium text-slate-600 dark:bg-slate-800 dark:text-slate-300">
            {orderedRuns.length}
          </span>
        </header>
      )}

      {onAddRun && (
        <div className="border-b border-slate-200 p-2 dark:border-slate-700">
          <button
            type="button"
            onClick={onAddRun}
            disabled={isAdding}
            className={cn(
              'inline-flex w-full items-center justify-center gap-1.5 rounded-md bg-primary-600 font-medium text-white transition hover:bg-primary-700 disabled:opacity-50 focus-visible:ring-2 focus-visible:ring-primary-500',
              isSimple ? 'h-12 text-base' : 'h-9 text-sm',
            )}
            aria-label={addLabel}
          >
            <Plus size={isSimple ? 20 : 16} />
            {addLabel}
          </button>
        </div>
      )}

      <ul className="min-h-0 flex-1 overflow-auto" role="list">
        {orderedRuns.length === 0 && (
          <li className="flex h-full flex-col items-center justify-center gap-2 p-6 text-center text-xs text-slate-500 dark:text-slate-400">
            <span className="text-3xl" aria-hidden>
              {isSimple ? '🪟' : '📐'}
            </span>
            <p>
              {t('GlassEnclosure.Designer.Shell.NoRuns', {
                defaultValue: 'Henüz cephe eklenmedi.',
              })}
            </p>
          </li>
        )}
        {orderedRuns.map((run) => (
          <RunListItem
            key={run.id}
            run={run}
            active={selection.kind === 'run' && selection.runId === run.id}
            isSimple={isSimple}
            onSelect={() => handleSelect(run.id)}
          />
        ))}
      </ul>
    </section>
  );
};

interface RunListItemProps {
  run: SceneRunState;
  active: boolean;
  isSimple: boolean;
  onSelect: () => void;
}

const RunListItem = ({ run, active, isSimple, onSelect }: RunListItemProps) => {
  const { t } = useTranslation();
  const panelCount = run.panels.length;

  return (
    <li>
      <button
        type="button"
        onClick={onSelect}
        aria-pressed={active}
        className={cn(
          'flex w-full items-center gap-3 border-b border-slate-100 px-3 py-2 text-left transition-colors dark:border-slate-800',
          active
            ? 'bg-primary-50 dark:bg-primary-950/40'
            : 'hover:bg-slate-50 dark:hover:bg-slate-800/60',
        )}
      >
        <RunThumbnail run={run} active={active} isSimple={isSimple} />
        <div className="min-w-0 flex-1">
          <div
            className={cn(
              'truncate font-medium',
              active
                ? 'text-primary-700 dark:text-primary-300'
                : 'text-slate-900 dark:text-slate-100',
              isSimple ? 'text-base' : 'text-sm',
            )}
          >
            {run.label ||
              `${t('GlassEnclosure.Designer.DefaultRunLabel', { defaultValue: 'Run' })} ${run.orderIndex + 1}`}
          </div>
          <div className="mt-0.5 flex items-center gap-2 text-[11px] text-slate-500 dark:text-slate-400">
            <span>
              {run.lengthMm} × {run.heightMm} mm
            </span>
            <span aria-hidden>·</span>
            <span>
              {t('GlassEnclosure.Designer.PanelCount', {
                count: panelCount,
                defaultValue: '{{count}} panel',
              }).toLowerCase()}
            </span>
          </div>
        </div>
      </button>
    </li>
  );
};

interface RunThumbnailProps {
  run: SceneRunState;
  active: boolean;
  isSimple: boolean;
}

const RunThumbnail = ({ run, active, isSimple }: RunThumbnailProps) => {
  const size = isSimple ? 48 : 40;
  const padding = 4;
  const innerWidth = size - padding * 2;
  const innerHeight = size - padding * 2;
  const aspect = run.lengthMm / Math.max(run.heightMm, 1);
  let drawW = innerWidth;
  let drawH = innerWidth / aspect;
  if (drawH > innerHeight) {
    drawH = innerHeight;
    drawW = innerHeight * aspect;
  }
  const offsetX = (size - drawW) / 2;
  const offsetY = (size - drawH) / 2;
  const panelCount = Math.max(run.panels.length, 1);
  const panelWidth = drawW / panelCount;

  return (
    <svg
      width={size}
      height={size}
      viewBox={`0 0 ${size} ${size}`}
      aria-hidden
      className={cn(
        'shrink-0 rounded border',
        active
          ? 'border-primary-400 bg-primary-50 dark:border-primary-500/60 dark:bg-primary-900/30'
          : 'border-slate-200 bg-slate-50 dark:border-slate-700 dark:bg-slate-800',
      )}
    >
      <rect
        x={offsetX}
        y={offsetY}
        width={drawW}
        height={drawH}
        fill="none"
        stroke="currentColor"
        strokeWidth={1.25}
        className={active ? 'text-primary-500' : 'text-slate-400 dark:text-slate-500'}
      />
      {Array.from({ length: panelCount - 1 }).map((_, i) => {
        const x = offsetX + panelWidth * (i + 1);
        return (
          <line
            key={i}
            x1={x}
            y1={offsetY}
            x2={x}
            y2={offsetY + drawH}
            stroke="currentColor"
            strokeWidth={0.75}
            className={active ? 'text-primary-400' : 'text-slate-300 dark:text-slate-600'}
          />
        );
      })}
    </svg>
  );
};

export default RunsPanel;
