import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { AlertCircle, AlertTriangle, CheckCircle2, Info } from 'lucide-react';
import { useDesignerStore } from '../model/designerStore';
import type { GlassValidationSeverity } from '../model/project.types';

const SEVERITY_ICON: Record<GlassValidationSeverity, ReactNode> = {
  Error: <AlertCircle size={16} className="text-red-600" />,
  Warning: <AlertTriangle size={16} className="text-amber-600" />,
  Info: <Info size={16} className="text-blue-600" />,
};

const SEVERITY_BG: Record<GlassValidationSeverity, string> = {
  Error: 'border-red-500/60 bg-red-50 dark:border-red-500/40 dark:bg-red-950/30',
  Warning: 'border-amber-500/60 bg-amber-50 dark:border-amber-500/40 dark:bg-amber-950/30',
  Info: 'border-blue-500/60 bg-blue-50 dark:border-blue-500/40 dark:bg-blue-950/30',
};

export function ValidationPanel() {
  const { t } = useTranslation();
  const findings = useDesignerStore((s) => s.validation);
  const setSelection = useDesignerStore((s) => s.setSelection);

  if (findings.length === 0) {
    return (
      <section className="flex items-center gap-2 rounded-md border border-emerald-500/60 bg-emerald-50 p-3 text-sm text-emerald-700 dark:border-emerald-500/40 dark:bg-emerald-950/30 dark:text-emerald-300">
        <CheckCircle2 size={16} />
        {t('GlassEnclosure.Validation.AllGood')}
      </section>
    );
  }

  const grouped = {
    Error: findings.filter((f) => f.severity === 'Error'),
    Warning: findings.filter((f) => f.severity === 'Warning'),
    Info: findings.filter((f) => f.severity === 'Info'),
  };

  return (
    <section className="flex flex-col gap-2">
      {(['Error', 'Warning', 'Info'] as const).map((severity) => {
        const items = grouped[severity];
        if (items.length === 0) return null;
        return (
          <div key={severity} className="space-y-1.5">
            {items.map((finding, idx) => (
              <button
                key={`${finding.code}-${idx}`}
                type="button"
                onClick={() => {
                  if (finding.affectedPanelId && finding.affectedRunId) {
                    setSelection({
                      kind: 'panel',
                      runId: finding.affectedRunId,
                      panelId: finding.affectedPanelId,
                      connectionId: null,
                    });
                  } else if (finding.affectedRunId) {
                    setSelection({
                      kind: 'run',
                      runId: finding.affectedRunId,
                      panelId: null,
                      connectionId: null,
                    });
                  }
                }}
                className={`flex w-full items-start gap-2 rounded-md border p-2 text-left text-xs hover:opacity-90 ${SEVERITY_BG[severity]}`}
              >
                <span className="mt-0.5">{SEVERITY_ICON[severity]}</span>
                <span className="flex-1 text-slate-700 dark:text-slate-200">
                  <span className="font-semibold">
                    {t(finding.messageKey as never, { defaultValue: finding.code })}
                  </span>
                  {finding.messageArgs && (
                    <span className="ml-1 font-mono text-slate-500">[{finding.messageArgs}]</span>
                  )}
                </span>
              </button>
            ))}
          </div>
        );
      })}
    </section>
  );
}
