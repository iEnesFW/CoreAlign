import { useTranslation } from 'react-i18next';
import type { DashboardWidget } from '../model/bi.types';

interface Props {
  widgets: DashboardWidget[];
  renderWidget: (widget: DashboardWidget) => React.ReactNode;
  onRemove?: (widget: DashboardWidget) => void;
}

export const WidgetGrid = ({ widgets, renderWidget, onRemove }: Props) => {
  const { t } = useTranslation();
  if (widgets.length === 0) {
    return (
      <div className="flex h-64 items-center justify-center rounded-lg border border-dashed border-slate-300 text-slate-500 dark:border-slate-700 dark:text-slate-400">
        {t('BI.Dashboard.Empty', { defaultValue: 'No widgets yet — add your first widget.' })}
      </div>
    );
  }
  return (
    <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
      {widgets
        .slice()
        .sort((a, b) => a.displayOrder - b.displayOrder)
        .map((w) => (
          <div
            key={w.id}
            className="relative"
            style={{
              gridColumn: `span ${Math.min(Math.max(w.width, 1), 4)}`,
              minHeight: `${Math.max(w.height, 2) * 64}px`,
            }}
          >
            {renderWidget(w)}
            {onRemove ? (
              <button
                type="button"
                aria-label={t('BI.Dashboard.RemoveWidget', { defaultValue: 'Remove widget' })}
                onClick={() => onRemove(w)}
                className="absolute right-2 top-2 rounded-full bg-white/80 px-2 text-xs text-slate-500 hover:bg-red-50 hover:text-red-600 dark:bg-slate-800/80 dark:text-slate-400"
              >
                {'×'}
              </button>
            ) : null}
          </div>
        ))}
    </div>
  );
};
