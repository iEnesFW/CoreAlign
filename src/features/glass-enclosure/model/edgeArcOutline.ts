import { bowArcPlanPoints } from './arcGeometry';
import type { EdgeArcKey, EdgeArcMap } from './project.types';
import type { Point2D } from './polygonValidation';

export type { EdgeArcKey, EdgeArcMap };

const MIN_SAGITTA_MM = 1;
const EDGE_SEGMENTS = 24;

interface RectEdge {
  key: EdgeArcKey;
  from: (widthMm: number, depthMm: number) => Point2D;
  to: (widthMm: number, depthMm: number) => Point2D;
}

const RECT_EDGES: RectEdge[] = [
  { key: 'front', from: () => ({ x: 0, y: 0 }), to: (w) => ({ x: w, y: 0 }) },
  { key: 'right', from: (w) => ({ x: w, y: 0 }), to: (w, d) => ({ x: w, y: d }) },
  { key: 'back', from: (w, d) => ({ x: w, y: d }), to: (_w, d) => ({ x: 0, y: d }) },
  { key: 'left', from: (_w, d) => ({ x: 0, y: d }), to: () => ({ x: 0, y: 0 }) },
];

export const hasEdgeArc = (edgeArc: EdgeArcMap | null | undefined): boolean => {
  if (!edgeArc) return false;
  return RECT_EDGES.some((edge) => {
    const s = edgeArc[edge.key];
    return typeof s === 'number' && Math.abs(s) >= MIN_SAGITTA_MM;
  });
};

export const edgeArcOutline = (
  widthMm: number,
  depthMm: number,
  edgeArc: EdgeArcMap,
  segments = EDGE_SEGMENTS,
): Point2D[] => {
  if (!(widthMm > 0) || !(depthMm > 0)) return [];
  const out: Point2D[] = [];
  for (const edge of RECT_EDGES) {
    const a = edge.from(widthMm, depthMm);
    const b = edge.to(widthMm, depthMm);
    const sagitta = edgeArc[edge.key];
    const poly =
      typeof sagitta === 'number' && Math.abs(sagitta) >= MIN_SAGITTA_MM
        ? bowArcPlanPoints(a.x, a.y, b.x, b.y, -sagitta, segments)
        : [a, b];
    for (let i = 0; i < poly.length - 1; i += 1) out.push({ x: poly[i].x, y: poly[i].y });
  }
  return out;
};

export const hasPolygonEdgeArc = (
  edgeArcs: ReadonlyArray<number | null | undefined> | null | undefined,
): boolean =>
  !!edgeArcs && edgeArcs.some((s) => typeof s === 'number' && Math.abs(s) >= MIN_SAGITTA_MM);

export const bowedPolygonOutline = (
  points: readonly Point2D[],
  edgeArcs: ReadonlyArray<number | null | undefined> | null | undefined,
  segments = EDGE_SEGMENTS,
): Point2D[] => {
  const n = points.length;
  const straight = () => points.map((p) => ({ x: p.x, y: p.y }));
  if (n < 3 || !edgeArcs) return straight();
  const out: Point2D[] = [];
  for (let i = 0; i < n; i += 1) {
    const a = points[i];
    const b = points[(i + 1) % n];
    const sagitta = edgeArcs[i];
    const poly =
      typeof sagitta === 'number' && Math.abs(sagitta) >= MIN_SAGITTA_MM
        ? bowArcPlanPoints(a.x, a.y, b.x, b.y, sagitta, segments)
        : [a, b];
    for (let k = 0; k < poly.length - 1; k += 1) out.push({ x: poly[k].x, y: poly[k].y });
  }
  return out;
};
