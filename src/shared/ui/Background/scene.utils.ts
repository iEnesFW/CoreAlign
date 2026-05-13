import * as THREE from 'three';
import type { SceneParams } from './scene.types';

export const getPathPoint = (
  t: number,
  lineIndex: number,
  time: number,
  params: SceneParams,
): THREE.Vector3 => {
  const totalLen = params.curveLength + params.straightLength;
  const currentX = -params.curveLength + t * totalLen;

  let y = 0;
  let z = 0;
  const spreadFactor = (lineIndex / params.lineCount - 0.5) * 2;

  if (currentX < 0) {
    const ratio = (currentX + params.curveLength) / params.curveLength;
    let shapeFactor = (Math.cos(ratio * Math.PI) + 1) / 2;
    shapeFactor = Math.pow(shapeFactor, params.curvePower);

    y = spreadFactor * params.spreadHeight * shapeFactor;
    z = spreadFactor * params.spreadDepth * shapeFactor;

    const waveFactor = shapeFactor;
    const wave =
      Math.sin(time * params.waveSpeed + currentX * 0.1 + lineIndex) *
      params.waveHeight *
      waveFactor;
    y += wave;
  }

  return new THREE.Vector3(currentX, y, z);
};

export const pickSignalColor = (params: SceneParams): THREE.Color => {
  const choices = [new THREE.Color(params.colorSignal)];
  if (params.useColor2) choices.push(new THREE.Color(params.colorSignal2));
  if (params.useColor3) choices.push(new THREE.Color(params.colorSignal3));
  return choices[Math.floor(Math.random() * choices.length)];
};
