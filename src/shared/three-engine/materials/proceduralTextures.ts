import { CanvasTexture, RepeatWrapping, SRGBColorSpace } from 'three';
import type { Texture } from 'three';

export type ProceduralMaterialKey = 'wood' | 'marble' | 'concrete' | 'panel';

export const PROCEDURAL_MATERIAL_KEYS: ProceduralMaterialKey[] = [
  'wood',
  'marble',
  'concrete',
  'panel',
];

const SIZE = 256;
const cache = new Map<ProceduralMaterialKey, Texture>();

const mulberry32 = (seed: number) => {
  let a = seed;
  return () => {
    a |= 0;
    a = (a + 0x6d2b79f5) | 0;
    let t = Math.imul(a ^ (a >>> 15), 1 | a);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
};

const createCanvas = () => {
  const canvas = document.createElement('canvas');
  canvas.width = SIZE;
  canvas.height = SIZE;
  return canvas;
};

const drawWood = (ctx: CanvasRenderingContext2D) => {
  const rand = mulberry32(7);
  ctx.fillStyle = '#b08a5a';
  ctx.fillRect(0, 0, SIZE, SIZE);
  for (let x = 0; x < SIZE; x += 1) {
    const wave = Math.sin(x * 0.12) * 6 + Math.sin(x * 0.031) * 14;
    const shade = 0.82 + 0.18 * Math.sin(x * 0.6 + wave * 0.2);
    ctx.fillStyle = `rgba(92, 62, 28, ${0.22 * (1 - shade)})`;
    ctx.fillRect(x, 0, 1, SIZE);
  }
  for (let i = 0; i < 9; i += 1) {
    const x = rand() * SIZE;
    ctx.strokeStyle = `rgba(70, 45, 20, ${0.25 + rand() * 0.2})`;
    ctx.lineWidth = 1 + rand() * 1.5;
    ctx.beginPath();
    ctx.moveTo(x, 0);
    ctx.bezierCurveTo(
      x + rand() * 14 - 7,
      SIZE * 0.33,
      x + rand() * 14 - 7,
      SIZE * 0.66,
      x + rand() * 10 - 5,
      SIZE,
    );
    ctx.stroke();
  }
};

const drawMarble = (ctx: CanvasRenderingContext2D) => {
  const rand = mulberry32(11);
  const gradient = ctx.createLinearGradient(0, 0, SIZE, SIZE);
  gradient.addColorStop(0, '#f3f4f6');
  gradient.addColorStop(1, '#e2e5e9');
  ctx.fillStyle = gradient;
  ctx.fillRect(0, 0, SIZE, SIZE);
  for (let i = 0; i < 7; i += 1) {
    ctx.strokeStyle = `rgba(120, 128, 140, ${0.12 + rand() * 0.16})`;
    ctx.lineWidth = 0.8 + rand() * 1.8;
    ctx.beginPath();
    let x = rand() * SIZE;
    let y = 0;
    ctx.moveTo(x, y);
    while (y < SIZE) {
      x += rand() * 36 - 18;
      y += 14 + rand() * 22;
      ctx.lineTo(x, y);
    }
    ctx.stroke();
  }
};

const drawConcrete = (ctx: CanvasRenderingContext2D) => {
  const rand = mulberry32(23);
  ctx.fillStyle = '#a9adb3';
  ctx.fillRect(0, 0, SIZE, SIZE);
  for (let i = 0; i < 2600; i += 1) {
    const v = 150 + Math.floor(rand() * 60);
    ctx.fillStyle = `rgba(${v}, ${v}, ${v + 4}, ${0.18 + rand() * 0.2})`;
    ctx.fillRect(rand() * SIZE, rand() * SIZE, 1 + rand() * 2, 1 + rand() * 2);
  }
};

const drawPanel = (ctx: CanvasRenderingContext2D) => {
  ctx.fillStyle = '#e8eaed';
  ctx.fillRect(0, 0, SIZE, SIZE);
  ctx.strokeStyle = 'rgba(110, 118, 128, 0.55)';
  ctx.lineWidth = 2;
  const step = SIZE / 2;
  for (let i = 0; i <= 2; i += 1) {
    ctx.beginPath();
    ctx.moveTo(i * step, 0);
    ctx.lineTo(i * step, SIZE);
    ctx.stroke();
    ctx.beginPath();
    ctx.moveTo(0, i * step);
    ctx.lineTo(SIZE, i * step);
    ctx.stroke();
  }
  ctx.strokeStyle = 'rgba(255, 255, 255, 0.7)';
  ctx.lineWidth = 1;
  for (let i = 0; i <= 2; i += 1) {
    ctx.beginPath();
    ctx.moveTo(i * step + 2, 0);
    ctx.lineTo(i * step + 2, SIZE);
    ctx.stroke();
  }
};

const PAINTERS: Record<ProceduralMaterialKey, (ctx: CanvasRenderingContext2D) => void> = {
  wood: drawWood,
  marble: drawMarble,
  concrete: drawConcrete,
  panel: drawPanel,
};

export const isProceduralMaterialKey = (value: string): value is ProceduralMaterialKey =>
  (PROCEDURAL_MATERIAL_KEYS as string[]).includes(value);

export const getProceduralTexture = (key: ProceduralMaterialKey): Texture => {
  const cached = cache.get(key);
  if (cached) return cached;
  const canvas = createCanvas();
  const ctx = canvas.getContext('2d');
  if (ctx) PAINTERS[key](ctx);
  const texture = new CanvasTexture(canvas);
  texture.wrapS = RepeatWrapping;
  texture.wrapT = RepeatWrapping;
  texture.colorSpace = SRGBColorSpace;
  texture.anisotropy = 4;
  cache.set(key, texture);
  return texture;
};
