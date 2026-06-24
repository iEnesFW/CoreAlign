import { Canvas, useFrame } from '@react-three/fiber';
import { Line, Sphere } from '@react-three/drei';
import * as THREE from 'three';

const NetworkNode = ({ position }: { position: [number, number, number] }) => (
  <Sphere position={position} args={[0.05, 10, 10]}>
    <meshBasicMaterial color="#3b82f6" transparent opacity={0.6} />
  </Sphere>
);

const ConnectionLine = ({
  start,
  end,
  distance,
}: {
  start: THREE.Vector3;
  end: THREE.Vector3;
  distance: number;
}) => {
  const opacity = Math.max(0, 1 - distance / 2.5);
  return (
    <Line points={[start, end]} color="#3b82f6" transparent opacity={opacity * 0.3} lineWidth={1} />
  );
};

interface FieldNode {
  position: THREE.Vector3;
  velocity: THREE.Vector3;
}

const NODE_COUNT = 40;
const CONNECTION_DISTANCE = 2.5;
const RNG_SEED = 0x9e3779b9;

const createRng = (seed: number) => {
  let state = seed >>> 0;
  return () => {
    state = (state + 0x6d2b79f5) >>> 0;
    let t = Math.imul(state ^ (state >>> 15), 1 | state);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
};

const buildNodes = (): FieldNode[] => {
  const rng = createRng(RNG_SEED);
  const generated: FieldNode[] = [];
  for (let i = 0; i < NODE_COUNT; i++) {
    generated.push({
      position: new THREE.Vector3((rng() - 0.5) * 12, (rng() - 0.5) * 8, (rng() - 0.5) * 5),
      velocity: new THREE.Vector3((rng() - 0.5) * 0.005, (rng() - 0.5) * 0.005, 0),
    });
  }
  return generated;
};

const nodes = buildNodes();

const NetworkField = () => {
  useFrame(() => {
    for (const node of nodes) {
      node.position.add(node.velocity);
      if (Math.abs(node.position.x) > 6) node.velocity.x *= -1;
      if (Math.abs(node.position.y) > 4) node.velocity.y *= -1;
    }
  });

  const connections = [];
  for (let i = 0; i < nodes.length; i++) {
    for (let j = i + 1; j < nodes.length; j++) {
      const dist = nodes[i].position.distanceTo(nodes[j].position);
      if (dist < CONNECTION_DISTANCE) {
        connections.push(
          <ConnectionLine
            key={`${i}-${j}`}
            start={nodes[i].position}
            end={nodes[j].position}
            distance={dist}
          />,
        );
      }
    }
  }

  return (
    <group>
      {nodes.map((node, i) => (
        <NetworkNode key={i} position={[node.position.x, node.position.y, node.position.z]} />
      ))}
      {connections}
    </group>
  );
};

export const ThreeBackground = () => (
  <div
    style={{
      position: 'absolute',
      top: 0,
      left: 0,
      width: '100%',
      height: '100%',
      zIndex: 0,
      pointerEvents: 'none',
      background: 'radial-gradient(circle at center, #131316 0%, #0a0a0c 100%)',
    }}
  >
    <Canvas camera={{ position: [0, 0, 5], fov: 75 }}>
      <ambientLight intensity={0.5} />
      <NetworkField />
    </Canvas>
  </div>
);
