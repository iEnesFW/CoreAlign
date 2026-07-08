import { ProfileBar } from './ProfileBar';
import type { OpeningFrameRect } from './WallObject';
import type { QualityPreset } from '@/shared/three-engine';

interface WallOpeningFramesProps {
  frames: OpeningFrameRect[];
  thicknessMm: number;
  quality: QualityPreset;
}

const FRAME_FACE_MM = 50;
const FRAME_COLOR = '#aeb4ba';
const SECTION_M = FRAME_FACE_MM / 1000;

export function WallOpeningFrames({ frames, thicknessMm, quality }: WallOpeningFramesProps) {
  const section = { width: Math.min(FRAME_FACE_MM, thicknessMm), height: FRAME_FACE_MM };
  return (
    <>
      {frames.map((f, i) => {
        const h = f.y1 - f.y0;
        const w = f.x1 - f.x0;
        if (h <= 0 || w <= 0) return null;
        const cx = (f.x0 + f.x1) / 2;
        const yMid = (f.y0 + f.y1) / 2;
        const spanW = Math.max(0.001, w - SECTION_M);
        // WHY: for an opening narrower than the profile section the two jambs already span the full
        // width, so a horizontal top/sill bar degenerates to a ~1mm sliver — omit it.
        const showHorizontals = w > SECTION_M;
        return (
          <group key={i}>
            <ProfileBar
              lengthM={h}
              crossSectionMm={section}
              hexColor={FRAME_COLOR}
              finish="Anodized"
              quality={quality}
              position={[f.x0, yMid, 0]}
              rotation={[0, 0, Math.PI / 2]}
            />
            <ProfileBar
              lengthM={h}
              crossSectionMm={section}
              hexColor={FRAME_COLOR}
              finish="Anodized"
              quality={quality}
              position={[f.x1, yMid, 0]}
              rotation={[0, 0, Math.PI / 2]}
            />
            {showHorizontals && (
              <ProfileBar
                lengthM={spanW}
                crossSectionMm={section}
                hexColor={FRAME_COLOR}
                finish="Anodized"
                quality={quality}
                position={[cx, f.y1, 0]}
              />
            )}
            {showHorizontals && f.hasSill && (
              <ProfileBar
                lengthM={spanW}
                crossSectionMm={section}
                hexColor={FRAME_COLOR}
                finish="Anodized"
                quality={quality}
                position={[cx, f.y0, 0]}
              />
            )}
          </group>
        );
      })}
    </>
  );
}
