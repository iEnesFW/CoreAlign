import { MIN_PANEL_MM } from './panelResize';
import { developedLengthMm, isRealArc } from './arcGeometry';
import { refitPanelShape } from './panelShapeOutline';
import { notifyPanelOutlineRejected } from './panelOutlineFeedback';
import type { ScenePanelState, SceneRunState } from './project.types';

/**
 * The ONE writer for a run's panel span.
 *
 * WHY it lives outside the store: the multi-align toolbar built its own patched run and split the
 * widths over the CHORD, while the store split them over the DEVELOPED length (radius·sweep) — so
 * equalising the length of an arc run wrote panel widths ~4.5% short and persisted them to the
 * server (BOM and the cutting list then under-measured the glass). Two writers for one invariant
 * is the split-brain this module exists to prevent.
 */
export const distributePanelWidths = (
  panels: ScenePanelState[],
  lengthMm: number,
  runHeightMm?: number,
): ScenePanelState[] => {
  const count = panels.length;
  if (count === 0) return panels;
  // WHY the refit rides along: a redistribution changes a pane's BOX, and a shaped pane's stored
  // outline must be re-clamped into it or the server-side box validator refuses the persist.
  const apply = (panel: ScenePanelState, widthMm: number): ScenePanelState => {
    const moved = panel.widthMm === widthMm ? panel : { ...panel, widthMm };
    if (runHeightMm === undefined) return moved;
    const refit = refitPanelShape(moved, widthMm, moved.heightMm ?? runHeightMm);
    if (!refit) return moved;
    if (refit.rejection) notifyPanelOutlineRejected(refit.rejection);
    return { ...moved, shapeKind: refit.shapeKind, shapePointsJson: refit.shapePointsJson };
  };
  if (lengthMm <= count * MIN_PANEL_MM) {
    return panels.map((panel) => apply(panel, MIN_PANEL_MM));
  }
  const rawTotal = panels.reduce((sum, panel) => sum + panel.widthMm, 0);
  const widths = panels.map((panel, index) => {
    if (index === count - 1) return 0;
    const share = rawTotal > 0 ? panel.widthMm / rawTotal : 1 / count;
    return Math.max(MIN_PANEL_MM, Math.round(share * lengthMm));
  });
  widths[count - 1] = lengthMm - widths.reduce((a, b) => a + b, 0);
  while (widths[count - 1] < MIN_PANEL_MM) {
    let widest = 0;
    for (let i = 1; i < count - 1; i += 1) if (widths[i] > widths[widest]) widest = i;
    const take = Math.min(MIN_PANEL_MM - widths[count - 1], widths[widest] - MIN_PANEL_MM);
    if (take <= 0) break;
    widths[widest] -= take;
    widths[count - 1] += take;
  }
  return panels.map((panel, index) => apply(panel, widths[index]));
};

// Σ panel widths = the DEVELOPED length (physical glass) — for an arc run that's radius·sweep,
// for a straight run the length itself. The run passed here must already carry the arc fields
// the widths should follow (i.e. call AFTER merging a patch).
export const runPanelTargetMm = (run: SceneRunState): number =>
  Math.max(
    developedLengthMm(run.lengthMm, run.geomArcRadiusMm, run.geomArcSweepDeg),
    run.panels.length * MIN_PANEL_MM,
  );

export const withClampedRunLength = (run: SceneRunState, lengthMm: number): SceneRunState => {
  // Panels bound the DEVELOPED length, not the chord: on an arc run a legitimate panel count can
  // exceed chord/MIN (the glass lives on radius·sweep), so clamping the CHORD against the panel
  // count would corrupt chord = 2r·sin(sweep/2) on every commit. Straight runs keep the old rule
  // (there chord IS the panel span).
  const floorMm = isRealArc(run.geomArcRadiusMm, run.geomArcSweepDeg)
    ? MIN_PANEL_MM
    : run.panels.length * MIN_PANEL_MM;
  const clamped = Math.max(floorMm, Math.round(lengthMm));
  const next = { ...run, lengthMm: clamped };
  return {
    ...next,
    panels: distributePanelWidths(next.panels, runPanelTargetMm(next), next.heightMm),
  };
};
