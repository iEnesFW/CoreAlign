import { ExtrudeGeometry, Shape } from 'three';
import { filletedShapeMm } from './surfaceFeatureShapes';
import { panelOutlinePointsMm, type PanelOutlineSpec } from '../../model/panelOutline';
import type { CornerRadiiMm } from '../../model/project.types';

export interface PanelGlassSpec extends PanelOutlineSpec {
  cornerRadiiMm?: CornerRadiiMm | null;
}

const rectShapeMm = (widthMm: number, heightMm: number): Shape => {
  const w = Math.max(1, widthMm) / 1000;
  const h = Math.max(1, heightMm) / 1000;
  const shape = new Shape();
  shape.moveTo(-w / 2, 0);
  shape.lineTo(w / 2, 0);
  shape.lineTo(w / 2, h);
  shape.lineTo(-w / 2, h);
  shape.closePath();
  return shape;
};

const panelFaceShape = (spec: PanelGlassSpec): Shape => {
  const pts = panelOutlinePointsMm(spec);
  // Never extrude a degenerate outline (a transient/empty shape would throw in three).
  if (pts.length < 3) return rectShapeMm(spec.widthMm, spec.heightMm);
  const top = spec.topShape ?? 'flat';
  if (!spec.shapeKind && top !== 'arched' && pts.length === 4) {
    const r = spec.cornerRadiiMm ?? {};
    return filletedShapeMm(
      pts.map((p) => ({ x: p.x, z: p.y })),
      [r.bl ?? 0, r.br ?? 0, r.tr ?? 0, r.tl ?? 0],
    );
  }
  const shape = new Shape();
  shape.moveTo(pts[0].x / 1000, pts[0].y / 1000);
  for (let i = 1; i < pts.length; i += 1) shape.lineTo(pts[i].x / 1000, pts[i].y / 1000);
  shape.closePath();
  return shape;
};

export const buildPanelGlassGeometry = (
  spec: PanelGlassSpec,
  thicknessM: number,
): ExtrudeGeometry => {
  const geom = new ExtrudeGeometry(panelFaceShape(spec), {
    depth: thicknessM,
    bevelEnabled: false,
  });
  geom.translate(0, 0, -thicknessM / 2);
  return geom;
};
