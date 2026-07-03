import { useRef, useState } from 'react';
import { Line } from '@react-three/drei';
import type { Group } from 'three';
import { useDrag3D } from '@/shared/three-engine';
import { boxCornersMm, resizeBoxFromCorner, type BoxFootprint } from './footprintCorners';

const MM = 1000;
const GRID_MM = 10;
const STICK_STEP_MM = 100;
const STICK_TOL_MM = 18;
const HANDLE_RADIUS_M = 0.06;
const HANDLE_COLOR = '#1d4ed8';
const HANDLE_HOVER_COLOR = '#f97316';
const PREVIEW_COLOR = '#f97316';

const snapGrid = (value: number) => Math.round(value / GRID_MM) * GRID_MM;

// 'corners' = the four box corners (thick footprints: walls, slabs). 'ends' = two handles on the
// centreline ends (thin runs) — a thin box's four corners collapse to two screen dots, which read
// as "two dots at one corner"; one handle per end is unambiguous and only changes length.
export type CornerHandleMode = 'corners' | 'ends';

interface FootprintCornerHandlesProps {
  box: BoxFootprint;
  topYM: number;
  onCommit: (next: BoxFootprint) => void;
  mode?: CornerHandleMode;
  // Live-preview outline for NON-RECTANGULAR bodies (arc walls/runs, curved slabs): maps the
  // previewed box to the ACTUAL outline the commit would produce, so the user sees the real
  // shape instead of a dashed phantom rectangle detached from the curved body.
  previewOutline?: (next: BoxFootprint) => [number, number, number][];
}

interface HandleSpec {
  cornerIndex: number;
  x: number;
  y: number;
}

// The on-screen position + the resize-anchor corner index for each handle of a box.
const handleSpecs = (box: BoxFootprint, mode: CornerHandleMode): HandleSpec[] => {
  const c = boxCornersMm(box);
  if (mode === 'ends') {
    return [
      { cornerIndex: 0, x: (c[0].x + c[3].x) / 2, y: (c[0].y + c[3].y) / 2 },
      { cornerIndex: 1, x: (c[1].x + c[2].x) / 2, y: (c[1].y + c[2].y) / 2 },
    ];
  }
  return c.map((p, i) => ({ cornerIndex: i, x: p.x, y: p.y }));
};

// Draggable spheres at a box's plan corners / ends (rendered in the WORLD frame, so this must sit
// OUTSIDE the object's rotated group). Dragging resizes the box from that corner with the opposite
// corner pinned (Q vertex-edit mode for walls / slabs / runs); the new footprint previews live.
export function FootprintCornerHandles({
  box,
  topYM,
  onCommit,
  mode = 'corners',
  previewOutline,
}: FootprintCornerHandlesProps) {
  const [previewBox, setPreviewBox] = useState<BoxFootprint | null>(null);
  const specs = handleSpecs(box, mode);
  const previewPoints = previewBox
    ? previewOutline
      ? previewOutline(previewBox)
      : [...boxCornersMm(previewBox), boxCornersMm(previewBox)[0]].map(
          (p): [number, number, number] => [p.x / MM, topYM, p.y / MM],
        )
    : null;
  return (
    <>
      {previewPoints && (
        <Line
          points={previewPoints}
          color={PREVIEW_COLOR}
          lineWidth={2}
          dashed
          dashSize={0.05}
          gapSize={0.03}
        />
      )}
      {specs.map((spec) => (
        <CornerHandle
          key={spec.cornerIndex}
          box={box}
          cornerIndex={spec.cornerIndex}
          handleX={spec.x}
          handleY={spec.y}
          topYM={topYM}
          mode={mode}
          onPreview={setPreviewBox}
          onCommit={onCommit}
        />
      ))}
    </>
  );
}

interface CornerHandleProps {
  box: BoxFootprint;
  cornerIndex: number;
  handleX: number;
  handleY: number;
  topYM: number;
  mode: CornerHandleMode;
  onPreview: (box: BoxFootprint | null) => void;
  onCommit: (next: BoxFootprint) => void;
}

function CornerHandle({
  box,
  cornerIndex,
  handleX,
  handleY,
  topYM,
  mode,
  onPreview,
  onCommit,
}: CornerHandleProps) {
  const anchorRef = useRef<Group>(null);
  const [hovered, setHovered] = useState(false);

  // Where THIS handle sits for a given box state (mirrors handleSpecs so the sphere tracks the
  // clamped corner — it "sticks" at the min-size limit instead of running past or snapping back).
  const handlePosForBox = (b: BoxFootprint) => {
    const specs = handleSpecs(b, mode);
    return specs.find((s) => s.cornerIndex === cornerIndex) ?? { x: handleX, y: handleY };
  };

  const drag = useDrag3D({
    constraint: { mode: 'ground' },
    enabled: true,
    onMove: (delta) => {
      const resized = resizeBoxFromCorner(
        box,
        cornerIndex,
        snapGrid(handleX + delta.x),
        snapGrid(handleY + delta.z),
        50,
        STICK_STEP_MM,
        STICK_TOL_MM,
      );
      onPreview(resized);
      const pos = handlePosForBox(resized);
      anchorRef.current?.position.set(pos.x / MM, topYM, pos.y / MM);
    },
    onCommit: (delta) => {
      onPreview(null);
      anchorRef.current?.position.set(handleX / MM, topYM, handleY / MM);
      onCommit(
        resizeBoxFromCorner(
          box,
          cornerIndex,
          snapGrid(handleX + delta.x),
          snapGrid(handleY + delta.z),
          50,
          STICK_STEP_MM,
          STICK_TOL_MM,
        ),
      );
    },
  });

  return (
    <group ref={anchorRef} position={[handleX / MM, topYM, handleY / MM]}>
      <mesh
        {...drag.handlers}
        onClick={(e) => {
          e.stopPropagation();
          drag.consumeClick();
        }}
        onPointerOver={(e) => {
          e.stopPropagation();
          setHovered(true);
          document.body.style.cursor = 'grab';
        }}
        onPointerOut={() => {
          setHovered(false);
          document.body.style.cursor = 'auto';
        }}
        renderOrder={999}
      >
        <sphereGeometry args={[HANDLE_RADIUS_M, 16, 16]} />
        {/* WHY: depthTest off so corners behind the object body still show (were occluded). */}
        <meshBasicMaterial
          color={hovered ? HANDLE_HOVER_COLOR : HANDLE_COLOR}
          depthTest={false}
          depthWrite={false}
          transparent
        />
      </mesh>
    </group>
  );
}
