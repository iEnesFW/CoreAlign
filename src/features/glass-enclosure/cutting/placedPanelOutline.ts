import { panelOutlinePointsMm } from '../model/panelOutline';
import { parsePanelPolygonPoints } from '../model/panelPolygon';
import type { PanelCutShapeDto } from '../model/engineering.types';
import type { PanelShapeKind, PanelTopShape } from '../model/project.types';

export interface PlacedPanelLike {
  x: number;
  y: number;
  widthMm: number;
  heightMm: number;
  rotated: boolean;
  shape?: PanelCutShapeDto | null;
}

export type PanelShapeToken = 'raked' | 'arched' | 'rounded' | 'ellipse' | 'polygon';

const hasRadii = (s: PanelCutShapeDto): boolean =>
  Boolean(
    (s.cornerRadiusTlMm ?? 0) ||
    (s.cornerRadiusTrMm ?? 0) ||
    (s.cornerRadiusBrMm ?? 0) ||
    (s.cornerRadiusBlMm ?? 0),
  );

export const panelShapeToken = (shape?: PanelCutShapeDto | null): PanelShapeToken | null => {
  if (!shape) return null;
  if (shape.shapeKind === 'polygon' && parsePanelPolygonPoints(shape.shapePointsJson))
    return 'polygon';
  if (shape.shapeKind === 'ellipse') return 'ellipse';
  const top = shape.topShape ?? 'flat';
  if (top === 'raked') return 'raked';
  if (top === 'arched' && (shape.archRiseMm ?? 0) > 0) return 'arched';
  if (hasRadii(shape)) return 'rounded';
  return null;
};

// SVG polygon "points" for a placed shaped panel, in sheet coordinates.
// Returns null for a plain rectangle so the caller can draw a faster <rect>.
// panelOutlinePointsMm is bottom-centre, y-up; here we flip to SVG y-down and
// rotate 90° CW into the placed box when the nester rotated the blank.
export const placedPanelPolygonPoints = (p: PlacedPanelLike): string | null => {
  const shape = p.shape;
  if (!shape) return null;
  // Only a raked or arched top changes the silhouette from the blank rectangle.
  // Rounded-only panels keep a rectangular outline (radii are annotated, not drawn
  // here) so the caller falls back to a fast <rect>; true radii arrive with #8.2.
  const top = shape.topShape ?? 'flat';
  const polyPoints =
    shape.shapeKind === 'polygon' ? parsePanelPolygonPoints(shape.shapePointsJson) : null;
  const drawsOutline =
    Boolean(polyPoints) ||
    shape.shapeKind === 'ellipse' ||
    top === 'raked' ||
    (top === 'arched' && (shape.archRiseMm ?? 0) > 0);
  if (!drawsOutline) return null;

  const originalWidth = p.rotated ? p.heightMm : p.widthMm;
  const blankHeight = p.rotated ? p.widthMm : p.heightMm;

  const outline = panelOutlinePointsMm({
    widthMm: originalWidth,
    heightMm: shape.nominalHeightMm,
    topShape: (shape.topShape ?? 'flat') as PanelTopShape,
    topRightHeightMm: shape.topRightHeightMm,
    archRiseMm: shape.archRiseMm,
    shapeKind: (shape.shapeKind ?? null) as PanelShapeKind | null,
    points: polyPoints,
  });

  return outline
    .map((pt) => {
      const bx = pt.x + originalWidth / 2;
      const by = blankHeight - pt.y;
      const sx = p.rotated ? p.x + (blankHeight - by) : p.x + bx;
      const sy = p.rotated ? p.y + bx : p.y + by;
      return `${Math.round(sx)},${Math.round(sy)}`;
    })
    .join(' ');
};
