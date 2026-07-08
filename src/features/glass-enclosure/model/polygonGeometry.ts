import type { PolygonVertex } from './project.types';

export interface PolygonEdge {
  originX: number;
  originY: number;
  lengthMm: number;
  rotationDeg: number;
}

const MIN_VERTEX_COUNT = 3;

const isFiniteVertex = (value: unknown): value is PolygonVertex => {
  if (typeof value !== 'object' || value === null) return false;
  const candidate = value as Record<string, unknown>;
  return (
    typeof candidate.xMm === 'number' &&
    Number.isFinite(candidate.xMm) &&
    typeof candidate.yMm === 'number' &&
    Number.isFinite(candidate.yMm)
  );
};

export const parsePolygonVertices = (json: string | null): PolygonVertex[] => {
  if (!json) return [];
  try {
    const parsed: unknown = JSON.parse(json);
    if (!Array.isArray(parsed) || parsed.length < MIN_VERTEX_COUNT) return [];
    if (!parsed.every(isFiniteVertex)) return [];
    return parsed.map((vertex) => ({ xMm: vertex.xMm, yMm: vertex.yMm }));
  } catch {
    return [];
  }
};

export const polygonEdges = (vertices: PolygonVertex[]): PolygonEdge[] => {
  if (vertices.length < MIN_VERTEX_COUNT) return [];
  return vertices.map((from, index) => {
    const to = vertices[(index + 1) % vertices.length];
    const dx = to.xMm - from.xMm;
    const dy = to.yMm - from.yMm;
    return {
      originX: from.xMm,
      originY: from.yMm,
      lengthMm: Math.hypot(dx, dy),
      rotationDeg: (Math.atan2(dy, dx) * 180) / Math.PI,
    };
  });
};

export const polygonIsClosedValid = (vertices: PolygonVertex[]): boolean => {
  const edges = polygonEdges(vertices);
  if (edges.length < MIN_VERTEX_COUNT || !edges.every((edge) => edge.lengthMm > 0)) return false;
  // WHY: every edge can be non-zero yet the loop enclose NO area (e.g. 3 collinear points), which
  // renders overlapping flat facade segments — require a real signed area.
  let area2 = 0;
  for (let i = 0; i < vertices.length; i += 1) {
    const a = vertices[i];
    const b = vertices[(i + 1) % vertices.length];
    area2 += a.xMm * b.yMm - b.xMm * a.yMm;
  }
  return Math.abs(area2) > 1;
};
