import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { AlertTriangle, RotateCw, Scissors } from 'lucide-react';
import { cn } from '@/shared/lib/cn';
import { Modal } from '@/shared/ui/Modal/Modal';
import { useDesignerUxMode } from '@/features/persona/hooks/useDesignerUxMode';
import { QuoteSummaryView } from '@/features/glass-enclosure/ui/QuoteSummaryView';
import { Optimize2DButton } from '@/features/glass-enclosure/cutting/Optimize2DButton';
import { Glass2DNestingViewer } from '@/features/glass-enclosure/cutting/Glass2DNestingViewer';
import type {
  BOMSummaryDto,
  Glass2DNestingReportDto,
} from '@/features/glass-enclosure/model/engineering.types';
import type { GlassProjectDto } from '@/features/glass-enclosure/model/project.types';

interface BOMPanelProps {
  project: GlassProjectDto;
  bom: BOMSummaryDto | null;
  isLoading: boolean;
  onRecompute: () => void | Promise<void>;
  isRecomputing: boolean;
  className?: string;
}

export const BOMPanel = ({
  project,
  bom,
  isLoading,
  onRecompute,
  isRecomputing,
  className,
}: BOMPanelProps) => {
  const { t } = useTranslation();
  const mode = useDesignerUxMode();
  const isSimple = mode === 'Simple';
  const [nestingOpen, setNestingOpen] = useState(false);
  const [nestingReport, setNestingReport] = useState<Glass2DNestingReportDto | null>(null);

  const title = isSimple
    ? t('GlassEnclosure.Designer.Shell.BomTitleSimple', { defaultValue: 'Malzeme Listesi' })
    : t('GlassEnclosure.Designer.Shell.BomTitlePro', { defaultValue: 'BOM' });

  const lineCount = bom?.lines.length ?? 0;
  const totalLabel = bom
    ? new Intl.NumberFormat(undefined, {
        style: 'currency',
        currency: bom.currency ?? project.currency ?? 'TRY',
        maximumFractionDigits: 2,
      }).format(bom.grandTotal)
    : null;

  return (
    <section
      className={cn('flex h-full flex-col bg-slate-50 dark:bg-slate-950', className)}
      aria-label={t('GlassEnclosure.Designer.Shell.BomPanel', {
        defaultValue: 'Bill of Materials',
      })}
      data-tour="designer-bom"
    >
      <header
        className={cn(
          'flex flex-wrap items-center justify-between gap-2 border-b border-slate-200 bg-white px-3 dark:border-slate-700 dark:bg-slate-900',
          isSimple ? 'py-3' : 'py-2',
        )}
      >
        <div className="flex items-center gap-2">
          <h2
            className={cn(
              'font-semibold text-slate-900 dark:text-slate-100',
              isSimple ? 'text-lg' : 'text-sm',
            )}
          >
            {title}
          </h2>
          {project.isBomStale && (
            <span
              role="status"
              className={cn(
                'inline-flex items-center gap-1 rounded-full border border-amber-300 bg-amber-50 px-2 py-0.5 font-medium text-amber-700 dark:border-amber-700/50 dark:bg-amber-950/40 dark:text-amber-300',
                isSimple ? 'text-xs' : 'text-[10px]',
              )}
              title={project.bomStaleReason ?? undefined}
            >
              <AlertTriangle size={isSimple ? 14 : 11} />
              {t('GlassEnclosure.Designer.Shell.BomStale', { defaultValue: 'Outdated' })}
            </span>
          )}
        </div>

        <div className="flex min-w-0 flex-wrap items-center justify-end gap-2">
          <Badge
            label={t('GlassEnclosure.Designer.Shell.BomLines', { defaultValue: 'Lines' })}
            value={lineCount.toString()}
            isSimple={isSimple}
          />
          {totalLabel && (
            <Badge
              label={t('GlassEnclosure.Designer.Shell.BomTotal', { defaultValue: 'Total' })}
              value={totalLabel}
              isSimple={isSimple}
              accent
            />
          )}
          <button
            type="button"
            onClick={() => setNestingOpen(true)}
            className={cn(
              'inline-flex items-center gap-1.5 rounded-md border border-violet-300 bg-violet-50 font-medium text-violet-700 transition hover:bg-violet-100 focus-visible:ring-2 focus-visible:ring-violet-500 dark:border-violet-700/50 dark:bg-violet-950/40 dark:text-violet-300 dark:hover:bg-violet-900/40',
              isSimple ? 'h-11 px-4 text-sm' : 'h-8 px-3 text-xs',
            )}
            aria-label={t('GlassEnclosure.Cutting.Nesting.OpenAdvanced', {
              defaultValue: 'Open advanced 2D nesting',
            })}
          >
            <Scissors size={isSimple ? 16 : 13} />
            {t('GlassEnclosure.Cutting.Nesting.AdvancedNesting', {
              defaultValue: 'Gelişmiş 2D Nesting',
            })}
          </button>
          <button
            type="button"
            onClick={onRecompute}
            disabled={isRecomputing}
            className={cn(
              'inline-flex items-center gap-1.5 rounded-md bg-blue-600 font-medium text-white transition hover:bg-blue-700 disabled:opacity-50 focus-visible:ring-2 focus-visible:ring-blue-500',
              isSimple ? 'h-11 px-4 text-sm' : 'h-8 px-3 text-xs',
            )}
            aria-label={t('GlassEnclosure.Quote.Recompute', { defaultValue: 'Recompute' })}
          >
            <RotateCw size={isSimple ? 16 : 13} className={isRecomputing ? 'animate-spin' : ''} />
            {isSimple
              ? t('GlassEnclosure.Designer.Shell.RecomputeSimple', {
                  defaultValue: 'Yenile',
                })
              : t('GlassEnclosure.Quote.Recompute', { defaultValue: 'Recompute' })}
          </button>
        </div>
      </header>

      <Modal
        open={nestingOpen}
        onClose={() => setNestingOpen(false)}
        size="2xl"
        title={t('GlassEnclosure.Cutting.Nesting.Title', { defaultValue: '2D Nesting' })}
        subtitle={t('GlassEnclosure.Cutting.Nesting.AdvancedSubtitle', {
          defaultValue: 'Optimize sheet layout to minimize waste',
        })}
        icon={<Scissors size={16} />}
      >
        <div className="flex flex-col gap-4">
          <div className="flex justify-end">
            <Optimize2DButton
              projectId={project.id}
              onOptimized={(report) => setNestingReport(report)}
            />
          </div>
          <Glass2DNestingViewer report={nestingReport} />
        </div>
      </Modal>

      <div className="flex-1 overflow-auto">
        <QuoteSummaryView
          project={project}
          bom={bom}
          isLoading={isLoading}
          onRecompute={() => {
            void onRecompute();
          }}
          isRecomputing={isRecomputing}
        />
      </div>
    </section>
  );
};

interface BadgeProps {
  label: string;
  value: string;
  isSimple: boolean;
  accent?: boolean;
}

const Badge = ({ label, value, isSimple, accent }: BadgeProps) => (
  <div
    className={cn(
      'flex items-center gap-1.5 rounded-md border px-2 py-1',
      accent
        ? 'border-emerald-300 bg-emerald-50 text-emerald-800 dark:border-emerald-700/40 dark:bg-emerald-950/30 dark:text-emerald-200'
        : 'border-slate-200 bg-white text-slate-700 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200',
    )}
  >
    <span
      className={cn(
        'uppercase tracking-wide text-slate-500 dark:text-slate-400',
        isSimple ? 'text-[10px]' : 'text-[9px]',
      )}
    >
      {label}
    </span>
    <span
      className={cn(
        'font-mono font-semibold',
        isSimple ? 'text-sm' : 'text-xs',
        accent && 'text-emerald-700 dark:text-emerald-300',
      )}
    >
      {value}
    </span>
  </div>
);

export default BOMPanel;
