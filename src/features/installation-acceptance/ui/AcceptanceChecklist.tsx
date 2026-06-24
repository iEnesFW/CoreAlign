import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Check, ChevronDown, ChevronUp, Minus, X } from 'lucide-react';
import type {
  ChecklistCategoryEntry,
  InstallationChecklistResult,
} from '../model/installationAcceptance.types';

interface Props {
  checklistJson: string;
  onItemChange: (
    category: string,
    itemKey: string,
    result: InstallationChecklistResult,
    notes: string | null,
  ) => void;
  disabled?: boolean;
}

const RESULT_OPTIONS: {
  value: InstallationChecklistResult;
  key: string;
  icon: React.ComponentType<{ className?: string }>;
}[] = [
  { value: 'Pass', key: 'InstallationAcceptance.Checklist.Result.Pass', icon: Check },
  { value: 'Fail', key: 'InstallationAcceptance.Checklist.Result.Fail', icon: X },
  { value: 'NotApplicable', key: 'InstallationAcceptance.Checklist.Result.NA', icon: Minus },
];

const RESULT_TONE: Record<InstallationChecklistResult, string> = {
  NotEvaluated: 'bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-300',
  Pass: 'bg-success-100 text-success-700 dark:bg-success-500/20 dark:text-success-300',
  Fail: 'bg-danger-100 text-danger-700 dark:bg-danger-500/20 dark:text-danger-300',
  NotApplicable: 'bg-slate-200 text-slate-600 dark:bg-slate-700 dark:text-slate-300',
};

export const AcceptanceChecklist = ({ checklistJson, onItemChange, disabled }: Props) => {
  const { t } = useTranslation();
  const checklist = useMemo<ChecklistCategoryEntry[]>(() => {
    try {
      return JSON.parse(checklistJson) as ChecklistCategoryEntry[];
    } catch {
      return [];
    }
  }, [checklistJson]);

  const [expanded, setExpanded] = useState<Record<string, boolean>>({});

  const toggle = (category: string) => setExpanded((p) => ({ ...p, [category]: !p[category] }));

  return (
    <div className="flex flex-col gap-3">
      {checklist.map((cat) => {
        const isOpen = expanded[cat.category] ?? true;
        const passed = cat.items.filter((i) => i.result === 'Pass').length;
        return (
          <section
            key={cat.category}
            className="rounded-lg border border-slate-200 bg-white shadow-sm dark:border-slate-700 dark:bg-slate-900"
          >
            <button
              type="button"
              onClick={() => toggle(cat.category)}
              className="flex w-full items-center justify-between px-4 py-3 text-left"
            >
              <span className="font-semibold text-slate-800 dark:text-slate-100">
                {t(`InstallationAcceptance.Checklist.Category.${cat.category}`)}
              </span>
              <span className="flex items-center gap-2 text-sm text-slate-500 dark:text-slate-400">
                {passed}/{cat.items.length}
                {isOpen ? <ChevronUp className="size-4" /> : <ChevronDown className="size-4" />}
              </span>
            </button>
            {isOpen && (
              <ul className="divide-y divide-slate-100 border-t border-slate-100 dark:divide-slate-800 dark:border-slate-800">
                {cat.items.map((item) => (
                  <li key={item.key} className="flex flex-col gap-2 px-4 py-3">
                    <div className="flex items-start justify-between gap-3">
                      <span className="text-sm text-slate-700 dark:text-slate-200">
                        {t(`InstallationAcceptance.Checklist.Item.${item.key}`)}
                      </span>
                      <span className={`rounded px-2 py-0.5 text-xs ${RESULT_TONE[item.result]}`}>
                        {t(
                          `InstallationAcceptance.Checklist.Result.${item.result === 'NotApplicable' ? 'NA' : item.result}`,
                        )}
                      </span>
                    </div>
                    <div className="flex flex-wrap gap-2">
                      {RESULT_OPTIONS.map((opt) => {
                        const Icon = opt.icon;
                        const active = item.result === opt.value;
                        return (
                          <button
                            key={opt.value}
                            type="button"
                            disabled={disabled}
                            onClick={() =>
                              onItemChange(cat.category, item.key, opt.value, item.notes ?? null)
                            }
                            className={`flex items-center gap-1 rounded border px-3 py-1.5 text-xs ${
                              active
                                ? 'border-primary-500 bg-primary-50 text-primary-700 dark:border-primary-400 dark:bg-primary-500/20 dark:text-primary-200'
                                : 'border-slate-200 bg-white text-slate-600 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-300'
                            } disabled:opacity-50`}
                          >
                            <Icon className="size-3.5" />
                            {t(opt.key)}
                          </button>
                        );
                      })}
                    </div>
                    <input
                      type="text"
                      defaultValue={item.notes ?? ''}
                      disabled={disabled}
                      placeholder={t('InstallationAcceptance.Checklist.NotesPlaceholder')}
                      onBlur={(e) =>
                        onItemChange(cat.category, item.key, item.result, e.target.value || null)
                      }
                      className="w-full rounded border border-slate-200 bg-white px-2 py-1 text-xs text-slate-700 placeholder:text-slate-400 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-200 dark:placeholder:text-slate-500"
                    />
                  </li>
                ))}
              </ul>
            )}
          </section>
        );
      })}
    </div>
  );
};
