import { useMemo } from 'react';
import { Line } from '@react-three/drei';
import { arcPointAt, resolveArc } from '../../model/arcGeometry';

const OUTLINE_STEPS = 48;

interface ArcOutlineProps {
  radiusMm: number;
  sweepDeg: number;
  baseYM: number;
  topYM: number;
  // Plan half-thickness (metres): > 0 draws the outer AND inner edge rings; 0 = centreline only.
  halfWidthM?: number;
  color: string;
  lineWidth?: number;
}

// Explicit selection outline for ARC bodies, drawn analytically from the stored arc in the
// object's GROUP-LOCAL frame (x/z plan, y up). drei <Edges threshold> cannot serve here: the
// curved silhouette's facet seams sit under the threshold (only the end caps emit), and after a
// CSG hole is carved the edge hashing fragments — so the outline must not derive from the render
// geometry at all.
export function ArcOutline({
  radiusMm,
  sweepDeg,
  baseYM,
  topYM,
  halfWidthM = 0,
  color,
  lineWidth = 1.5,
}: ArcOutlineProps) {
  const polylines = useMemo(() => {
    const resolved = resolveArc(radiusMm, sweepDeg);
    const rM = resolved.radiusM;
    const dir = resolved.direction;
    const offsets = halfWidthM > 0 ? [-halfWidthM, halfWidthM] : [0];
    const edgePoint = (phi: number, off: number): [number, number] => {
      const p = arcPointAt(rM, dir, phi);
      const tangent = Math.atan2(dir * Math.sin(phi), Math.cos(phi));
      return [p.x - Math.sin(tangent) * off, p.z + Math.cos(tangent) * off];
    };
    const lines: [number, number, number][][] = [];
    for (const off of offsets) {
      const top: [number, number, number][] = [];
      const bottom: [number, number, number][] = [];
      for (let i = 0; i <= OUTLINE_STEPS; i += 1) {
        const phi = (resolved.sweepRad * i) / OUTLINE_STEPS;
        const [x, z] = edgePoint(phi, off);
        top.push([x, topYM, z]);
        bottom.push([x, baseYM, z]);
      }
      lines.push(top, bottom);
    }
    for (const phi of [0, resolved.sweepRad]) {
      for (const off of offsets) {
        const [x, z] = edgePoint(phi, off);
        lines.push([
          [x, baseYM, z],
          [x, topYM, z],
        ]);
      }
    }
    return lines;
  }, [radiusMm, sweepDeg, baseYM, topYM, halfWidthM]);

  return (
    <>
      {polylines.map((points, i) => (
        <Line key={i} points={points} color={color} lineWidth={lineWidth} raycast={() => null} />
      ))}
    </>
  );
}
