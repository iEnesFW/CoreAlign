import { useRef, useState } from 'react';
import type { Group } from 'three';
import { useDrag3D } from '@/shared/three-engine';
import { boxCornersMm, resizeBoxFromCorner, type BoxFootprint } from './footprintCorners';

const MM = 1000;
const GRID_MM = 10;
const HANDLE_RADIUS_M = 0.06;
const HANDLE_COLOR = '#1d4ed8';
const HANDLE_HOVER_COLOR = '#f97316';

const snapGrid = (value: number) => Math.round(value / GRID_MM) * GRID_MM;

interface FootprintCornerHandlesProps {
  box: BoxFootprint;
  topYM: number;
  onCommit: (next: BoxFootprint) => void;
}

// Draggable spheres at a box's four plan corners (rendered in the WORLD frame, so this must
// sit OUTSIDE the object's rotated group). Dragging a corner resizes the box from that corner
// with the opposite corner pinned (Q vertex-edit mode for walls / slabs).
export function FootprintCornerHandles({ box, topYM, onCommit }: FootprintCornerHandlesProps) {
  const corners = boxCornersMm(box);
  return (
    <>
      {corners.map((corner, index) => (
        <CornerHandle
          key={index}
          index={index}
          box={box}
          cornerX={corner.x}
          cornerY={corner.y}
          topYM={topYM}
          onCommit={onCommit}
        />
      ))}
    </>
  );
}

interface CornerHandleProps {
  index: number;
  box: BoxFootprint;
  cornerX: number;
  cornerY: number;
  topYM: number;
  onCommit: (next: BoxFootprint) => void;
}

function CornerHandle({ index, box, cornerX, cornerY, topYM, onCommit }: CornerHandleProps) {
  const anchorRef = useRef<Group>(null);
  const [hovered, setHovered] = useState(false);

  const drag = useDrag3D({
    constraint: { mode: 'ground' },
    enabled: true,
    onMove: (delta) => {
      anchorRef.current?.position.set(
        snapGrid(cornerX + delta.x) / MM,
        topYM,
        snapGrid(cornerY + delta.z) / MM,
      );
    },
    onCommit: (delta) => {
      anchorRef.current?.position.set(cornerX / MM, topYM, cornerY / MM);
      onCommit(
        resizeBoxFromCorner(box, index, snapGrid(cornerX + delta.x), snapGrid(cornerY + delta.z)),
      );
    },
  });

  return (
    <group ref={anchorRef} position={[cornerX / MM, topYM, cornerY / MM]}>
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
