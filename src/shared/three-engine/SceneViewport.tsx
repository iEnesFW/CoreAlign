import { Suspense, useEffect, useRef, useState, type ReactNode } from 'react';
import { Canvas, useThree } from '@react-three/fiber';
import {
  ContactShadows,
  Environment,
  Grid,
  OrbitControls,
  PerspectiveCamera,
} from '@react-three/drei';
import { QUALITY_SETTINGS, type QualityPreset } from './quality/qualityPreset';

export interface ViewportCamera {
  position: [number, number, number];
  target: [number, number, number];
  zoom: number;
}

interface OrbitLike {
  target: { x: number; y: number; z: number; set: (x: number, y: number, z: number) => void };
  update: () => void;
  addEventListener: (type: 'change', cb: () => void) => void;
  removeEventListener: (type: 'change', cb: () => void) => void;
}

const CAMERA_SAVE_DEBOUNCE_MS = 450;

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

          <ambientLight intensity={presentation ? 0.35 : 0.55} />
          <directionalLight
            position={[5, 8, 3]}
            intensity={presentation ? 1.4 : 1.1}
            castShadow={settings.shadows}
            shadow-mapSize-width={settings.shadowMapSize}
            shadow-mapSize-height={settings.shadowMapSize}
            shadow-camera-far={20}
            shadow-camera-left={-10}
            shadow-camera-right={10}
            shadow-camera-top={10}
            shadow-camera-bottom={-10}
          />
          <directionalLight position={[-4, 3, -4]} intensity={0.4} />

          {resolvedAppearance.environment !== 'none' && (
            <Suspense fallback={null}>
              <Environment preset={resolvedAppearance.environment} />
            </Suspense>
          )}

          {resolvedAppearance.ground && (
            <mesh rotation={[-Math.PI / 2, 0, 0]} position={[0, -0.002, 0]} receiveShadow>
              <circleGeometry args={[40, 64]} />
              <meshStandardMaterial
                color={resolvedAppearance.ground}
                roughness={0.95}
                metalness={0}
              />
            </mesh>
          )}

          {!presentation && (
            <Grid
              args={[40, 40]}
              cellSize={0.5}
              cellThickness={0.5}
              cellColor="#cbd5e1"
              sectionSize={2}
              sectionThickness={1}
              sectionColor="#94a3b8"
              fadeDistance={32}
              fadeStrength={1}
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
