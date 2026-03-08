import * as THREE from 'three';

export interface SceneParams {
    colorBg: string;
    colorLine: string;
    colorSignal: string;
    useColor2: boolean;
    colorSignal2: string;
    useColor3: boolean;
    colorSignal3: string;
    lineCount: number;
    globalRotation: number;
    positionX: number;
    positionY: number;
    spreadHeight: number;
    spreadDepth: number;
    curveLength: number;
    straightLength: number;
    curvePower: number;
    waveSpeed: number;
    waveHeight: number;
    lineOpacity: number;
    signalCount: number;
    speedGlobal: number;
    trailLength: number;
    bloomStrength: number;
    bloomRadius: number;
    bloomThreshold: number;
}

export interface Signal {
    mesh: THREE.Line;
    laneIndex: number;
    speed: number;
    progress: number;
    history: THREE.Vector3[];
    assignedColor: THREE.Color;
}

export interface SceneConstants {
    segmentCount: number;
}
