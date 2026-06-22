import type { SceneWallState } from './project.types';

export interface OpenEdge {
  originX: number;
  originY: number;
  rotationDeg: number;
  lengthMm: number;
  heightMm?: number;
  geomZ?: number;
  geomArcRadiusMm?: number;
  geomArcSweepDeg?: number;
  arcGlassBent?: boolean;
}

const ENDPOINT_TOLERANCE_MM = 150;
const MIN_EDGE_MM = 300;

interface Endpoint {
  x: number;
  y: number;
  wallId: string;
}

const wallEndpoints = (wall: SceneWallState): [Endpoint, Endpoint] => {
  const radians = (wall.rotationDeg * Math.PI) / 180;
  return [
    { x: wall.originX, y: wall.originY, wallId: wall.id },
    {
      x: wall.originX + wall.lengthMm * Math.cos(radians),
      y: wall.originY + wall.lengthMm * Math.sin(radians),
      wallId: wall.id,
    },
  ];
};

const distance = (a: Endpoint, b: Endpoint) => Math.hypot(a.x - b.x, a.y - b.y);

export const computeOpenEdges = (walls: SceneWallState[]): OpenEdge[] => {
  if (walls.length < 2) return [];
  const endpoints = walls.flatMap(wallEndpoints);
  const free = endpoints.filter(
    (point, index) =>
      !endpoints.some(
        (other, otherIndex) =>
          otherIndex !== index &&
          other.wallId !== point.wallId &&
          distance(point, other) <= ENDPOINT_TOLERANCE_MM,
      ),
  );

  const edges: OpenEdge[] = [];
  const used = new Set<number>();
  for (let i = 0; i < free.length; i += 1) {
    if (used.has(i)) continue;
    let best = -1;
    let bestDistance = Number.POSITIVE_INFINITY;
    for (let j = i + 1; j < free.length; j += 1) {
      if (used.has(j) || free[j].wallId === free[i].wallId) continue;
      const d = distance(free[i], free[j]);
      if (d < bestDistance) {
        bestDistance = d;
        best = j;
      }
    }
    if (best === -1 || bestDistance < MIN_EDGE_MM) continue;
    used.add(i);
    used.add(best);
    const a = free[i];
    const b = free[best];
    edges.push({
      originX: Math.round(a.x),
      originY: Math.round(a.y),
      rotationDeg: Math.round((Math.atan2(b.y - a.y, b.x - a.x) * 180) / Math.PI),
      lengthMm: Math.round(bestDistance),
    });
  }
  return edges;
};

export const DEFAULT_PANEL_TARGET_MM = 600;
export const MAX_AUTOFILL_PANELS = 20;
export const SERVER_PANEL_CAP = 50;

export const suggestedPanelCount = (lengthMm: number) =>
  Math.max(1, Math.min(MAX_AUTOFILL_PANELS, Math.ceil(lengthMm / DEFAULT_PANEL_TARGET_MM)));

export const panelCountForWidth = (lengthMm: number, maxPanelWidthMm?: number): number => {
  if (maxPanelWidthMm && maxPanelWidthMm > 0) {
    return Math.max(1, Math.min(SERVER_PANEL_CAP, Math.ceil(lengthMm / maxPanelWidthMm)));
  }
  return Math.max(1, Math.min(MAX_AUTOFILL_PANELS, Math.ceil(lengthMm / DEFAULT_PANEL_TARGET_MM)));
};

export const computeOpeningEdges = (walls: SceneWallState[]): OpenEdge[] => {
  const edges: OpenEdge[] = [];
  for (const wall of walls) {
    const radians = (wall.rotationDeg * Math.PI) / 180;
    const cos = Math.cos(radians);
    const sin = Math.sin(radians);
    // The opening's sill is measured from the wall's own base, so a raised wall
    // lifts the fill panel by the wall's geomZ on top of the local sill height.
    const wallBaseZ = wall.geomZ ?? 0;
    const pushEdge = (startMm: number, widthMm: number, sillMm: number, heightMm: number) => {
      if (widthMm < MIN_EDGE_MM || heightMm < MIN_EDGE_MM) return;
      edges.push({
        originX: Math.round(wall.originX + startMm * cos),
        originY: Math.round(wall.originY + startMm * sin),
        rotationDeg: wall.rotationDeg,
        lengthMm: Math.round(widthMm),
        heightMm: Math.round(heightMm),
        geomZ: Math.round(wallBaseZ + sillMm),
      });
    };
    for (const opening of wall.openings ?? []) {
      pushEdge(
        opening.offsetMm - opening.widthMm / 2,
        opening.widthMm,
        opening.sillMm,
        opening.heightMm,
      );
    }
    for (const feature of wall.features ?? []) {
      const throughHole =
        feature.mode === 'hole' ||
        (feature.mode === 'recess' && feature.depthMm >= wall.thicknessMm - 5);
      if (!throughHole) continue;
      pushEdge(
        feature.offsetMm - feature.widthMm / 2,
        feature.widthMm,
        feature.centerZMm - feature.heightMm / 2,
        feature.heightMm,
      );
    }
  }
  return edges;
};
