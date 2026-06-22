import { ExtrudeGeometry, Shape } from 'three';

const CURVE_STEP_RAD = 0.08;

// WHY: spine passes through the local origin at phi=0; callers lay it flat with
// rotation [-π/2,0,0] so extrude depth becomes world-up height (shared glass + wall body).
export const buildCurvedBandGeometry = (
  radiusM: number,
  direction: 1 | -1,
  phiStart: number,
  phiEnd: number,
  thicknessM: number,
  depthM: number,
): ExtrudeGeometry => {
  const radius = Math.max(0.001, Number.isFinite(radiusM) ? radiusM : 0.001);
  const span = Math.max(1e-4, phiEnd - phiStart);
  const endPhi = phiStart + span;
  const centerY = -direction * radius;
  const outer = radius + thicknessM / 2;
  const inner = Math.max(0.001, radius - thicknessM / 2);
  const toAngle = (phi: number) => (direction === 1 ? Math.PI / 2 - phi : phi - Math.PI / 2);
  const outerClockwise = direction === 1;
  const shape = new Shape();
  shape.absarc(0, centerY, outer, toAngle(phiStart), toAngle(endPhi), outerClockwise);
  shape.absarc(0, centerY, inner, toAngle(endPhi), toAngle(phiStart), !outerClockwise);
  shape.closePath();
  const curveSegments = Math.max(8, Math.ceil(span / CURVE_STEP_RAD));
  return new ExtrudeGeometry(shape, { depth: depthM, bevelEnabled: false, curveSegments });
};
