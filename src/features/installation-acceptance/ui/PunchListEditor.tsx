import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { AlertTriangle, CheckCircle2, Plus } from 'lucide-react';
import type {
  AddPunchListItemInput,
  PunchListItem,
  PunchListSeverity,
  ResolvePunchListItemInput,
} from '../model/installationAcceptance.types';

interface Props {
  acceptanceId: string;
  items: PunchListItem[];
  onAdd: (input: AddPunchListItemInput) => void;
  onResolve: (input: ResolvePunchListItemInput) => void;
  disabled?: boolean;
}

const SEVERITIES: PunchListSeverity[] = ['Minor', 'Moderate', 'Critical'];

const SEVERITY_TONE: Record<PunchListSeverity, string> = {
  Minor: 'bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-200',
  Moderate: 'bg-warning-100 text-warning-700 dark:bg-warning-500/20 dark:text-warning-200',
  Critical: 'bg-danger-100 text-danger-700 dark:bg-danger-500/20 dark:text-danger-200',
};

export const PunchListEditor = ({ acceptanceId, items, onAdd, onResolve, disabled }: Props) => {
  const { t } = useTranslation();
  const [description, setDescription] = useState<string>('');
  const [severity, setSeverity] = useState<PunchListSeverity>('Minor');

  const submit = () => {
    if (!description.trim()) return;
    onAdd({ acceptanceId, description: description.trim(), severity });
    setDescription('');
    setSeverity('Minor');
  };

  return (
    <div className="flex flex-col gap-3">
      <ul className="flex flex-col gap-2">
        {items.length === 0 && (
          <li className="rounded border border-dashed border-slate-300 px-3 py-3 text-sm text-slate-500 dark:border-slate-700 dark:text-slate-400">
            {t('InstallationAcceptance.PunchList.Empty')}
          </li>
        )}
        {items.map((item) => (
          <li
            key={item.id}
            className="flex items-start justify-between gap-3 rounded border border-slate-200 bg-white px-3 py-2 dark:border-slate-700 dark:bg-slate-900"
          >
            <div className="flex flex-1 flex-col gap-1">
              <div className="flex items-center gap-2">
                <AlertTriangle className="size-4 text-warning-500" />
                <span className="text-sm text-slate-700 dark:text-slate-200">
                  {item.description}
                </span>
              </div>
              <div className="flex items-center gap-2 text-xs">
                <span className={`rounded px-2 py-0.5 ${SEVERITY_TONE[item.severity]}`}>
                  {t(`InstallationAcceptance.PunchList.Severity.${item.severity}`)}
                </span>
                <span className="text-slate-500 dark:text-slate-400">
                  {t(`InstallationAcceptance.PunchList.Status.${item.status}`)}
                </span>
              </div>
            </div>
            {item.status !== 'Resolved' && (
              <button
                type="button"
                onClick={() => onResolve({ punchItemId: item.id, resolutionNotes: null })}
                disabled={disabled}
                className="flex items-center gap-1 rounded border border-success-300 px-2 py-1 text-xs text-success-700 disabled:opacity-50 dark:border-success-700 dark:text-success-300"
              >
                <CheckCircle2 className="size-3.5" />
                {t('InstallationAcceptance.PunchList.Resolve')}
              </button>
            )}
          </li>
        ))}
      </ul>

      <div className="flex flex-col gap-2 rounded border border-slate-200 bg-slate-50 p-3 dark:border-slate-700 dark:bg-slate-800/40">
        <input
          type="text"
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          placeholder={t('InstallationAcceptance.PunchList.DescriptionPlaceholder')}
          disabled={disabled}
          className="w-full rounded border border-slate-300 bg-white px-3 py-2 text-sm dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
        />
        <div className="flex flex-wrap gap-2">
          {SEVERITIES.map((s) => (
            <button
              key={s}
              type="button"
              onClick={() => setSeverity(s)}
              disabled={disabled}
              className={`rounded px-3 py-1.5 text-xs ${
                severity === s
                  ? 'border border-primary-500 bg-primary-50 text-primary-700 dark:border-primary-400 dark:bg-primary-500/20 dark:text-primary-200'
                  : `border border-slate-200 ${SEVERITY_TONE[s]}`
              } disabled:opacity-50`}
            >
              {t(`InstallationAcceptance.PunchList.Severity.${s}`)}
            </button>
          ))}
        </div>
        <button
          type="button"
          onClick={submit}
          disabled={disabled || !description.trim()}
          className="flex items-center justify-center gap-1 rounded bg-primary-600 px-3 py-2 text-sm font-medium text-white disabled:opacity-50"
        >
          <Plus className="size-4" />
          {t('InstallationAcceptance.PunchList.Add')}
        </button>
      </div>
    </div>
  );
};
