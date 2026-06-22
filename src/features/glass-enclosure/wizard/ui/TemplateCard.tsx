import { useTranslation } from 'react-i18next';
import { Layers, Ruler } from 'lucide-react';
import { cn } from '@/shared/lib/cn';
import { formatNumber } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import type { ProjectTemplateSummaryDto } from '../model/enclosure.types';

interface TemplateCardProps {
  template: ProjectTemplateSummaryDto;
  selected: boolean;
  estimatedMinM2?: number | null;
  estimatedMaxM2?: number | null;
  panelCount?: number | null;
  onSelect: (templateId: string) => void;
}

export const TemplateCard = ({
  template,
  selected,
  estimatedMinM2,
  estimatedMaxM2,
  panelCount,
  onSelect,
}: TemplateCardProps) => {
  const { t } = useTranslation();
  const locale = useFormatLocale();

  const m2Label =
    estimatedMinM2 !== null &&
    estimatedMinM2 !== undefined &&
    estimatedMaxM2 !== null &&
    estimatedMaxM2 !== undefined
      ? `${formatNumber(estimatedMinM2, locale, 0)}–${formatNumber(estimatedMaxM2, locale, 0)} m²`
      : null;

  return (
    <button
      type="button"
      onClick={() => onSelect(template.id)}
      className={cn(
        'group flex flex-col gap-2 rounded-xl border p-3 text-left transition-all',
        'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary-500',
        selected
          ? 'border-primary-500 bg-primary-50/60 ring-2 ring-primary-500/40 dark:bg-primary-500/10'
          : 'border-slate-200 bg-white hover:border-primary-300 hover:shadow-sm dark:border-slate-800 dark:bg-slate-900 dark:hover:border-primary-700',
      )}
    >
      <div
        className={cn(
          'flex h-24 w-full items-center justify-center overflow-hidden rounded-lg border border-slate-100 bg-slate-50',
          'dark:border-slate-800 dark:bg-slate-800',
        )}
      >
        {template.thumbnailUrl ? (
          <img
            src={template.thumbnailUrl}
            alt={t(template.displayNameKey, { defaultValue: template.code })}
            className="h-full w-full object-cover"
          />
        ) : (
          <Layers className="text-slate-300 dark:text-slate-600" size={32} />
        )}
      </div>
      <div className="space-y-1">
        <h4 className="line-clamp-2 text-sm font-semibold text-slate-900 dark:text-slate-100">
          {t(template.displayNameKey, { defaultValue: template.code })}
        </h4>
        <div className="flex flex-wrap items-center gap-1.5">
          <span className="inline-flex items-center gap-1 rounded-md bg-slate-100 px-1.5 py-0.5 text-[10px] font-medium text-slate-600 dark:bg-slate-800 dark:text-slate-300">
            <Layers size={10} />
            {t('GlassEnclosure.NewProjectWizard.Template.RunCount', {
              count: template.runPresetCount,
              defaultValue: '{{count}} run',
            })}
          </span>
          {m2Label && (
            <span className="inline-flex items-center gap-1 rounded-md bg-primary-100 px-1.5 py-0.5 text-[10px] font-medium text-primary-700 dark:bg-primary-500/20 dark:text-primary-300">
              <Ruler size={10} />
              {m2Label}
            </span>
          )}
          {panelCount !== null && panelCount !== undefined && (
            <span className="inline-flex items-center gap-1 rounded-md bg-success-100 px-1.5 py-0.5 text-[10px] font-medium text-success-700 dark:bg-success-500/20 dark:text-success-300">
              {t('GlassEnclosure.NewProjectWizard.Template.PanelCount', {
                count: panelCount,
                defaultValue: '{{count}} panel',
              })}
            </span>
          )}
        </div>
      </div>
    </button>
  );
};

export default TemplateCard;
