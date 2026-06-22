import { useRef } from 'react';
import { useTranslation } from 'react-i18next';
import type { PanelPoint } from '../model/panelOutline';
import {
  parsePanelPolygonPoints,
  presetPolygonPoints,
  serializePanelPolygonPoints,
} from '../model/panelPolygon';

interface PanelPolygonEditorProps {
  widthMm: number;
  heightMm: number;
  pointsJson?: string | null;
  onPreview: (json: string) => void;
  onCommit: (json: string) => void;
}

const GRID_MM = 25;
const clamp = (v: number, lo: number, hi: number) => Math.min(hi, Math.max(lo, v));
const snap = (v: number) => Math.round(v / GRID_MM) * GRID_MM;

export function PanelPolygonEditor({
  widthMm,
  heightMm,
  pointsJson,
  onPreview,
  onCommit,
}: PanelPolygonEditorProps) {
  const { t } = useTranslation();
  const svgRef = useRef<SVGSVGElement | null>(null);
  const dragIndex = useRef<number | null>(null);

  const w = Math.max(1, widthMm);
  const h = Math.max(1, heightMm);
  const points = parsePanelPolygonPoints(pointsJson) ?? presetPolygonPoints(4, w, h);

  // SVG is y-down; the panel frame is bottom-centred, y-up.
  const toSvgX = (x: number) => x + w / 2;
  const toSvgY = (y: number) => h - y;

  const toPanel = (clientX: number, clientY: number): PanelPoint => {
    const rect = svgRef.current?.getBoundingClientRect();
    if (!rect || rect.width === 0 || rect.height === 0) return { x: 0, y: 0 };
    const fx = (clientX - rect.left) / rect.width;
    const fy = (clientY - rect.top) / rect.height;
    return {
      x: clamp(snap(fx * w - w / 2), -w / 2, w / 2),
      y: clamp(snap(h - fy * h), 0, h),
    };
  };

  const addVertex = (e: React.PointerEvent<SVGSVGElement>) => {
    if (dragIndex.current !== null) return;
    onCommit(serializePanelPolygonPoints([...points, toPanel(e.clientX, e.clientY)]));
  };

  const startDrag = (index: number) => (e: React.PointerEvent<SVGCircleElement>) => {
    e.stopPropagation();
    dragIndex.current = index;
    e.currentTarget.setPointerCapture(e.pointerId);
  };

  const moveDrag = (e: React.PointerEvent<SVGSVGElement>) => {
    if (dragIndex.current === null) return;
    const moved = toPanel(e.clientX, e.clientY);
    onPreview(
      serializePanelPolygonPoints(points.map((p, i) => (i === dragIndex.current ? moved : p))),
    );
  };

  const endDrag = (e: React.PointerEvent<SVGSVGElement>) => {
    if (dragIndex.current === null) return;
    dragIndex.current = null;
    onCommit(serializePanelPolygonPoints(points));
    e.stopPropagation();
  };

  const removeVertex = (index: number) => (e: React.MouseEvent) => {
    e.stopPropagation();
    if (points.length <= 3) return;
    onCommit(serializePanelPolygonPoints(points.filter((_, i) => i !== index)));
  };

  const vertexR = Math.max(20, Math.min(w, h) / 30);
  const polyPoints = points.map((p) => `${toSvgX(p.x)},${toSvgY(p.y)}`).join(' ');

  return (
    <div className="flex flex-col gap-1.5">
      <svg
        ref={svgRef}
        viewBox={`0 0 ${w} ${h}`}
        preserveAspectRatio="xMidYMid meet"
        style={{ aspectRatio: `${w} / ${h}` }}
        className="block w-full cursor-crosshair touch-none rounded border border-slate-300 bg-slate-50 dark:border-slate-600 dark:bg-slate-900"
        onPointerDown={addVertex}
        onPointerMove={moveDrag}
        onPointerUp={endDrag}
      >
        <polygon
          points={polyPoints}
          fill="rgba(99,102,241,0.18)"
          stroke="#6366f1"
          strokeWidth={vertexR / 3}
        />
        {points.map((p, i) => (
          <circle
            key={i}
            cx={toSvgX(p.x)}
            cy={toSvgY(p.y)}
            r={vertexR}
            fill="#6366f1"
            stroke="#ffffff"
            strokeWidth={vertexR / 4}
            className="cursor-grab"
            onPointerDown={startDrag(i)}
            onDoubleClick={removeVertex(i)}
          />
        ))}
      </svg>
      <p className="text-[10px] leading-snug text-slate-500 dark:text-slate-400">
        {t('GlassEnclosure.Designer.Panel.PolygonHint', {
          defaultValue: 'Tıkla: nokta ekle · Sürükle: taşı · Çift tık: sil',
        })}
      </p>
    </div>
  );
}
