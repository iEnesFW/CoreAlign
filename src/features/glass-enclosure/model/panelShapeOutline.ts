import { polygonAreaMm2, polygonSelfIntersects, polygonSignedAreaMm2 } from './polygonValidation';
import { parsePanelPolygonPoints, serializePanelPolygonPoints } from './panelPolygon';
import type { PanelPoint } from './panelOutline';

/**
 * The ONE gate a shaped glass panel's outline passes through before it is written.
 *
 * WHY it has to exist: the panel-shape path had no validation at all, while the wall/slab free-draw
 * path already refused a self-intersecting stroke. A bowtie outline is not a cosmetic problem here:
 *  - earcut turns a pinched contour into non-manifold glass;
 *  - the shoelace area SIGNS CANCEL across the two lobes, so `panelPolygonAreaMm2` under-reports the
 *    silhouette — and that number is the one the BOM prices, the technical summary weighs, and the
 *    cut list orders from. A pane the fabricator cuts to a smaller area than the customer is
 *    charged for (or the other way round) is a money defect, not a rendering one.
 * So every producer — the vertex editor, the presets, an imported project, and any future pen
 * stroke — comes through here, and the render, the cut list, the nesting blank and the DXF all read
 * the outline it approved.
 *
 * Panel-local coordinates: bottom-centred, y-up. x ∈ [-w/2, w/2], y ∈ [0, h].
 */

export type PanelOutlineRejection =
  | 'tooFewPoints'
  | 'selfIntersecting'
  | 'degenerate'
  | 'unparsable';

export interface PanelOutlineResult {
  points: PanelPoint[] | null;
  rejection: PanelOutlineRejection | null;
}

// Two vertices closer than this are the same click; below this the edge between them carries no
// geometry but can still make the crossing test report a false pinch.
const MIN_VERTEX_GAP_MM = 1;

// A silhouette thinner than this is a sliver the cutter cannot make and the nester cannot place.
const MIN_AREA_MM2 = 10_000; // 100 x 100 mm

const clamp = (value: number, lo: number, hi: number) => Math.min(hi, Math.max(lo, value));

const dropRepeats = (points: PanelPoint[]): PanelPoint[] => {
  const out: PanelPoint[] = [];
  for (const p of points) {
    const prev = out[out.length - 1];
    if (prev && Math.hypot(p.x - prev.x, p.y - prev.y) < MIN_VERTEX_GAP_MM) continue;
    out.push(p);
  }
  // The contour is implicitly closed, so the last vertex must not repeat the first either.
  while (out.length >= 2) {
    const first = out[0];
    const last = out[out.length - 1];
    if (Math.hypot(last.x - first.x, last.y - first.y) >= MIN_VERTEX_GAP_MM) break;
    out.pop();
  }
  return out;
};

/**
 * Clamp into the panel box, drop duplicates, reject anything unusable, and return a CCW contour.
 *
 * WHY the winding is forced: earcut, the DXF writer and the nesting blank each infer orientation
 * independently, and a clockwise contour flips a normal in one of them while the others carry on —
 * the glass renders inside-out or the cut path runs backwards. One agreed direction removes the
 * question entirely.
 */
export const normalizePanelOutline = (
  points: readonly PanelPoint[] | null | undefined,
  widthMm: number,
  heightMm: number,
): PanelOutlineResult => {
  if (!points) return { points: null, rejection: 'unparsable' };

  const halfW = Math.max(1, widthMm) / 2;
  const maxY = Math.max(1, heightMm);
  const clamped = points
    .filter((p) => Number.isFinite(p.x) && Number.isFinite(p.y))
    .map((p) => ({
      x: Math.round(clamp(p.x, -halfW, halfW)),
      y: Math.round(clamp(p.y, 0, maxY)),
    }));

  const deduped = dropRepeats(clamped);
  if (deduped.length < 3) return { points: null, rejection: 'tooFewPoints' };
  // Clamping can push two distinct vertices onto the same box edge and make them coincide, so the
  // crossing test runs on the FINAL contour, never on the raw input.
  if (polygonSelfIntersects(deduped)) return { points: null, rejection: 'selfIntersecting' };
  if (polygonAreaMm2(deduped) < MIN_AREA_MM2) return { points: null, rejection: 'degenerate' };

  const ccw = polygonSignedAreaMm2(deduped) < 0 ? [...deduped].reverse() : deduped;
  return { points: ccw, rejection: null };
};

/** Same gate, straight from/to the persisted JSON — what the store and the editors call. */
export const normalizePanelOutlineJson = (
  json: string | null | undefined,
  widthMm: number,
  heightMm: number,
): { json: string | null; rejection: PanelOutlineRejection | null } => {
  const parsed = parsePanelPolygonPoints(json);
  if (!parsed) return { json: null, rejection: 'unparsable' };
  const result = normalizePanelOutline(parsed, widthMm, heightMm);
  return {
    json: result.points ? serializePanelPolygonPoints(result.points) : null,
    rejection: result.rejection,
  };
};
