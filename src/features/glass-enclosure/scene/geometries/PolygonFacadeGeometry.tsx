import { useMemo } from 'react';
import { Edges } from '@react-three/drei';
import { QUALITY_SETTINGS, type QualityPreset } from '@/shared/three-engine';
import { ProfileBar } from '../builders/ProfileBar';
import { useGlassMaterial } from '../materials/glassMaterial';
import { polygonEdges, polygonIsClosedValid } from '../../model/polygonGeometry';
import type { PolygonVertex } from '../../model/project.types';
import type {
  ColorFinishType,
  ColorOptionDto,
  GlassTypeDto,
} from '../../model/glassEnclosure.types';

interface PolygonFacadeGeometryProps {
  vertices: PolygonVertex[];
  heightMm: number;
  quality: QualityPreset;
  glassTypes?: Map<string, GlassTypeDto>;
  colors?: Map<string, ColorOptionDto>;
}

const DEFAULT_GLASS_THICKNESS_MM = 8;
const DEFAULT_PROFILE_CROSS_SECTION = { width: 50, height: 60 };
const CORNER_POST_CROSS_SECTION = { width: 60, height: 60 };
const DEFAULT_HEX_COLOR = '#cfd5d9';
const GLASS_EDGE_INSET_M = 0.012;
const EDGE_LINE_COLOR = '#9aacb5';

const firstMapValue = <T,>(map?: Map<string, T>): T | undefined =>
  map ? map.values().next().value : undefined;

export function PolygonFacadeGeometry({
  vertices,
  heightMm,
  quality,
  glassTypes,
  colors,
}: PolygonFacadeGeometryProps) {
  const edges = useMemo(() => polygonEdges(vertices), [vertices]);
  const isValid = useMemo(() => polygonIsClosedValid(vertices), [vertices]);

  const sampleGlass = firstMapValue(glassTypes);
  const glassThicknessMm = sampleGlass?.thicknessMm ?? DEFAULT_GLASS_THICKNESS_MM;
  const sampleColor = firstMapValue(colors);
  const profileColor = sampleColor?.hexColor ?? DEFAULT_HEX_COLOR;
  const profileFinish: ColorFinishType = sampleColor?.finishType ?? 'PowderCoated';

  const glassMaterial = useGlassMaterial({ quality, thicknessMm: glassThicknessMm });
  const settings = QUALITY_SETTINGS[quality];

  if (!isValid) return null;

  const heightM = heightMm / 1000;
  const profileHalfM = DEFAULT_PROFILE_CROSS_SECTION.height / 1000 / 2;
  const glassHeightM = Math.max(0.05, heightM - 2 * profileHalfM);

  return (
    <group>
      {edges.map((edge, index) => {
        const lengthM = edge.lengthMm / 1000;
        const glassWidthM = Math.max(0.05, lengthM - GLASS_EDGE_INSET_M);
        return (
          <group
            key={`edge-${index}`}
            position={[edge.originX / 1000, 0, edge.originY / 1000]}
            rotation={[0, (-edge.rotationDeg * Math.PI) / 180, 0]}
          >
            <group position={[lengthM / 2, 0, 0]}>
              <ProfileBar
                lengthM={lengthM}
                crossSectionMm={DEFAULT_PROFILE_CROSS_SECTION}
                hexColor={profileColor}
                finish={profileFinish}
                quality={quality}
                position={[0, heightM, 0]}
              />
              <ProfileBar
                lengthM={lengthM}
                crossSectionMm={DEFAULT_PROFILE_CROSS_SECTION}
                hexColor={profileColor}
                finish={profileFinish}
                quality={quality}
                position={[0, 0, 0]}
              />
              <mesh
                position={[0, profileHalfM + glassHeightM / 2, 0]}
                material={glassMaterial}
                castShadow={settings.shadows}
                receiveShadow={settings.shadows}
              >
                <planeGeometry args={[glassWidthM, glassHeightM]} />
                <Edges color={EDGE_LINE_COLOR} threshold={15} />
              </mesh>
            </group>
          </group>
        );
      })}
      {vertices.map((vertex, index) => (
        <ProfileBar
          key={`post-${index}`}
          lengthM={heightM}
          crossSectionMm={CORNER_POST_CROSS_SECTION}
          hexColor={profileColor}
          finish={profileFinish}
          quality={quality}
          position={[vertex.xMm / 1000, heightM / 2, vertex.yMm / 1000]}
          rotation={[0, 0, Math.PI / 2]}
        />
      ))}
    </group>
  );
}
