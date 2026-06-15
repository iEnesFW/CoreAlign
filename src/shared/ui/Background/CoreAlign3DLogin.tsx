import { useEffect, useRef } from 'react';
import * as THREE from 'three';
import { EffectComposer } from 'three/addons/postprocessing/EffectComposer.js';
import { RenderPass } from 'three/addons/postprocessing/RenderPass.js';
import { UnrealBloomPass } from 'three/addons/postprocessing/UnrealBloomPass.js';
import styles from './CoreAlign3DLogin.module.css';
import { DARK_PARAMS, LIGHT_PARAMS } from './scene.config';
import type { Signal } from './scene.types';
import { getPathPoint, pickSignalColor } from './scene.utils';
import { createSignalMesh, setupLines } from './scene.objects';
import { CONSTANTS } from './scene.config';

interface CoreAlign3DLoginProps {
  theme?: 'light' | 'dark';
}

export const CoreAlign3DLogin = ({ theme = 'dark' }: CoreAlign3DLoginProps) => {
  const containerRef = useRef<HTMLDivElement>(null);
  const themeRef = useRef(theme);

  const sceneRef = useRef<THREE.Scene | null>(null);
  const bgMaterialRef = useRef<THREE.LineBasicMaterial | null>(null);
  const bloomPassRef = useRef<UnrealBloomPass | null>(null);
  const paramsRef = useRef(theme === 'light' ? { ...LIGHT_PARAMS } : { ...DARK_PARAMS });

  useEffect(() => {
    themeRef.current = theme;
    const targetParams = theme === 'light' ? LIGHT_PARAMS : DARK_PARAMS;

    Object.assign(paramsRef.current, targetParams);

    if (sceneRef.current) {
      sceneRef.current.background = new THREE.Color(targetParams.colorBg);
      (sceneRef.current.fog as THREE.FogExp2).color.set(targetParams.colorBg);
    }
    if (bgMaterialRef.current) {
      bgMaterialRef.current.color.set(targetParams.colorLine);
      bgMaterialRef.current.opacity = targetParams.lineOpacity;
    }
    if (bloomPassRef.current) {
      bloomPassRef.current.strength = targetParams.bloomStrength;
      bloomPassRef.current.radius = targetParams.bloomRadius;
    }
  }, [theme]);

  useEffect(() => {
    if (!containerRef.current) return;

    const params = paramsRef.current;

    const scene = new THREE.Scene();
    scene.background = new THREE.Color(params.colorBg);
    scene.fog = new THREE.FogExp2(params.colorBg, 0.002);
    sceneRef.current = scene;

    const camera = new THREE.PerspectiveCamera(45, window.innerWidth / window.innerHeight, 1, 1000);
    camera.position.set(0, 0, 90);
    camera.lookAt(0, 0, 0);

    const renderer = new THREE.WebGLRenderer({ antialias: true, alpha: false });
    renderer.setSize(window.innerWidth, window.innerHeight);
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));

    renderer.domElement.style.position = 'absolute';
    renderer.domElement.style.top = '0';
    renderer.domElement.style.left = '0';
    renderer.domElement.style.width = '100%';
    renderer.domElement.style.height = '100%';

    containerRef.current.appendChild(renderer.domElement);

    const leftGroup = new THREE.Group();
    const rightGroup = new THREE.Group();
    rightGroup.scale.x = -1;

    scene.add(leftGroup);
    scene.add(rightGroup);

    const updateGroupPositions = () => {
      leftGroup.position.set(params.positionX, params.positionY, 0);
      rightGroup.position.set(params.positionX, params.positionY, 0);
      leftGroup.rotation.z = THREE.MathUtils.degToRad(params.globalRotation);
      rightGroup.rotation.z = THREE.MathUtils.degToRad(-params.globalRotation);

      const fovRad = THREE.MathUtils.degToRad(45 / 2);
      const visibleHeight = 2 * Math.tan(fovRad) * 90;
      const visibleWidth = visibleHeight * (window.innerWidth / window.innerHeight);
      const scaleX = Math.max(1, visibleWidth / 2 / params.curveLength);
      leftGroup.scale.set(scaleX, 1, 1);
      rightGroup.scale.set(-scaleX, 1, 1);
    };
    updateGroupPositions();

    const renderScene = new RenderPass(scene, camera);
    const bloomPass = new UnrealBloomPass(
      new THREE.Vector2(window.innerWidth, window.innerHeight),
      1.5,
      0.4,
      0.85,
    );
    bloomPass.threshold = 0;
    bloomPass.strength = params.bloomStrength;
    bloomPass.radius = params.bloomRadius;
    bloomPassRef.current = bloomPass;

    const composer = new EffectComposer(renderer);
    composer.addPass(renderScene);
    composer.addPass(bloomPass);

    let backgroundLines: THREE.Line[] = [];
    let signals: Signal[] = [];

    const bgMaterial = new THREE.LineBasicMaterial({
      color: params.colorLine,
      transparent: true,
      opacity: params.lineOpacity,
      depthWrite: false,
    });
    bgMaterialRef.current = bgMaterial;

    const isLight = themeRef.current === 'light';
    const signalMaterial = new THREE.LineBasicMaterial({
      vertexColors: true,
      blending: isLight ? THREE.NormalBlending : THREE.AdditiveBlending,
      depthWrite: false,
      depthTest: false,
      transparent: true,
    });

    const rebuildAll = () => {
      leftGroup.children.forEach((c) => {
        const mesh = c as THREE.Mesh;
        if (mesh.geometry) mesh.geometry.dispose();
      });
      leftGroup.clear();
      rightGroup.clear();

      backgroundLines = [];
      signals = [];

      const linesLeft = setupLines(leftGroup, params, bgMaterial);
      backgroundLines.push(...linesLeft);

      linesLeft.forEach((line) => {
        const mirror = line.clone();
        rightGroup.add(mirror);
      });

      for (let i = 0; i < params.signalCount; i++) {
        const mesh = createSignalMesh(leftGroup, signalMaterial);
        const meshMirror = mesh.clone();
        rightGroup.add(meshMirror);

        signals.push({
          mesh,
          laneIndex: Math.floor(Math.random() * params.lineCount),
          speed: 0.2 + Math.random() * 0.5,
          progress: Math.random(),
          history: [],
          assignedColor: pickSignalColor(params),
        });
      }
      updateGroupPositions();
    };

    rebuildAll();

    let gui: { destroy: () => void } | null = null;
    if (import.meta.env.DEV) {
      void import('lil-gui').then(({ default: GUI }) => {
        if (!containerRef.current) return;
        const dev = new GUI({ title: 'Settings' });
        dev.domElement.style.position = 'absolute';
        dev.domElement.style.bottom = '10px';
        dev.domElement.style.right = '10px';
        dev.hide();
        containerRef.current.appendChild(dev.domElement);

        const folderColors = dev.addFolder('Colors');
        folderColors
          .addColor(params, 'colorBg')
          .name('Background')
          .onChange((v: string) => {
            scene.background = new THREE.Color(v);
            (scene.fog as THREE.FogExp2).color.set(v);
          });
        folderColors
          .addColor(params, 'colorLine')
          .name('Lines')
          .onChange((v: string) => {
            bgMaterial.color.set(v);
          });

        const folderGeneral = dev.addFolder('General');
        folderGeneral
          .add(params, 'globalRotation', -180, 180)
          .name('Rotation')
          .onChange(updateGroupPositions);
        folderGeneral
          .add(params, 'positionX', -200, 200)
          .name('Position X')
          .onChange(updateGroupPositions);
        folderGeneral
          .add(params, 'positionY', -100, 100)
          .name('Position Y')
          .onChange(updateGroupPositions);
        folderGeneral.add(params, 'lineCount', 10, 300, 1).name('Lines').onFinishChange(rebuildAll);

        gui = dev;
      });
    }

    const timer = new THREE.Timer();
    let frameId = 0;
    let paused = typeof document !== 'undefined' && document.hidden;

    const animate = () => {
      if (paused) {
        frameId = 0;
        return;
      }
      frameId = requestAnimationFrame(animate);
      timer.update();
      const time = timer.getElapsed();

      backgroundLines.forEach((line) => {
        const positions = line.geometry.attributes.position.array as Float32Array;
        const lineId = line.userData.id;
        for (let j = 0; j < CONSTANTS.segmentCount; j++) {
          const t = j / (CONSTANTS.segmentCount - 1);
          const vec = getPathPoint(t, lineId, time, params);
          positions[j * 3] = vec.x;
          positions[j * 3 + 1] = vec.y;
          positions[j * 3 + 2] = vec.z;
        }
        line.geometry.attributes.position.needsUpdate = true;
      });

      signals.forEach((sig) => {
        sig.progress += sig.speed * 0.005 * params.speedGlobal;
        if (sig.progress > 1.0) {
          sig.progress = 0;
          sig.laneIndex = Math.floor(Math.random() * params.lineCount);
          sig.history = [];
          sig.assignedColor = pickSignalColor(params);
        }

        const pos = getPathPoint(sig.progress, sig.laneIndex, time, params);
        sig.history.push(pos);
        if (sig.history.length > params.trailLength + 1) sig.history.shift();

        const positions = sig.mesh.geometry.attributes.position.array as Float32Array;
        const colors = sig.mesh.geometry.attributes.color.array as Float32Array;
        const drawCount = Math.max(1, params.trailLength);

        for (let i = 0; i < drawCount; i++) {
          let index = sig.history.length - 1 - i;
          if (index < 0) index = 0;
          const p = sig.history[index] || new THREE.Vector3();
          positions[i * 3] = p.x;
          positions[i * 3 + 1] = p.y;
          positions[i * 3 + 2] = p.z;

          let alpha = 1;
          if (params.trailLength > 0) alpha = Math.max(0, 1 - i / params.trailLength);
          colors[i * 3] = sig.assignedColor.r * alpha;
          colors[i * 3 + 1] = sig.assignedColor.g * alpha;
          colors[i * 3 + 2] = sig.assignedColor.b * alpha;
        }
        sig.mesh.geometry.setDrawRange(0, drawCount);
        sig.mesh.geometry.attributes.position.needsUpdate = true;
        sig.mesh.geometry.attributes.color.needsUpdate = true;
      });

      composer.render();
    };

    animate();

    const handleResize = () => {
      camera.aspect = window.innerWidth / window.innerHeight;
      camera.updateProjectionMatrix();
      renderer.setSize(window.innerWidth, window.innerHeight);
      composer.setSize(window.innerWidth, window.innerHeight);
      updateGroupPositions();
    };
    window.addEventListener('resize', handleResize);

    const handleVisibilityChange = () => {
      const isHidden = document.hidden;
      if (isHidden === paused) return;
      paused = isHidden;
      if (!paused && frameId === 0) {
        animate();
      }
    };
    document.addEventListener('visibilitychange', handleVisibilityChange);

    const currentContainer = containerRef.current;
    return () => {
      cancelAnimationFrame(frameId);
      window.removeEventListener('resize', handleResize);
      document.removeEventListener('visibilitychange', handleVisibilityChange);
      gui?.destroy();
      renderer.dispose();
      if (currentContainer) currentContainer.innerHTML = '';
    };
  }, []);

  return <div ref={containerRef} className={styles.container} />;
};
