import { GLTFExporter } from 'three/examples/jsm/exporters/GLTFExporter.js';
import { logger } from '@/shared/lib/logger';
import type { Object3D } from 'three';
import { isRealArc } from './arcGeometry';
import { arcBandOutlineMm } from './bandOutline';
import { curvedSlabPlanOutlineMm } from '../scene/builders/curvedSlabGeometry';
import { bowedPolygonOutline, edgeArcOutline, hasEdgeArc } from './edgeArcOutline';
import type { SceneSlabState, SceneState } from './project.types';

const RUN_THICKNESS_MM = 50;
const DEG2RAD = Math.PI / 180;

const escapeXml = (value: string): string =>
  value.replace(/[&<>"']/g, (ch) => {
    switch (ch) {
      case '&':
        return '&amp;';
      case '<':
        return '&lt;';
      case '>':
        return '&gt;';
      case '"':
        return '&quot;';
      default:
        return '&#39;';
    }
  });

interface Pt {
  x: number;
  y: number;
}

interface PlanShape {
  layer: string;
  points: Pt[];
}

const orientedRect = (
  originX: number,
  originY: number,
  lengthMm: number,
  rotationDeg: number,
  widthMm: number,
): Pt[] => {
  const rad = rotationDeg * DEG2RAD;
  const cos = Math.cos(rad);
  const sin = Math.sin(rad);
  const ex = originX + lengthMm * cos;
  const ey = originY + lengthMm * sin;
  const nx = -sin * (widthMm / 2);
  const ny = cos * (widthMm / 2);
  return [
    { x: originX + nx, y: originY + ny },
    { x: ex + nx, y: ey + ny },
    { x: ex - nx, y: ey - ny },
    { x: originX - nx, y: originY - ny },
  ];
};

const slabRect = (slab: SceneSlabState): Pt[] => {
  // A plan-arc slab is a bent band; curvedSlabPlanOutlineMm is the SAME source the mesh, the
  // footprint and the snap targets read, so the export cannot disagree with what is on screen.
  if (isRealArc(slab.geomArcRadiusMm, slab.geomArcSweepDeg)) {
    const cosR = Math.cos(slab.rotationDeg * DEG2RAD);
    const sinR = Math.sin(slab.rotationDeg * DEG2RAD);
    return curvedSlabPlanOutlineMm(
      slab.lengthMm,
      slab.depthMm,
      slab.geomArcRadiusMm ?? 0,
      slab.geomArcSweepDeg ?? 1,
      slab.slabArcAxis ?? 'length',
    ).map((p) => ({
      x: slab.originX + p.x * cosR - p.z * sinR,
      y: slab.originY + p.x * sinR + p.z * cosR,
    }));
  }
  // Same rule for a single-edge arc: the mesh and the footprint bow that edge, so exporting a
  // plain rectangle shipped a silhouette that does not match the body on screen.
  if (hasEdgeArc(slab.geomEdgeArc)) {
    const cosE = Math.cos(slab.rotationDeg * DEG2RAD);
    const sinE = Math.sin(slab.rotationDeg * DEG2RAD);
    const bowed = edgeArcOutline(slab.lengthMm, slab.depthMm, slab.geomEdgeArc ?? {});
    if (bowed.length >= 3) {
      return bowed.map((p) => ({
        x: slab.originX + p.x * cosE - p.y * sinE,
        y: slab.originY + p.x * sinE + p.y * cosE,
      }));
    }
  }
  const rad = slab.rotationDeg * DEG2RAD;
  const dx = Math.cos(rad);
  const dy = Math.sin(rad);
  const px = -dy;
  const py = dx;
  return [
    { x: slab.originX, y: slab.originY },
    { x: slab.originX + slab.lengthMm * dx, y: slab.originY + slab.lengthMm * dy },
    {
      x: slab.originX + slab.lengthMm * dx + slab.depthMm * px,
      y: slab.originY + slab.lengthMm * dy + slab.depthMm * py,
    },
    { x: slab.originX + slab.depthMm * px, y: slab.originY + slab.depthMm * py },
  ];
};

export const scenePlanShapes = (scene: SceneState): PlanShape[] => {
  const shapes: PlanShape[] = [];
  for (const wall of scene.walls ?? []) {
    // WHY isRealArc and not a radius check: a legacy row with a radius but no sweep renders
    // STRAIGHT, and a radius-only gate would export a phantom one-degree stub for it.
    const points = isRealArc(wall.geomArcRadiusMm, wall.geomArcSweepDeg)
      ? arcBandOutlineMm(wall, wall.originX, wall.originY, wall.rotationDeg, wall.thicknessMm / 2)
      : orientedRect(wall.originX, wall.originY, wall.lengthMm, wall.rotationDeg, wall.thicknessMm);
    shapes.push({ layer: 'WALLS', points });
  }
  for (const run of scene.runs) {
    const points = isRealArc(run.geomArcRadiusMm, run.geomArcSweepDeg)
      ? arcBandOutlineMm(run, run.originX, run.originY, run.rotationDeg, RUN_THICKNESS_MM / 2)
      : orientedRect(run.originX, run.originY, run.lengthMm, run.rotationDeg, RUN_THICKNESS_MM);
    shapes.push({ layer: 'RUNS', points });
  }
  for (const slab of scene.slabs ?? []) {
    shapes.push({ layer: slab.kind === 'roof' ? 'ROOFS' : 'FLOORS', points: slabRect(slab) });
  }
  for (const surface of scene.surfaces ?? []) {
    if (surface.points.length >= 3) {
      const outline = bowedPolygonOutline(surface.points, surface.edgeArcs ?? null);
      shapes.push({ layer: 'SURFACES', points: outline.map((p) => ({ x: p.x, y: p.y })) });
    }
  }
  return shapes;
};

export const sceneToDxf = (scene: SceneState): string => {
  const lines: string[] = ['0', 'SECTION', '2', 'ENTITIES'];
  for (const shape of scenePlanShapes(scene)) {
    lines.push('0', 'POLYLINE', '8', shape.layer, '66', '1', '70', '1');
    for (const p of shape.points) {
      lines.push('0', 'VERTEX', '8', shape.layer, '10', String(p.x), '20', String(p.y), '30', '0');
    }
    lines.push('0', 'SEQEND');
  }
  lines.push('0', 'ENDSEC', '0', 'EOF');
  return lines.join('\n');
};

const planBounds = (shapes: PlanShape[]) => {
  let minX = Infinity;
  let minY = Infinity;
  let maxX = -Infinity;
  let maxY = -Infinity;
  for (const s of shapes)
    for (const p of s.points) {
      if (p.x < minX) minX = p.x;
      if (p.y < minY) minY = p.y;
      if (p.x > maxX) maxX = p.x;
      if (p.y > maxY) maxY = p.y;
    }
  if (!Number.isFinite(minX)) return { minX: 0, minY: 0, maxX: 1000, maxY: 1000 };
  return { minX, minY, maxX, maxY };
};

const LAYER_FILL: Record<string, string> = {
  WALLS: '#94a3b8',
  RUNS: '#bae6fd',
  ROOFS: '#cbd5e1',
  FLOORS: '#e2e8f0',
  SURFACES: '#ddd6fe',
};

export const sceneToPlanSvg = (scene: SceneState, title: string): string => {
  const shapes = scenePlanShapes(scene);
  const { minX, minY, maxX, maxY } = planBounds(shapes);
  const pad = 200;
  const w = maxX - minX + pad * 2;
  const h = maxY - minY + pad * 2;
  const ty = (y: number) => maxY - y + pad;
  const tx = (x: number) => x - minX + pad;
  const polys = shapes
    .map((s) => {
      const pts = s.points.map((p) => `${tx(p.x).toFixed(1)},${ty(p.y).toFixed(1)}`).join(' ');
      return `<polygon points="${pts}" fill="${LAYER_FILL[s.layer] ?? '#e2e8f0'}" fill-opacity="0.5" stroke="#1e293b" stroke-width="20" />`;
    })
    .join('');
  return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${w} ${h}" width="100%">
    <text x="${pad}" y="${pad / 2}" font-size="120" font-family="sans-serif" fill="#0f172a">${escapeXml(title)}</text>
    ${polys}
  </svg>`;
};

export const downloadTextFile = (filename: string, mime: string, text: string) => {
  const blob = new Blob([text], { type: mime });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  a.click();
  URL.revokeObjectURL(url);
};

export const printPlanSvg = (svg: string) => {
  const win = window.open('', '_blank');
  if (!win) return;
  win.document.write(
    `<!doctype html><html><head><title>Plan</title><style>@media print{body{margin:0}}body{margin:0;padding:12px}</style></head><body>${svg}</body></html>`,
  );
  win.document.close();
  const triggerPrint = () => {
    win.focus();
    win.print();
  };
  if (win.document.readyState === 'complete') {
    triggerPrint();
  } else {
    win.addEventListener('load', triggerPrint);
  }
};

let exportRoot: Object3D | null = null;

export const registerExportRoot = (root: Object3D | null) => {
  exportRoot = root;
};

export const exportSceneGlb = (filename: string) => {
  if (!exportRoot) return;
  new GLTFExporter().parse(
    exportRoot,
    (result) => {
      const blob =
        result instanceof ArrayBuffer
          ? new Blob([result], { type: 'model/gltf-binary' })
          : new Blob([JSON.stringify(result)], { type: 'model/gltf+json' });
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = filename;
      a.click();
      URL.revokeObjectURL(url);
    },
    (error) => logger.error('glass-designer.glb-export-failed', { error: String(error) }),
    { binary: true },
  );
};
