import * as THREE from 'three';
import { SceneParams } from './scene.types';
import { CONSTANTS } from './scene.config';
import { getPathPoint, pickSignalColor } from './scene.utils';

export const setupLines = (group: THREE.Group, params: SceneParams, material: THREE.LineBasicMaterial): THREE.Line[] => {
    const lines: THREE.Line[] = [];
    for (let i = 0; i < params.lineCount; i++) {
        const geometry = new THREE.BufferGeometry();
        const positions = new Float32Array(CONSTANTS.segmentCount * 3);
        geometry.setAttribute('position', new THREE.BufferAttribute(positions, 3));

        const line = new THREE.Line(geometry, material);
        line.userData = { id: i };
        line.renderOrder = 0;
        group.add(line);
        lines.push(line);
    }
    return lines;
};

export const createSignalMesh = (group: THREE.Group, material: THREE.LineBasicMaterial): THREE.Line => {
    const maxTrail = 150;
    const geometry = new THREE.BufferGeometry();
    const positions = new Float32Array(maxTrail * 3);
    const colors = new Float32Array(maxTrail * 3);

    geometry.setAttribute('position', new THREE.BufferAttribute(positions, 3));
    geometry.setAttribute('color', new THREE.BufferAttribute(colors, 3));

    const mesh = new THREE.Line(geometry, material);
    mesh.frustumCulled = false;
    mesh.renderOrder = 1;

    group.add(mesh);
    return mesh;
};
