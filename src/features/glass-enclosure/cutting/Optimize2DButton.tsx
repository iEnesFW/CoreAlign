import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Sparkles } from 'lucide-react';
import { safeRequestWithNotify } from '@/shared/lib/safeRequest';
import { useOptimize2DNestingMutation } from '../hooks/useGlassProjectQueries';
import type { Glass2DNestingReportDto, Optimize2DNestingInput } from '../model/engineering.types';

interface Optimize2DButtonProps {
  projectId: string;
  onOptimized?: (report: Glass2DNestingReportDto) => void;
  defaultHeuristic?: string;
  defaultGuillotine?: boolean;
}

const HEURISTICS = ['BestShortSideFit', 'BestAreaFit', 'BestLongSideFit', 'BottomLeft'] as const;

export function Optimize2DButton({
  projectId,
  onOptimized,
  defaultHeuristic = 'BestShortSideFit',
  defaultGuillotine = false,
}: Optimize2DButtonProps) {
  const { t } = useTranslation();
  const mutation = useOptimize2DNestingMutation();
  const [heuristic, setHeuristic] = useState<string>(defaultHeuristic);
  const [guillotine, setGuillotine] = useState<boolean>(defaultGuillotine);
  const [allowRotation, setAllowRotation] = useState<boolean>(true);
  const [open, setOpen] = useState<boolean>(false);

  const run = async () => {
    const input: Optimize2DNestingInput = {
      algorithm: 'MaxRects',
      heuristic,
      minimizeSheets: true,
      acceptableUtilization: 0.85,
      guillotineOnly: guillotine,
      allowRotation,
    };
    const [response] = await safeRequestWithNotify(mutation.mutateAsync({ id: projectId, input }), {
      successMessage: t('GlassEnclosure.Cutting.Nesting.OptimizeSuccess'),
      errorMessage: t('GlassEnclosure.Cutting.Nesting.OptimizeError'),
      showSuccessNotification: true,
    });
    if (response?.data && onOptimized) {
      onOptimized(response.data);
    }
    setOpen(false);
  };

  return (
    <div className="relative inline-block">
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        disabled={mutation.isPending}
        className="inline-flex items-center gap-1.5 rounded-md bg-violet-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-violet-700 disabled:opacity-50"
      >
        <Sparkles size={14} className={mutation.isPending ? 'animate-pulse' : ''} />
        {t('GlassEnclosure.Cutting.Nesting.OptimizeAdvanced')}
      </button>

      {open && (
        <div className="absolute right-0 z-20 mt-2 w-72 space-y-3 rounded-md border border-slate-200 bg-white p-3 shadow-lg dark:border-slate-700 dark:bg-slate-800">
          <label className="flex flex-col gap-1">
            <span className="text-xs font-medium text-slate-600 dark:text-slate-300">
              {t('GlassEnclosure.Cutting.Nesting.Heuristic')}
            </span>
            <select
              value={heuristic}
              onChange={(e) => setHeuristic(e.target.value)}
              className="rounded border border-slate-300 bg-white px-2 py-1 text-xs dark:border-slate-600 dark:bg-slate-700 dark:text-slate-100"
            >
              {HEURISTICS.map((h) => (
                <option key={h} value={h}>
                  {h}
                </option>
              ))}
            </select>
          </label>
          <label className="flex items-center gap-2 text-xs text-slate-700 dark:text-slate-200">
            <input
              type="checkbox"
              checked={guillotine}
              onChange={(e) => setGuillotine(e.target.checked)}
            />
            {t('GlassEnclosure.Cutting.Nesting.GuillotineOnly')}
          </label>
          <label className="flex items-center gap-2 text-xs text-slate-700 dark:text-slate-200">
            <input
              type="checkbox"
              checked={allowRotation}
              onChange={(e) => setAllowRotation(e.target.checked)}
            />
            {t('GlassEnclosure.Cutting.Nesting.AllowRotation')}
          </label>
          <button
            type="button"
            onClick={run}
            disabled={mutation.isPending}
            className="w-full rounded-md bg-violet-600 px-3 py-1.5 text-xs font-medium text-white hover:bg-violet-700 disabled:opacity-50"
          >
            {mutation.isPending
              ? t('GlassEnclosure.Cutting.Nesting.Optimizing')
              : t('GlassEnclosure.Cutting.Nesting.Run')}
          </button>
        </div>
      )}
    </div>
  );
}
