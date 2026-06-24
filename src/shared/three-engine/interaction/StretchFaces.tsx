import { useMemo, useRef, useState } from 'react';
import { Html, Line } from '@react-three/drei';
import { DoubleSide } from 'three';
import { useDrag3D } from './useDrag3D';
import type { CSSProperties } from 'react';
import type { Group } from 'three';

export interface StretchFaceDef {
  id: string;
  centerM: [number, number, number];
  rotation: [number, number, number];
  widthM: number;
  heightM: number;
  axis: [number, number, number];
  hitWidthM?: number;
  hitHeightM?: number;
  label?: (deltaMm: number) => string;
  onPreview: (deltaMm: number) => void;
  onCommit: (deltaMm: number) => void;
}

const MM = 1000;
const FACE_COLOR = '#2563eb';
const FACE_OPACITY = 0.18;
const DASH_SIZE_M = 0.05;
const GAP_SIZE_M = 0.035;
const OUTLINE_LIFT_M = 0.002;
const LABEL_LIFT_M = 0.05;

const LABEL_STYLE: CSSProperties = {
  pointerEvents: 'none',
  background: 'rgba(15, 23, 42, 0.88)',
  color: '#ffffff',
  padding: '2px 8px',
  borderRadius: 6,
  fontSize: 11,
  fontWeight: 600,
  whiteSpace: 'nowrap',
};

export function StretchFaces({ faces }: { faces: StretchFaceDef[] }) {
  return (
    <>
      {faces.map((face) => (
        <StretchFace key={face.id} face={face} />
      ))}
    </>
  );
}

function StretchFace({ face }: { face: StretchFaceDef }) {
  const anchorRef = useRef<Group>(null);
  const [hovered, setHovered] = useState(false);
  const [dragging, setDragging] = useState(false);
  const [deltaMm, setDeltaMm] = useState(0);

  const outline = useMemo<[number, number, number][]>(() => {
    const hw = face.widthM / 2;
    const hh = face.heightM / 2;
    return [
      [-hw, -hh, OUTLINE_LIFT_M],
      [hw, -hh, OUTLINE_LIFT_M],
      [hw, hh, OUTLINE_LIFT_M],
      [-hw, hh, OUTLINE_LIFT_M],
      [-hw, -hh, OUTLINE_LIFT_M],
    ];
  }, [face.widthM, face.heightM]);

  const applyOffset = (delta: number) => {
    anchorRef.current?.position.set(
      face.centerM[0] + (face.axis[0] * delta) / MM,
      face.centerM[1] + (face.axis[1] * delta) / MM,
      face.centerM[2] + (face.axis[2] * delta) / MM,
    );
  };

  const drag = useDrag3D({
    constraint: { mode: 'axis', targetRef: anchorRef, localAxis: face.axis },
    enabled: true,
    onMove: (delta) => {
      if (delta.x === 0) {
        setDragging(false);
        setDeltaMm(0);
        applyOffset(0);
        face.onPreview(0);
        return;
      }
      setDragging(true);
      setDeltaMm(delta.x);
      applyOffset(delta.x);
      face.onPreview(delta.x);
    },
    onCommit: (delta) => {
      setDragging(false);
      setDeltaMm(0);
      applyOffset(0);
      if (Math.round(delta.x) !== 0) face.onCommit(delta.x);
      else face.onPreview(0);
    },
  });

  const highlighted = hovered || dragging;

  return (
    <group ref={anchorRef} position={face.centerM}>
      <group rotation={face.rotation}>
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
        >
          <planeGeometry args={[face.hitWidthM ?? face.widthM, face.hitHeightM ?? face.heightM]} />
          <meshBasicMaterial transparent opacity={0} depthWrite={false} side={DoubleSide} />
        </mesh>
        {highlighted && (
          <>
            <mesh raycast={() => null}>
              <planeGeometry args={[face.widthM, face.heightM]} />
              <meshBasicMaterial
                color={FACE_COLOR}
                transparent
                opacity={FACE_OPACITY}
                depthWrite={false}
                side={DoubleSide}
              />
            </mesh>
            <Line
              points={outline}
              color={FACE_COLOR}
              dashed
              dashSize={DASH_SIZE_M}
              gapSize={GAP_SIZE_M}
              lineWidth={1.5}
              raycast={() => null}
            />
          </>
        )}
        {dragging && face.label && (
          <Html center position={[0, 0, LABEL_LIFT_M]} zIndexRange={[40, 0]}>
            <div style={LABEL_STYLE}>{face.label(deltaMm)}</div>
          </Html>
        )}
      </group>
    </group>
  );
}
