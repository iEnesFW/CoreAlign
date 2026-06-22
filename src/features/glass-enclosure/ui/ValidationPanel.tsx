import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { AlertCircle, AlertTriangle, CheckCircle2, Info } from 'lucide-react';
import { useDesignerStore } from '../model/designerStore';
import type { GlassValidationSeverity } from '../model/project.types';

const SEVERITY_ICON: Record<GlassValidationSeverity, ReactNode> = {
  Error: <AlertCircle size={16} className="text-danger-600" />,
  Warning: <AlertTriangle size={16} className="text-warning-600" />,
  Info: <Info size={16} className="text-primary-600" />,
};

const SEVERITY_BG: Record<GlassValidationSeverity, string> = {
  Error: 'border-danger-500/60 bg-danger-50 dark:border-danger-500/40 dark:bg-danger-950/30',
  Warning: 'border-warning-500/60 bg-warning-50 dark:border-warning-500/40 dark:bg-warning-950/30',
  Info: 'border-primary-500/60 bg-primary-50 dark:border-primary-500/40 dark:bg-primary-950/30',
};

export function ValidationPanel() {
  const { t } = useTranslation();
  const findings = useDesignerStore((s) => s.validation);
  const setSelection = useDesignerStore((s) => s.setSelection);

  if (findings.length === 0) {
    return (
      <section className="flex items-center gap-2 rounded-md border border-success-500/60 bg-success-50 p-3 text-sm text-success-700 dark:border-success-500/40 dark:bg-success-950/30 dark:text-success-300">
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
