import { Suspense, useEffect, useMemo, useRef, useState, type ReactNode } from 'react';
import { Canvas, useStore, useThree } from '@react-three/fiber';
import {
  ContactShadows,
  Environment,
  Grid,
  OrbitControls,
  PerspectiveCamera,
  Sky,
} from '@react-three/drei';
import { Box3, RepeatWrapping, Sphere, Vector3, type Texture } from 'three';
import { registerViewportCamera } from './viewportCamera';
import { QUALITY_SETTINGS, type QualityPreset } from './quality/qualityPreset';
import { installAcceleratedRaycast } from './acceleratedRaycast';
import {
  getProceduralTexture,
  isProceduralMaterialKey,
  type ProceduralMaterialKey,
} from './materials/proceduralTextures';

installAcceleratedRaycast();

export interface ViewportCamera {
  position: [number, number, number];
  target: [number, number, number];
  zoom: number;
}

interface OrbitLike {
  target: { x: number; y: number; z: number; set: (x: number, y: number, z: number) => void };
  minDistance?: number;
  maxDistance?: number;
  update: () => void;
  addEventListener: (type: 'change', cb: () => void) => void;
  removeEventListener: (type: 'change', cb: () => void) => void;
}

const CAMERA_SAVE_DEBOUNCE_MS = 450;

/**
 * Dev/E2E-only handle on the live render surface: scene, camera, renderer and canvas.
 *
 * WHY this is needed to test the designer at all: the store hook proves what was AUTHORED, but the
 * defects that matter here happen AFTER that — a carve that comes out inside-out, a body that
 * renders somewhere other than where its numbers say. Reading the scene graph catches those. The
 * CAMERA is the second half: driving a real pointer drag means knowing which PIXEL a corner handle
 * occupies, and the only honest way to get that is to project the world point through the same
 * camera the renderer uses.
 */
function RenderSurfaceExporter() {
  // WHY: useStore() is the stable zustand store; useThree() returns a fresh state object on every
  // camera/size change, which would tear down and re-install the handle constantly and leave a
  // window where the harness sees it missing.
  const store = useStore();
  useEffect(() => {
    const w = window as unknown as {
      __E2E__?: boolean;
      __CAD_R3F__?: () => unknown;
    };
    if (!import.meta.env.DEV && !w.__E2E__) return;
    const handle = () => store.getState();
    w.__CAD_R3F__ = handle;
    return () => {
      // WHY: each Canvas owns its own React root, so a remount can run the OLD root's cleanup
      // AFTER the new root's effect. Deleting unconditionally would leave the handle pointing at a
      // disposed renderer whose GL context is lost. Only retract our own registration.
      if (w.__CAD_R3F__ === handle) delete w.__CAD_R3F__;
    };
  }, [store]);
  return null;
}

const FIT_MARGIN = 1.35;
const FRAMED_NDC_MARGIN = 0.9;

/**
 * Gives the toolbar a real camera to drive.
 *
 * WHY this exists: the canvas zoom/fit buttons took optional props that nothing ever passed, so
 * they rendered permanently disabled — and a project whose geometry sits away from the origin
 * opened with the content off-frame and no way to recover except manually orbiting. Measured on a
 * real project: content projected to screen x 1249..2829 while the canvas spanned 596..1208.
 */
function CameraCommands() {
  const camera = useThree((s) => s.camera);
  const controls = useThree((s) => s.controls) as unknown as OrbitLike | null;
  const scene = useThree((s) => s.scene);
  const size = useThree((s) => s.size);

  useEffect(() => {
    if (!controls) return;

    const perspective = camera as typeof camera & { fov?: number; aspect?: number };

    const viewDirection = () => {
      const dir = new Vector3(
        camera.position.x - controls.target.x,
        camera.position.y - controls.target.y,
        camera.position.z - controls.target.z,
      );
      // A degenerate vector happens when the camera sits exactly on its target; fall back to the
      // default three-quarter view rather than producing NaN.
      if (dir.lengthSq() < 1e-9) dir.set(0.6, 0.45, 0.78);
      return dir.normalize();
    };

    const clampDistance = (value: number) => {
      const min = typeof controls.minDistance === 'number' ? controls.minDistance : 0.01;
      const max = typeof controls.maxDistance === 'number' ? controls.maxDistance : Infinity;
      return Math.min(Math.max(value, min), max);
    };

    // WHY not Box3.setFromObject: it swallows the whole subtree, including invisible bodies,
    // annotation text and empty groups parked at the origin. Measured: that inflated box reached
    // from the origin to the geometry, so "is the content on screen?" answered yes while the user
    // saw empty grid. Only visible meshes count as content.
    const contentBox = (objectName: string) => {
      const target = scene.getObjectByName(objectName);
      if (!target) return null;
      const box = new Box3();
      let found = false;
      target.traverse((child) => {
        const mesh = child as typeof child & {
          isMesh?: boolean;
          geometry?: { boundingBox: Box3 | null; computeBoundingBox: () => void };
        };
        if (!mesh.isMesh || !child.visible || !mesh.geometry) return;
        if (!mesh.geometry.boundingBox) mesh.geometry.computeBoundingBox();
        const local = mesh.geometry.boundingBox;
        if (!local) return;
        child.updateWorldMatrix(true, false);
        box.union(local.clone().applyMatrix4(child.matrixWorld));
        found = true;
      });
      return found && !box.isEmpty() ? box : null;
    };

    const api = {
      fitTo: (objectName: string) => {
        const box = contentBox(objectName);
        if (!box) return false;

        const sphere = box.getBoundingSphere(new Sphere());
        if (!Number.isFinite(sphere.radius) || sphere.radius <= 0) return false;

        // Frame against the NARROWER of the two half-angles: a tall thin viewport clips the width
        // if you only respect the vertical field of view.
        const fovDeg = perspective.fov ?? 45;
        const vHalf = (fovDeg * Math.PI) / 360;
        const aspect =
          perspective.aspect ?? (size.width > 0 && size.height > 0 ? size.width / size.height : 1);
        const hHalf = Math.atan(Math.tan(vHalf) * Math.max(aspect, 0.0001));
        const half = Math.max(Math.min(vHalf, hHalf), 0.05);

        const distance = clampDistance((sphere.radius / Math.sin(half)) * FIT_MARGIN);
        const dir = viewDirection();
        controls.target.set(sphere.center.x, sphere.center.y, sphere.center.z);
        camera.position.set(
          sphere.center.x + dir.x * distance,
          sphere.center.y + dir.y * distance,
          sphere.center.z + dir.z * distance,
        );
        camera.updateProjectionMatrix();
        controls.update();
        return true;
      },
      framesObject: (objectName: string) => {
        const box = contentBox(objectName);
        if (!box) return false;

        camera.updateMatrixWorld();
        camera.updateProjectionMatrix();
        // WHY the CENTRE and not "any corner overlaps the frustum": measured on a real project,
        // one corner of the content box grazed the frustum edge while the whole run sat off to the
        // right of the viewport — an any-overlap test called that "framed" and suppressed the fit,
        // leaving the user staring at empty grid. The margin keeps a body that is merely clipped at
        // the edge from being yanked around.
        const centre = box.getCenter(new Vector3()).project(camera);
        return (
          centre.z <= 1 &&
          Math.abs(centre.x) <= FRAMED_NDC_MARGIN &&
          Math.abs(centre.y) <= FRAMED_NDC_MARGIN
        );
      },
      zoomBy: (factor: number) => {
        if (!Number.isFinite(factor) || factor <= 0) return false;
        const dir = viewDirection();
        const currentDistance = Math.hypot(
          camera.position.x - controls.target.x,
          camera.position.y - controls.target.y,
          camera.position.z - controls.target.z,
        );
        const next = clampDistance(currentDistance * factor);
        camera.position.set(
          controls.target.x + dir.x * next,
          controls.target.y + dir.y * next,
          controls.target.z + dir.z * next,
        );
        controls.update();
        return true;
      },
    };

    return registerViewportCamera(api);
  }, [camera, controls, scene, size]);

  return null;
}

function CameraSync({
  initialCamera,
  onChange,
}: {
  initialCamera?: ViewportCamera;
  onChange?: (camera: ViewportCamera) => void;
}) {
  const camera = useThree((s) => s.camera);
  const controls = useThree((s) => s.controls) as unknown as OrbitLike | null;
  const appliedRef = useRef(false);

  useEffect(() => {
    if (appliedRef.current || !initialCamera || !controls) return;
    appliedRef.current = true;
    camera.position.set(...initialCamera.position);
    controls.target.set(...initialCamera.target);
    controls.update();
  }, [initialCamera, controls, camera]);

  useEffect(() => {
    if (!controls || !onChange) return;
    let timer = 0;
    const handler = () => {
      window.clearTimeout(timer);
      timer = window.setTimeout(() => {
        onChange({
          position: [camera.position.x, camera.position.y, camera.position.z],
          target: [controls.target.x, controls.target.y, controls.target.z],
          zoom: camera.zoom,
        });
      }, CAMERA_SAVE_DEBOUNCE_MS);
    };
    controls.addEventListener('change', handler);
    return () => {
      controls.removeEventListener('change', handler);
      window.clearTimeout(timer);
    };
  }, [controls, camera, onChange]);

  return null;
}

export interface ViewportAppearance {
  environment: 'apartment' | 'sunset' | 'city' | 'dawn' | 'none';
  background: string;
  ground?: string | null;
  groundTexture?: ProceduralMaterialKey | null;
  sky?: boolean;
  sunPosition?: [number, number, number];
}

function GroundPlane({
  appearance,
  receiveShadow,
}: {
  appearance: ViewportAppearance;
  receiveShadow: boolean;
}) {
  const texture = useMemo<Texture | null>(() => {
    const key = appearance.groundTexture;
    if (!key || !isProceduralMaterialKey(key)) return null;
    const clone = getProceduralTexture(key).clone();
    clone.wrapS = RepeatWrapping;
    clone.wrapT = RepeatWrapping;
    clone.repeat.set(56, 56);
    clone.needsUpdate = true;
    return clone;
  }, [appearance.groundTexture]);

  useEffect(() => () => texture?.dispose(), [texture]);

  if (!appearance.ground && !texture) return null;

  return (
    <mesh rotation={[-Math.PI / 2, 0, 0]} position={[0, -0.002, 0]} receiveShadow={receiveShadow}>
      <circleGeometry args={[texture ? 70 : 40, 96]} />
      <meshStandardMaterial
        map={texture ?? undefined}
        color={texture ? '#ffffff' : (appearance.ground ?? '#e5e7eb')}
        roughness={appearance.groundTexture === 'asphalt' ? 0.9 : 0.97}
        metalness={0}
      />
    </mesh>
  );
}

interface SceneViewportProps {
  quality: QualityPreset;
  presentation?: boolean;
  appearance?: ViewportAppearance;
  cameraPosition?: [number, number, number];
  cameraTarget?: [number, number, number];
  initialCamera?: ViewportCamera;
  onCameraChange?: (camera: ViewportCamera) => void;
  onPointerMissed?: () => void;
  children: ReactNode;
}

export function SceneViewport({
  quality,
  presentation = false,
  appearance,
  cameraPosition = [3.5, 2.6, 4.5],
  cameraTarget = [0, 1.2, 0],
  initialCamera,
  onCameraChange,
  onPointerMissed,
  children,
}: SceneViewportProps) {
  const settings = QUALITY_SETTINGS[quality];
  const resolvedAppearance: ViewportAppearance = appearance ?? {
    environment: presentation ? 'sunset' : 'apartment',
    background: presentation ? '#0f172a' : '#f1f5f9',
  };
  const sunPosition: [number, number, number] = resolvedAppearance.sunPosition ?? [6, 7, 5];
  const containerRef = useRef<HTMLDivElement | null>(null);
  const [measured, setMeasured] = useState(false);

  useEffect(() => {
    const el = containerRef.current;
    if (!el) return;

    const check = (width: number, height: number) => {
      if (width > 0 && height > 0) setMeasured(true);
    };

    check(el.clientWidth, el.clientHeight);

    const observer = new ResizeObserver((entries) => {
      for (const entry of entries) {
        check(entry.contentRect.width, entry.contentRect.height);
      }
    });
    observer.observe(el);

    let raf = 0;
    if (el.clientWidth === 0 || el.clientHeight === 0) {
      raf = requestAnimationFrame(() => check(el.clientWidth, el.clientHeight));
    }

    return () => {
      observer.disconnect();
      if (raf) cancelAnimationFrame(raf);
    };
  }, []);

  return (
    <div ref={containerRef} className="h-full w-full">
      {measured && (
        <Canvas
          shadows={settings.shadows}
          gl={{ antialias: settings.antialias, preserveDrawingBuffer: true }}
          dpr={[1, settings.pixelRatioMax]}
          onPointerMissed={onPointerMissed}
        >
          <color attach="background" args={[resolvedAppearance.background]} />
          {resolvedAppearance.sky && (
            <Sky
              distance={450000}
              sunPosition={sunPosition}
              turbidity={2.4}
              rayleigh={0.9}
              mieCoefficient={0.004}
              mieDirectionalG={0.85}
            />
          )}
          <PerspectiveCamera makeDefault position={cameraPosition} fov={45} near={0.1} far={120} />
          <OrbitControls
            makeDefault
            enableDamping
            dampingFactor={0.08}
            target={cameraTarget}
            minDistance={1.2}
            maxDistance={40}
            minPolarAngle={Math.PI / 6}
            maxPolarAngle={Math.PI / 2.05}
          />
          <CameraSync initialCamera={initialCamera} onChange={onCameraChange} />
          <CameraCommands />
          <RenderSurfaceExporter />

          <hemisphereLight
            args={['#dce6f2', resolvedAppearance.ground ?? '#8a9099', presentation ? 0.35 : 0.55]}
          />
          <ambientLight intensity={presentation ? 0.25 : 0.4} />
          <directionalLight
            position={sunPosition}
            intensity={presentation ? 1.5 : 1.2}
            castShadow={settings.shadows}
            shadow-mapSize-width={settings.shadowMapSize}
            shadow-mapSize-height={settings.shadowMapSize}
            shadow-camera-far={20}
            shadow-camera-left={-10}
            shadow-camera-right={10}
            shadow-camera-top={10}
            shadow-camera-bottom={-10}
            shadow-bias={-0.0002}
          />
          <directionalLight position={[-4, 3, -4]} intensity={0.35} />

          {resolvedAppearance.environment !== 'none' && (
            <Suspense fallback={null}>
              <Environment preset={resolvedAppearance.environment} />
            </Suspense>
          )}

          <GroundPlane appearance={resolvedAppearance} receiveShadow={settings.shadows} />

          {!presentation && (
            <Grid
              args={[60, 60]}
              cellSize={0.5}
              cellThickness={0.7}
              cellColor="#94a3b8"
              sectionSize={2}
              sectionThickness={1.3}
              sectionColor="#475569"
              fadeDistance={60}
              fadeStrength={1.5}
              followCamera={false}
              infiniteGrid={false}
              position={[0, 0, 0]}
            />
          )}

          {settings.shadows && (
            <ContactShadows
              position={[0, 0.001, 0]}
              opacity={0.55}
              scale={40}
              blur={2.5}
              far={4}
              resolution={1024}
              color="#0f172a"
            />
          )}

          {children}
        </Canvas>
      )}
    </div>
  );
}
