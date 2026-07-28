import { isRealArc, radiusFromChordSweep } from './arcGeometry';
import { paneHalfSpanMm } from './paneSurface';
import type { PaneSurface } from './paneSurface';

const clampMm = (value: number, limit: number) => Math.min(limit, Math.max(-limit, value));

// The glass pane is shorter than the run by the top+bottom profile (60 mm), or a per-panel
// heightMm override. Hardware Y-offsets must clamp to THIS, not the run height — otherwise a piece
// can sit ~30mm above/below the actual glass.
const FRAME_HEIGHT_MM = 60;
export const glassClampHeightMm = (
  panelHeightMm: number | null | undefined,
  runHeightMm: number,
): number => panelHeightMm ?? Math.max(1, runHeightMm - FRAME_HEIGHT_MM);

// Every non-bent pane is drawn this much narrower than its stored width, for the cell joint.
const PANEL_JOINT_MM = 12;

export interface HardwareHostRun {
  lengthMm: number;
  geomArcRadiusMm?: number | null;
  geomArcSweepDeg?: number | null;
  arcGlassBent?: boolean;
}

/**
 * The pane width the renderer ACTUALLY draws.
 *
 * WHY: the clamp used the raw stored `panel.widthMm` while the renderer drew the pane 12 mm
 * narrower — so a piece snapped to the pane edge sat 6 mm off the glass on every straight run. On a
 * FACETED arc it is worse: `panel.widthMm` is the DEVELOPED length but the flat pane is drawn at the
 * CHORD, so the same snap landed 11 mm off. Clamp and render now measure the same pane.
 */
export const glassClampWidthMm = (panelWidthMm: number, run: HardwareHostRun): number => {
  const curved = isRealArc(run.geomArcRadiusMm, run.geomArcSweepDeg);
  // A BENT pane is the curved band itself — full developed width, no joint deduction.
  if (curved && run.arcGlassBent === true) return Math.max(1, panelWidthMm);
  if (!curved) return Math.max(1, panelWidthMm - PANEL_JOINT_MM);
  // Faceted arc: the pane is a FLAT chord across its own share of the sweep.
  const radiusMm = radiusFromChordSweep(run.lengthMm, run.geomArcRadiusMm, run.geomArcSweepDeg);
  const chordMm =
    radiusMm > 0
      ? 2 * radiusMm * Math.sin(Math.min(Math.PI, panelWidthMm / (2 * radiusMm)))
      : panelWidthMm;
  return Math.max(1, chordMm - PANEL_JOINT_MM);
};

export interface HardwarePlacementInput {
  offsetXmm: number;
  offsetYmm: number;
  widthMm: number;
  heightMm: number;
}

/** Clamp a piece so it stays entirely on the glass the renderer draws. */
export const clampHardwareOffsets = (
  drawnPaneWidthMm: number,
  glassHeightMm: number,
  item: HardwarePlacementInput,
): { offsetXmm: number; offsetYmm: number } => {
  const surface: PaneSurface = {
    widthMm: drawnPaneWidthMm,
    heightMm: glassHeightMm,
    thicknessMm: 0,
    baseYm: 0,
    curve: null,
  };
  const span = paneHalfSpanMm(surface, item.widthMm, item.heightMm);
  return {
    offsetXmm: clampMm(item.offsetXmm, span.uMm),
    offsetYmm: clampMm(item.offsetYmm, span.vMm),
  };
};
