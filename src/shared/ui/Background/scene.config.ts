import { SceneParams, SceneConstants } from './scene.types';

export const DEFAULT_PARAMS: SceneParams = {
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
    bloomRadius: 0.5
};

export const CONSTANTS: SceneConstants = {
    segmentCount: 150
};
