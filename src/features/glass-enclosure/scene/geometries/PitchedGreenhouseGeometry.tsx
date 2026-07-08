import { useEffect, useMemo } from 'react';
import { BufferGeometry, Float32BufferAttribute, Shape, ExtrudeGeometry } from 'three';
import { Edges } from '@react-three/drei';
import { useGlassMaterial } from '../materials/glassMaterial';
import { QUALITY_SETTINGS, type QualityPreset } from '@/shared/three-engine';
import { ProfileBar } from '../builders/ProfileBar';
import type { SceneRunState } from '../../model/project.types';
import type {
  ColorFinishType,
  ColorOptionDto,
  GlassTypeDto,
} from '../../model/glassEnclosure.types';

interface PitchedGreenhouseGeometryProps {
  runs: SceneRunState[];
  roofPitchDeg: number;
  ridgeHeightMm: number;
  eaveHeightMm: number;
  quality: QualityPreset;
  glassTypes: Map<string, GlassTypeDto>;
  colors: Map<string, ColorOptionDto>;
}

const DEFAULT_GLASS_THICKNESS_MM = 8;
const DEFAULT_FOOTPRINT_LENGTH_MM = 6000;
const DEFAULT_PROFILE_CROSS_SECTION = { width: 50, height: 60 };
const DEFAULT_HEX_COLOR = '#cfd5d9';

interface Footprint {
  lengthM: number;
  widthM: number;
  eaveM: number;
  ridgeM: number;
  halfWidthM: number;
  slopeLengthM: number;
  pitchRad: number;
}

const computeFootprint = (
  runs: SceneRunState[],
  roofPitchDeg: number,
  ridgeHeightMm: number,
  eaveHeightMm: number,
): Footprint => {
  const longestLength = runs.reduce((acc, run) => Math.max(acc, run.lengthMm), 0);
  const shortestLength = runs.reduce(
    (acc, run) => (run.lengthMm > 0 ? Math.min(acc, run.lengthMm) : acc),
    Number.POSITIVE_INFINITY,
  );
  const lengthMm = longestLength > 0 ? longestLength : DEFAULT_FOOTPRINT_LENGTH_MM;
  const widthMm =
    shortestLength !== Number.POSITIVE_INFINITY && shortestLength > 0
      ? shortestLength
      : Math.max(2000, Math.round(lengthMm * 0.6));

  const lengthM = lengthMm / 1000;
  const widthM = widthMm / 1000;
  const eaveM = eaveHeightMm / 1000;
  const halfWidthM = widthM / 2;
  // WHY: the roof PANELS were rotated by the user's roofPitchDeg while their length/position came
  // from ridge/eave/width — two different slopes, so a panel clipped through or floated above the
  // gable. Reconcile: when a pitch is set it drives the ridge (gable+slope+panels then share ONE
  // slope); then derive pitchRad from the actual rise so the panel rotation always matches the gable.
  const ridgeM =
    roofPitchDeg > 0
      ? eaveM + halfWidthM * Math.tan((roofPitchDeg * Math.PI) / 180)
      : ridgeHeightMm / 1000;
  const verticalRise = Math.max(0.01, ridgeM - eaveM);
  const pitchRad = Math.atan2(verticalRise, halfWidthM);
  const slopeLengthM = Math.sqrt(halfWidthM * halfWidthM + verticalRise * verticalRise);
  return { lengthM, widthM, eaveM, ridgeM, halfWidthM, slopeLengthM, pitchRad };
};

const useDisposeOnChange = (geometry: BufferGeometry) => {
  useEffect(() => () => geometry.dispose(), [geometry]);
};

const useGableGeometry = (footprint: Footprint, thicknessM: number): BufferGeometry => {
  const geometry = useMemo(() => {
    const { halfWidthM, eaveM, ridgeM } = footprint;
    const shape = new Shape();
    shape.moveTo(-halfWidthM, eaveM);
    shape.lineTo(halfWidthM, eaveM);
    shape.lineTo(0, ridgeM);
    shape.lineTo(-halfWidthM, eaveM);
    const extrude = new ExtrudeGeometry(shape, {
      depth: thicknessM,
      bevelEnabled: false,
    });
    extrude.translate(0, 0, -thicknessM / 2);
    return extrude;
  }, [footprint, thicknessM]);
  useDisposeOnChange(geometry);
  return geometry;
};

const useWallGeometry = (widthM: number, heightM: number, thicknessM: number) => {
  const geometry = useMemo(() => {
    const geom = new BufferGeometry();
    const halfWidth = widthM / 2;
    const halfThickness = thicknessM / 2;
    const vertices = new Float32Array([
      -halfWidth,
      0,
      halfThickness,
      halfWidth,
      0,
      halfThickness,
      halfWidth,
      heightM,
      halfThickness,
      -halfWidth,
      heightM,
      halfThickness,
    ]);
    geom.setAttribute('position', new Float32BufferAttribute(vertices, 3));
    geom.setIndex([0, 1, 2, 0, 2, 3]);
    geom.computeVertexNormals();
    return geom;
  }, [widthM, heightM, thicknessM]);
  useDisposeOnChange(geometry);
  return geometry;
};

const useRoofPanelGeometry = (lengthM: number, slopeLengthM: number, thicknessM: number) => {
  const geometry = useMemo(() => {
    const geom = new BufferGeometry();
    const halfLength = lengthM / 2;
    const halfSlope = slopeLengthM / 2;
    const halfThickness = thicknessM / 2;
    const vertices = new Float32Array([
      -halfLength,
      -halfSlope,
      halfThickness,
      halfLength,
      -halfSlope,
      halfThickness,
      halfLength,
      halfSlope,
      halfThickness,
      -halfLength,
      halfSlope,
      halfThickness,
    ]);
    geom.setAttribute('position', new Float32BufferAttribute(vertices, 3));
    geom.setIndex([0, 1, 2, 0, 2, 3]);
    geom.computeVertexNormals();
    return geom;
  }, [lengthM, slopeLengthM, thicknessM]);
  useDisposeOnChange(geometry);
  return geometry;
};

export function PitchedGreenhouseGeometry({
  runs,
  roofPitchDeg,
  ridgeHeightMm,
  eaveHeightMm,
  quality,
  glassTypes,
  colors,
}: PitchedGreenhouseGeometryProps) {
  const sampleRun = runs[0] ?? null;
  const sampleGlassType = sampleRun?.panels[0]?.glassTypeId
    ? glassTypes.get(sampleRun.panels[0].glassTypeId)
    : null;
  const glassThicknessMm = sampleGlassType?.thicknessMm ?? DEFAULT_GLASS_THICKNESS_MM;
  const glassThicknessM = glassThicknessMm / 1000;

  const sampleColor = sampleRun?.colorId ? (colors.get(sampleRun.colorId) ?? null) : null;
  const profileColor = sampleColor?.hexColor ?? DEFAULT_HEX_COLOR;
  const profileFinish: ColorFinishType = sampleColor?.finishType ?? 'PowderCoated';

  const footprint = useMemo(
    () => computeFootprint(runs, roofPitchDeg, ridgeHeightMm, eaveHeightMm),
    [runs, roofPitchDeg, ridgeHeightMm, eaveHeightMm],
  );

  const gableGeometry = useGableGeometry(footprint, glassThicknessM);
  const sideWallGeometry = useWallGeometry(footprint.lengthM, footprint.eaveM, glassThicknessM);
  const endWallGeometry = useWallGeometry(footprint.widthM, footprint.eaveM, glassThicknessM);
  const roofPanelGeometry = useRoofPanelGeometry(
    footprint.lengthM,
    footprint.slopeLengthM,
    glassThicknessM,
  );

  const glassMaterial = useGlassMaterial({ quality, thicknessMm: glassThicknessMm });
  const settings = QUALITY_SETTINGS[quality];

  const halfLength = footprint.lengthM / 2;
  const halfWidth = footprint.halfWidthM;
  const slopeCenterY = (footprint.eaveM + footprint.ridgeM) / 2;
  const slopeCenterZ = halfWidth / 2;

  return (
    <group>
      <mesh
        position={[0, 0, -halfLength]}
        geometry={gableGeometry}
        material={glassMaterial}
        castShadow={settings.shadows}
        receiveShadow={settings.shadows}
      >
        <Edges color="#9aacb5" threshold={15} />
      </mesh>
      <mesh
        position={[0, 0, halfLength]}
        geometry={gableGeometry}
        material={glassMaterial}
        castShadow={settings.shadows}
        receiveShadow={settings.shadows}
      >
        <Edges color="#9aacb5" threshold={15} />
      </mesh>

      <mesh
        position={[-halfWidth, 0, 0]}
        rotation={[0, Math.PI / 2, 0]}
        geometry={sideWallGeometry}
        material={glassMaterial}
        castShadow={settings.shadows}
        receiveShadow={settings.shadows}
      >
        <Edges color="#9aacb5" threshold={15} />
      </mesh>
      <mesh
        position={[halfWidth, 0, 0]}
        rotation={[0, Math.PI / 2, 0]}
        geometry={sideWallGeometry}
        material={glassMaterial}
        castShadow={settings.shadows}
        receiveShadow={settings.shadows}
      >
        <Edges color="#9aacb5" threshold={15} />
      </mesh>

      <mesh
        position={[0, 0, -halfLength]}
        geometry={endWallGeometry}
        material={glassMaterial}
        castShadow={settings.shadows}
        receiveShadow={settings.shadows}
      >
        <Edges color="#9aacb5" threshold={15} />
      </mesh>
      <mesh
        position={[0, 0, halfLength]}
        geometry={endWallGeometry}
        material={glassMaterial}
        castShadow={settings.shadows}
        receiveShadow={settings.shadows}
      >
        <Edges color="#9aacb5" threshold={15} />
      </mesh>

      <group position={[0, slopeCenterY, -slopeCenterZ]} rotation={[footprint.pitchRad, 0, 0]}>
        <mesh
          geometry={roofPanelGeometry}
          material={glassMaterial}
          castShadow={settings.shadows}
          receiveShadow={settings.shadows}
        >
          <Edges color="#9aacb5" threshold={15} />
        </mesh>
      </group>
      <group position={[0, slopeCenterY, slopeCenterZ]} rotation={[-footprint.pitchRad, 0, 0]}>
        <mesh
          geometry={roofPanelGeometry}
          material={glassMaterial}
          castShadow={settings.shadows}
          receiveShadow={settings.shadows}
        >
          <Edges color="#9aacb5" threshold={15} />
        </mesh>
      </group>

      <ProfileBar
        lengthM={footprint.lengthM}
        crossSectionMm={DEFAULT_PROFILE_CROSS_SECTION}
        hexColor={profileColor}
        finish={profileFinish}
        quality={quality}
        position={[0, footprint.ridgeM, 0]}
      />
      <ProfileBar
        lengthM={footprint.lengthM}
        crossSectionMm={DEFAULT_PROFILE_CROSS_SECTION}
        hexColor={profileColor}
        finish={profileFinish}
        quality={quality}
        position={[-halfWidth, footprint.eaveM, 0]}
      />
      <ProfileBar
        lengthM={footprint.lengthM}
        crossSectionMm={DEFAULT_PROFILE_CROSS_SECTION}
        hexColor={profileColor}
        finish={profileFinish}
        quality={quality}
        position={[halfWidth, footprint.eaveM, 0]}
      />
    </group>
  );
}
