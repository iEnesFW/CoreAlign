import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useDesignerStore } from '../model/designerStore';
import type { ProfileItemDto, ProfileSystemDto } from '../model/glassEnclosure.types';
import type { SceneConnectionState } from '../model/project.types';

interface RunConnectionInspectorProps {
  profileSystems: ProfileSystemDto[];
}

export function RunConnectionInspector({ profileSystems }: RunConnectionInspectorProps) {
  const selection = useDesignerStore((s) => s.selection);
  const connections = useDesignerStore((s) => s.scene.connections);
  const connection = useMemo(
    () => connections.find((c) => c.id === selection.connectionId),
    [connections, selection.connectionId],
  );
  if (!connection) return null;
  return (
    <ConnectionInspectorBody
      key={connection.id}
      connection={connection}
      profileSystems={profileSystems}
    />
  );
}

function ConnectionInspectorBody({
  connection,
  profileSystems,
}: {
  connection: SceneConnectionState;
  profileSystems: ProfileSystemDto[];
}) {
  const { t } = useTranslation();
  const runs = useDesignerStore((s) => s.scene.runs);
  const updateConnection = useDesignerStore((s) => s.updateConnection);
  const removeConnection = useDesignerStore((s) => s.removeConnection);
  const [draft, setDraft] = useState<SceneConnectionState>(connection);

  const runA = runs.find((r) => r.id === connection.runAId);
  const runB = runs.find((r) => r.id === connection.runBId);
  const cornerCandidates: ProfileItemDto[] = useMemo(() => {
    const seen = new Set<string>();
    const result: ProfileItemDto[] = [];
    for (const system of profileSystems) {
      for (const item of system.items) {
        if (item.role === 'Corner' && !seen.has(item.id)) {
          result.push(item);
          seen.add(item.id);
        }
      }
    }
    return result;
  }, [profileSystems]);

  const commit = (patch: Partial<SceneConnectionState>) => {
    setDraft({ ...draft, ...patch });
    updateConnection(connection.id, patch);
  };

  const suggestedMitre = draft.jointAngleDeg / 2;

  return (
    <section className="flex h-full flex-col gap-3 overflow-auto p-4">
      <header className="flex items-center justify-between">
        <h3 className="text-sm font-semibold uppercase tracking-wide text-slate-700 dark:text-slate-200">
          {t('GlassEnclosure.Designer.ConnectionInspector')}
        </h3>
        <button
          type="button"
          onClick={() => removeConnection(connection.id)}
          className="rounded border border-danger-500/40 px-2 py-1 text-xs text-danger-600 hover:bg-danger-50 dark:hover:bg-danger-950/30"
        >
          {t('Common.Delete')}
        </button>
      </header>

      <div className="rounded bg-slate-50 p-2 text-xs text-slate-700 dark:bg-slate-800 dark:text-slate-300">
        <div>
          <span className="font-medium">A:</span> {runA?.label ?? '—'}
        </div>
        <div>
          <span className="font-medium">B:</span> {runB?.label ?? '—'}
        </div>
      </div>

      <Field label={`${t('GlassEnclosure.Connection.JointAngle')} (°)`}>
        <input
          type="range"
          min={30}
          max={180}
          step={5}
          value={draft.jointAngleDeg}
          onChange={(e) =>
            commit({
              jointAngleDeg: Number(e.target.value),
              mitreCutDeg: Number(e.target.value) / 2,
            })
          }
          className="w-full"
        />
        <div className="text-xs text-slate-500">{draft.jointAngleDeg}°</div>
      </Field>

      <Field label={`${t('GlassEnclosure.Connection.MitreCut')} (°)`}>
        <input
          type="number"
          min={10}
          max={80}
          step={0.5}
          value={draft.mitreCutDeg}
          onChange={(e) => commit({ mitreCutDeg: Number(e.target.value) })}
          className={inputClass}
        />
        <div className="text-xs text-slate-500">
          {t('GlassEnclosure.Connection.SuggestedMitre', {
            value: suggestedMitre.toFixed(1),
            defaultValue: `Suggested ${suggestedMitre.toFixed(1)}°`,
          })}
        </div>
      </Field>

      <label className="flex items-center gap-2 text-sm text-slate-700 dark:text-slate-300">
        <input
          type="checkbox"
          checked={draft.usesCornerPost}
          onChange={(e) => commit({ usesCornerPost: e.target.checked })}
        />
        {t('GlassEnclosure.Connection.UseCornerPost')}
      </label>

      {draft.usesCornerPost && (
        <Field label={t('GlassEnclosure.Connection.CornerProfile')}>
          <select
            value={draft.cornerProfileId ?? ''}
            onChange={(e) => commit({ cornerProfileId: e.target.value || null })}
            className={inputClass}
          >
            <option value="">{t('GlassEnclosure.Connection.NoCornerProfile')}</option>
            {cornerCandidates.map((c) => (
              <option key={c.id} value={c.id}>
                {c.name}
              </option>
            ))}
          </select>
        </Field>
      )}

      {(draft.mitreCutDeg < 10 || draft.mitreCutDeg > 80) && (
        <div className="rounded border border-warning-500/60 bg-warning-50 p-2 text-xs text-warning-700 dark:border-warning-500/40 dark:bg-warning-950/30 dark:text-warning-300">
          {t('GlassEnclosure.Validation.ConnectionAngleInvalid')}
        </div>
      )}
    </section>
  );
}

const inputClass =
  'w-full rounded border border-slate-300 bg-white px-2 py-1 text-sm text-slate-900 focus:border-primary-500 focus:outline-none dark:border-slate-600 dark:bg-slate-900 dark:text-slate-100';

const Field = ({ label, children }: { label: string; children: React.ReactNode }) => (
  <label className="flex flex-col gap-1 text-sm text-slate-600 dark:text-slate-400">
    <span className="text-xs uppercase tracking-wide">{label}</span>
    {children}
  </label>
);
