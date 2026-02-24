import React, { useRef, useMemo } from 'react';
import { Canvas, useFrame } from '@react-three/fiber';
import { Line, Sphere } from '@react-three/drei';
import * as THREE from 'three';

const NetworkNode = ({ position }: { position: [number, number, number] }) => {
    return (
        <Sphere position={position} args={[0.05, 10, 10]}>
            <meshBasicMaterial color="#3b82f6" transparent opacity={0.6} />
        </Sphere>
    );
};

const ConnectionLine = ({ start, end, distance }: { start: THREE.Vector3, end: THREE.Vector3, distance: number }) => {
    const opacity = Math.max(0, 1 - distance / 2.5); // Fade out as distance increases
    return (
        <Line
            points={[start, end]}
            color="#3b82f6"
            transparent
            opacity={opacity * 0.3}
            lineWidth={1}
        />
    );
};

const NetworkField = () => {
    // Generate structured grid-like nodes with some randomness
    const count = 40;
    const nodes = useMemo(() => {
        const temp = [];
        for (let i = 0; i < count; i++) {
            temp.push({
                position: new THREE.Vector3(
                    (Math.random() - 0.5) * 12,
                    (Math.random() - 0.5) * 8,
                    (Math.random() - 0.5) * 5
                ),
                velocity: new THREE.Vector3(
                    (Math.random() - 0.5) * 0.005,
                    (Math.random() - 0.5) * 0.005,
                    0
                )
            });
        }
        return temp;
    }, []);

    const linesRef = useRef<any>(null);

    useFrame(() => {
        // Simple animation: move nodes slowly
        nodes.forEach(node => {
            node.position.add(node.velocity);
            // Bounce off boundaries roughly
            if (Math.abs(node.position.x) > 6) node.velocity.x *= -1;
            if (Math.abs(node.position.y) > 4) node.velocity.y *= -1;
        });

        // Force re-render of lines is implicit in React execution, 
        // but optimized implementations use instances. 
        // For <50 nodes, React re-render is fine for this demo.
    });

    // Calculate connections (brute force is fine for low N)
    const connections = [];
    for (let i = 0; i < count; i++) {
        for (let j = i + 1; j < count; j++) {
            const dist = nodes[i].position.distanceTo(nodes[j].position);
            if (dist < 2.5) {
                connections.push(
                    <ConnectionLine
                        key={`${i}-${j}`}
                        start={nodes[i].position}
                        end={nodes[j].position}
                        distance={dist}
                    />
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

export const ThreeBackground = () => {
    return (
        <div style={{ position: 'absolute', top: 0, left: 0, width: '100%', height: '100%', zIndex: 0, pointerEvents: 'none', background: 'radial-gradient(circle at center, #131316 0%, #0a0a0c 100%)' }}>
            <Canvas camera={{ position: [0, 0, 5], fov: 75 }}>
                {/* Subtle ambient light */}
                <ambientLight intensity={0.5} />
                <NetworkField />
            </Canvas>
        </div>
    );
};
