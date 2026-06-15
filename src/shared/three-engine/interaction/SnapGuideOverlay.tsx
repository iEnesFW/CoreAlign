import { useEffect, useState } from 'react';
import { Line } from '@react-three/drei';
import { subscribeSnapGuides } from './snapGuides';
import type { PlanSnapGuide } from './planSnap';

const MM = 1000;
const GUIDE_Y_M = 0.02;
const CORNER_ARM_M = 0.12;

const GUIDE_COLORS: Record<PlanSnapGuide['kind'], string> = {
  corner: '#f59e0b',
  edge: '#2563eb',
  axis: '#0ea5e9',
};

export function SnapGuideOverlay() {
  const [guides, setGuides] = useState<PlanSnapGuide[]>([]);
  useEffect(() => subscribeSnapGuides(setGuides), []);
  return (
    <>
      {guides.map((guide, index) =>
        guide.kind === 'corner' ? (
          <CornerMarker key={`${guide.kind}-${index}`} guide={guide} />
        ) : (
          <GuideLine key={`${guide.kind}-${index}`} guide={guide} />
        ),
      )}
    </>
  );
}

const GuideLine = ({ guide }: { guide: PlanSnapGuide }) => {
  const lengthSq =
    (guide.x2 - guide.x1) * (guide.x2 - guide.x1) + (guide.y2 - guide.y1) * (guide.y2 - guide.y1);
  if (lengthSq < 1) return null;
  return (
    <Line
      points={[
        [guide.x1 / MM, GUIDE_Y_M, guide.y1 / MM],
        [guide.x2 / MM, GUIDE_Y_M, guide.y2 / MM],
      ]}
      color={GUIDE_COLORS[guide.kind]}
      dashed
      dashSize={0.08}
      gapSize={0.05}
      lineWidth={1.5}
      raycast={() => null}
    />
  );
};

const CornerMarker = ({ guide }: { guide: PlanSnapGuide }) => {
  const x = guide.x1 / MM;
  const z = guide.y1 / MM;
  return (
    <>
      <Line
        points={[
          [x - CORNER_ARM_M, GUIDE_Y_M, z],
          [x + CORNER_ARM_M, GUIDE_Y_M, z],
        ]}
        color={GUIDE_COLORS.corner}
        lineWidth={2}
        raycast={() => null}
      />
      <Line
        points={[
          [x, GUIDE_Y_M, z - CORNER_ARM_M],
          [x, GUIDE_Y_M, z + CORNER_ARM_M],
        ]}
        color={GUIDE_COLORS.corner}
        lineWidth={2}
        raycast={() => null}
      />
    </>
  );
};
