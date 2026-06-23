import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useDesignerStore } from '../model/designerStore';
import { useRunEntityActions } from '../hooks/useDesignerEntityActions';
import {
  deriveArcFromChordSagitta,
  deriveArcFromRadius,
  deriveArcFromSweep,
  facetJointAngleDeg,
} from '../model/arcGeometry';
import type { ScenePanelState, SceneRunState } from '../model/project.types';

interface RunArcSectionProps {
  draft: SceneRunState;
  panels: ScenePanelState[];
  minRadius: number;
  onDraftRadius: (value: number | null) => void;
  commit: (patch: Partial<SceneRunState>) => void;
}

const WARN_MIN_RADIUS_MM = 1500;
const WARN_JOINT_ANGLE_DEG = 10;

export function RunArcSection({
  draft,
  panels,
  minRadius,
  onDraftRadius,
  commit,
}: RunArcSectionProps) {
  const { t } = useTranslation();
  const setRunGlassBent = useDesignerStore((s) => s.setRunGlassBent);
  const { persistRun } = useRunEntityActions();
  const [sweepDraft, setSweepDraft] = useState('');
  const [chordDraft, setChordDraft] = useState('');
  const [sagittaDraft, setSagittaDraft] = useState('');

  const radius = draft.geomArcRadiusMm ?? 0;
  const isArc = radius > 0;
  const derived = isArc ? deriveArcFromRadius(draft.lengthMm, radius) : null;
  const jointAngle = derived ? facetJointAngleDeg(derived.sweepDeg, panels.length) : 0;
  const hasSlidingPanels = panels.some(
    (p) => p.openingType === 'SlidingLeft' || p.openingType === 'SlidingRight',
  );

  // Turning a straight run into an arc auto-enables bent glass so the panes follow the
  // curve (the expected look); already-curved runs keep the user's bent/faceted choice.
  const bentOnArc = (): { arcGlassBent?: boolean } => (isArc ? {} : { arcGlassBent: true });

  const commitRadius = (raw: number) => {
    if (raw > 0) {
      const next = deriveArcFromRadius(draft.lengthMm, Math.max(minRadius, raw));
      onDraftRadius(next.radiusMm);
      commit({
        geomArcRadiusMm: next.radiusMm,
        geomArcSweepDeg: Math.round(next.sweepDeg * 10) / 10,
        ...bentOnArc(),
      });
    } else {
      commit({ geomArcRadiusMm: null, geomArcSweepDeg: null });
    }
  };

  const commitSweep = () => {
    const deg = Number(sweepDraft);
    if (!deg || deg <= 0) return;
    const next = deriveArcFromSweep(draft.lengthMm, deg);
    onDraftRadius(next.radiusMm);
    commit({
      geomArcRadiusMm: next.radiusMm,
      geomArcSweepDeg: Math.round(next.sweepDeg * 10) / 10,
      ...bentOnArc(),
    });
    setSweepDraft('');
  };

  const applyChordSagitta = () => {
    const chord = Number(chordDraft);
    const sagitta = Number(sagittaDraft);
    if (!chord || !sagitta || chord <= 0 || sagitta <= 0 || sagitta * 2 > chord) return;
    const next = deriveArcFromChordSagitta(chord, sagitta);
    onDraftRadius(next.radiusMm);
    commit({
      lengthMm: next.arcLengthMm,
      geomArcRadiusMm: next.radiusMm,
      geomArcSweepDeg: Math.round(next.sweepDeg * 10) / 10,
      ...bentOnArc(),
    });
    setChordDraft('');
    setSagittaDraft('');
  };

  return (
    <div className="space-y-2 rounded-md border border-slate-200 p-2.5 dark:border-slate-700">
      <p className="text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">
        {t('GlassEnclosure.Designer.Arc.Title', { defaultValue: 'Kavis (arc)' })}
      </p>

      <div className="grid grid-cols-2 gap-2">
        <Field label={t('GlassEnclosure.Field.ArcRadius', { defaultValue: 'Yarıçap (mm)' })}>
          <input
            type="number"
            min={0}
            max={50000}
            step={100}
            value={draft.geomArcRadiusMm ?? 0}
            onChange={(e) => onDraftRadius(Number(e.target.value) || null)}
            onBlur={() => commitRadius(draft.geomArcRadiusMm ?? 0)}
            className={inputClass}
          />
        </Field>
        <Field
          label={t('GlassEnclosure.Designer.Arc.SweepInput', { defaultValue: 'Yay açısı (°)' })}
        >
          <input
            type="number"
            min={1}
            max={180}
            step={1}
            value={sweepDraft}
            placeholder={derived ? derived.sweepDeg.toFixed(1) : '0'}
            onChange={(e) => setSweepDraft(e.target.value)}
            onBlur={commitSweep}
            className={inputClass}
          />
        </Field>
      </div>

      <div className="rounded bg-slate-50 p-2 dark:bg-slate-800/60">
        <p className="text-[10px] font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400">
          {t('GlassEnclosure.Designer.Arc.SurveyTitle', {
            defaultValue: 'Saha ölçüsünden (kiriş + ok yüksekliği)',
          })}
        </p>
        <div className="mt-1 flex items-end gap-2">
          <Field label={t('GlassEnclosure.Designer.Arc.Chord', { defaultValue: 'Kiriş (mm)' })}>
            <input
              type="number"
              min={100}
              value={chordDraft}
              onChange={(e) => setChordDraft(e.target.value)}
              className={inputClass}
            />
          </Field>
          <Field label={t('GlassEnclosure.Designer.Arc.Sagitta', { defaultValue: 'Ok (mm)' })}>
            <input
              type="number"
              min={1}
              value={sagittaDraft}
              onChange={(e) => setSagittaDraft(e.target.value)}
              className={inputClass}
            />
          </Field>
          <button
            type="button"
            onClick={applyChordSagitta}
            className="h-[30px] shrink-0 rounded bg-primary-600 px-3 text-xs font-medium text-white hover:bg-primary-700"
          >
            {t('GlassEnclosure.Designer.Arc.Apply', { defaultValue: 'Uygula' })}
          </button>
        </div>
      </div>

      {derived ? (
        <p className="text-[11px] text-slate-500 dark:text-slate-400">
          {t('GlassEnclosure.Designer.Arc.DerivedInfo', {
            defaultValue:
              'R{{r}} · {{deg}}° · kiriş {{chord}} mm · ok {{sagitta}} mm · eklem {{joint}}°',
            r: derived.radiusMm,
            deg: derived.sweepDeg.toFixed(1),
            chord: derived.chordMm,
            sagitta: derived.sagittaMm,
            joint: jointAngle.toFixed(1),
          })}
        </p>
      ) : (
        <p className="text-[11px] text-slate-400">
          {t('GlassEnclosure.Designer.ArcStraightInfo', {
            defaultValue: '0 = düz hat; kavis için yarıçap, açı veya kiriş+ok girin.',
          })}
          {' · '}
          {t('GlassEnclosure.Designer.ArcMinRadiusInfo', {
            defaultValue: 'Min. yarıçap {{min}} mm',
            min: minRadius,
          })}
        </p>
      )}

      {isArc && (
        <label className="flex items-center gap-2 text-sm text-slate-600 dark:text-slate-300">
          <input
            type="checkbox"
            checked={draft.arcGlassBent ?? false}
            onChange={(e) => {
              setRunGlassBent(draft.id, e.target.checked);
              void persistRun({ ...draft, arcGlassBent: e.target.checked });
            }}
          />
          {t('GlassEnclosure.Designer.Arc.BentGlass', {
            defaultValue: 'Bombeli cam (özel üretim, ~2.5-3× maliyet)',
          })}
        </label>
      )}

      {isArc && derived && (
        <div className="space-y-1">
          {derived.radiusMm < WARN_MIN_RADIUS_MM && (
            <Warning
              text={t('GlassEnclosure.Designer.Arc.WarnTightRadius', {
                defaultValue:
                  'Dar yarıçap ({{r}} mm) — sistem üreticisinin minimumunu doğrulayın (tipik ≥1500 mm).',
                r: derived.radiusMm,
              })}
            />
          )}
          {!draft.arcGlassBent && jointAngle > WARN_JOINT_ANGLE_DEG && (
            <Warning
              text={t('GlassEnclosure.Designer.Arc.WarnJointAngle', {
                defaultValue:
                  'Eklem açısı {{deg}}° — faseta için panel sayısını artırın (önerilen ≤10°).',
                deg: jointAngle.toFixed(1),
              })}
            />
          )}
          {hasSlidingPanels && !draft.arcGlassBent && (
            <Warning
              text={t('GlassEnclosure.Designer.Arc.WarnSliding', {
                defaultValue:
                  'Kayar paneller kavisli rayda düz camla çalışmaz — bombeli cam modunu açın veya açılım tipini değiştirin.',
              })}
            />
          )}
        </div>
      )}
    </div>
  );
}

const inputClass =
  'w-full rounded border border-slate-300 bg-white px-2 py-1 text-sm text-slate-900 focus:border-primary-500 focus:outline-none dark:border-slate-600 dark:bg-slate-900 dark:text-slate-100';

const Field = ({ label, children }: { label: string; children: React.ReactNode }) => (
  <label className="flex min-w-0 flex-1 flex-col gap-1 text-sm text-slate-600 dark:text-slate-400">
    <span className="text-[10px] uppercase tracking-wide">{label}</span>
    {children}
  </label>
);

const Warning = ({ text }: { text: string }) => (
  <p className="rounded border border-warning-300 bg-warning-50 px-2 py-1 text-[11px] text-warning-800 dark:border-warning-700 dark:bg-warning-950/30 dark:text-warning-300">
    {text}
  </p>
);
