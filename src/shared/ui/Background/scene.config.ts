import { SceneParams, SceneConstants } from './scene.types';

export const DARK_PARAMS: SceneParams = {
    colorBg: '#080808',
    colorLine: '#373f48',
    colorSignal: '#8fc9ff',
    useColor2: false,
    colorSignal2: '#ff0055',
    useColor3: false,
    colorSignal3: '#ffcc00',
    lineCount: 80,
    globalRotation: 0,
    positionX: 0,
    positionY: 0,
    spreadHeight: 45,
    spreadDepth: 0,
    curveLength: 80,
    straightLength: 5,
    curvePower: 0.8265,
    waveSpeed: 2.48,
    waveHeight: 0.145,
    lineOpacity: 0.557,
    signalCount: 94,
    speedGlobal: 0.345,
    trailLength: 3,
    bloomStrength: 3.0,
    bloomRadius: 0.5,
    bloomThreshold: 0
};

export const LIGHT_PARAMS: SceneParams = {
    colorBg: '#e8ecf2',
    colorLine: '#64748b',
    colorSignal: '#3b82f6',
    useColor2: false,
    colorSignal2: '#e11d48',
    useColor3: false,
    colorSignal3: '#f59e0b',
    lineCount: 80,
    globalRotation: 0,
    positionX: 0,
    positionY: 0,
    spreadHeight: 45,
    spreadDepth: 0,
    curveLength: 80,
    straightLength: 5,
    curvePower: 0.8265,
    waveSpeed: 2.48,
    waveHeight: 0.145,
    lineOpacity: 0.45,
    signalCount: 94,
    speedGlobal: 0.345,
    trailLength: 3,
    bloomStrength: 0.8,
    bloomRadius: 0.3,
    bloomThreshold: 0.85
};

export const DEFAULT_PARAMS: SceneParams = DARK_PARAMS;

export const CONSTANTS: SceneConstants = {
    segmentCount: 150
};
