import { ExtrudeGeometry, Path, Shape } from 'three';
import { panelOutlinePointsMm } from '../../model/panelOutline';
import type { PanelGlassSpec } from './panelGeometry';

// A flat frame band that hugs a shaped panel's silhouette: the glass outline with an
// inset copy punched out, extruded through the panel depth. One geometry follows any
// shape (polygon / ellipse / arched), so a shaped pane reads as a framed shaped window
// instead of a bare cut-out floating in a rectangular cell.
export const buildPanelFrameGeometry = (
  spec: PanelGlassSpec,
  frameWidthMm: number,
  depthM: number,
): ExtrudeGeometry | null => {
  const outer = panelOutlinePointsMm(spec);
  if (outer.length < 3) return null;
  let minX = Infinity;
  let maxX = -Infinity;
  let minY = Infinity;
  let maxY = -Infinity;
  for (const p of outer) {
    if (p.x < minX) minX = p.x;
    if (p.x > maxX) maxX = p.x;
    if (p.y < minY) minY = p.y;
    if (p.y > maxY) maxY = p.y;
  }
  const w = maxX - minX;
  const h = maxY - minY;
  if (w <= 0 || h <= 0) return null;
  const fw = Math.min(frameWidthMm, Math.min(w, h) / 3);
  const sx = (w - 2 * fw) / w;
  const sy = (h - 2 * fw) / h;
  if (sx <= 0 || sy <= 0) return null;
  const cx = (minX + maxX) / 2;
  const cy = (minY + maxY) / 2;

  const shape = new Shape();
  shape.moveTo(outer[0].x / 1000, outer[0].y / 1000);
  for (let i = 1; i < outer.length; i += 1) shape.lineTo(outer[i].x / 1000, outer[i].y / 1000);
  shape.closePath();

  const hole = new Path();
  const inner = outer.map((p) => ({ x: cx + (p.x - cx) * sx, y: cy + (p.y - cy) * sy }));
  hole.moveTo(inner[0].x / 1000, inner[0].y / 1000);
  for (let i = 1; i < inner.length; i += 1) hole.lineTo(inner[i].x / 1000, inner[i].y / 1000);
  hole.closePath();
  shape.holes.push(hole);

  const geom = new ExtrudeGeometry(shape, { depth: depthM, bevelEnabled: false });
  geom.translate(0, 0, -depthM / 2);
  return geom;
};
