import { Path, Shape } from 'three';
import type { FeatureOutlinePoint } from '../../model/wallFeatureGeometry';

export const outlineToShape = (outline: FeatureOutlinePoint[]): Shape => {
  const shape = new Shape();
  shape.moveTo(outline[0].x / 1000, outline[0].z / 1000);
  for (let i = 1; i < outline.length; i += 1)
    shape.lineTo(outline[i].x / 1000, outline[i].z / 1000);
  shape.closePath();
  return shape;
};

export const outlineToPath = (outline: FeatureOutlinePoint[]): Path => {
  const path = new Path();
  path.moveTo(outline[0].x / 1000, outline[0].z / 1000);
  for (let i = 1; i < outline.length; i += 1) path.lineTo(outline[i].x / 1000, outline[i].z / 1000);
  path.closePath();
  return path;
};

const MIN_FILLET_M = 0.0005;

interface FilletCorner {
  corner: { x: number; y: number };
  pIn: { x: number; y: number };
  pOut: { x: number; y: number };
  radM: number;
}

export const filletedShapeMm = (cornersMm: FeatureOutlinePoint[], radiiMm: number[]): Shape => {
  const pts = cornersMm.map((p) => ({ x: p.x / 1000, y: p.z / 1000 }));
  const n = pts.length;
  const corners: FilletCorner[] = pts.map((p, i) => {
    const prev = pts[(i + n - 1) % n];
    const next = pts[(i + 1) % n];
    const inLen = Math.hypot(p.x - prev.x, p.y - prev.y);
    const outLen = Math.hypot(next.x - p.x, next.y - p.y);
    const radM = Math.min(
      Math.max(0, (radiiMm[i] ?? 0) / 1000),
      Math.max(0, inLen / 2 - MIN_FILLET_M),
      Math.max(0, outLen / 2 - MIN_FILLET_M),
    );
    if (radM <= MIN_FILLET_M || inLen === 0 || outLen === 0) {
      return { corner: p, pIn: p, pOut: p, radM: 0 };
    }
    const dirIn = { x: (p.x - prev.x) / inLen, y: (p.y - prev.y) / inLen };
    const dirOut = { x: (next.x - p.x) / outLen, y: (next.y - p.y) / outLen };
    return {
      corner: p,
      radM,
      pIn: { x: p.x - dirIn.x * radM, y: p.y - dirIn.y * radM },
      pOut: { x: p.x + dirOut.x * radM, y: p.y + dirOut.y * radM },
    };
  });
  const shape = new Shape();
  shape.moveTo(corners[0].pOut.x, corners[0].pOut.y);
  for (let i = 1; i <= n; i += 1) {
    const c = corners[i % n];
    shape.lineTo(c.pIn.x, c.pIn.y);
    if (c.radM > 0) shape.quadraticCurveTo(c.corner.x, c.corner.y, c.pOut.x, c.pOut.y);
  }
  shape.closePath();
  return shape;
};
