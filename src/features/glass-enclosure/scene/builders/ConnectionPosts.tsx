import { ProfileBar } from './ProfileBar';
import { arcEndLocal, isRealArc } from '../../model/arcGeometry';
import type { QualityPreset } from '@/shared/three-engine';
import type { ColorOptionDto } from '../../model/glassEnclosure.types';
import type { SceneConnectionState, SceneRunState } from '../../model/project.types';

interface ConnectionPostsProps {
  connections: SceneConnectionState[];
  runs: SceneRunState[];
  colors: Map<string, ColorOptionDto>;
  quality: QualityPreset;
}

const POST_CROSS_SECTION = { width: 60, height: 60 };
const DEFAULT_HEX_COLOR = '#cfd5d9';
const DEG2RAD = Math.PI / 180;

const runEndpoints = (run: SceneRunState): { x: number; y: number }[] => {
  const rad = run.rotationDeg * DEG2RAD;
  const cos = Math.cos(rad);
  const sin = Math.sin(rad);
  let endX: number;
  let endY: number;
  if (isRealArc(run.geomArcRadiusMm, run.geomArcSweepDeg)) {
    const e = arcEndLocal(run.geomArcRadiusMm ?? 0, run.geomArcSweepDeg ?? 1);
    endX = run.originX + e.xMm * cos - e.yMm * sin;
    endY = run.originY + e.xMm * sin + e.yMm * cos;
  } else {
    endX = run.originX + run.lengthMm * cos;
    endY = run.originY + run.lengthMm * sin;
  }
  return [
    { x: run.originX, y: run.originY },
    { x: endX, y: endY },
  ];
};

const sharedCorner = (a: SceneRunState, b: SceneRunState): { x: number; y: number } | null => {
  const ea = runEndpoints(a);
  const eb = runEndpoints(b);
  let best: { x: number; y: number } | null = null;
  let bestDist = Infinity;
  for (const pa of ea) {
    for (const pb of eb) {
      const d = Math.hypot(pa.x - pb.x, pa.y - pb.y);
      if (d < bestDist) {
        bestDist = d;
        best = { x: (pa.x + pb.x) / 2, y: (pa.y + pb.y) / 2 };
      }
    }
  }
  return best;
};

export function ConnectionPosts({ connections, runs, colors, quality }: ConnectionPostsProps) {
  const runMap = new Map(runs.map((r) => [r.id, r]));
  return (
    <>
      {connections.map((connection) => {
        if (!connection.usesCornerPost) return null;
        const a = runMap.get(connection.runAId);
        const b = runMap.get(connection.runBId);
        if (!a || !b) return null;
        const corner = sharedCorner(a, b);
        if (!corner) return null;
        const baseMm = Math.max(a.geomZ ?? 0, b.geomZ ?? 0);
        const topMm = Math.min((a.geomZ ?? 0) + a.heightMm, (b.geomZ ?? 0) + b.heightMm);
        const heightM = Math.max(0, topMm - baseMm) / 1000;
        const baseY = baseMm / 1000;
        if (heightM <= 0) return null;
        const color = (a.colorId && colors.get(a.colorId)?.hexColor) || DEFAULT_HEX_COLOR;
        return (
          <group key={connection.id} position={[corner.x / 1000, baseY, corner.y / 1000]}>
            <ProfileBar
              lengthM={heightM}
              crossSectionMm={POST_CROSS_SECTION}
              hexColor={color}
              finish="PowderCoated"
              quality={quality}
              position={[0, heightM / 2, 0]}
              rotation={[0, 0, Math.PI / 2]}
            />
          </group>
        );
      })}
    </>
  );
}
