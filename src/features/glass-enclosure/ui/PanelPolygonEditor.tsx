import { useRef } from 'react';
import { useTranslation } from 'react-i18next';
import type { PanelPoint } from '../model/panelOutline';
import {
  parsePanelPolygonPoints,
  presetPolygonPoints,
  serializePanelPolygonPoints,
} from '../model/panelPolygon';
import { normalizePanelOutline } from '../model/panelShapeOutline';
import { notifyPanelOutlineRejected } from '../model/panelOutlineFeedback';

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
  // The last outline the gate ACCEPTED. Previews write straight into the scene (history-free) for
  // live feedback, so when a commit is refused the scene is left holding the previewed shape — this
  // is what we restore it to, otherwise a refused bowtie LOOKS committed.
  const lastValidJson = useRef<string | null>(null);

  const w = Math.max(1, widthMm);
  const h = Math.max(1, heightMm);
  const points = parsePanelPolygonPoints(pointsJson) ?? presetPolygonPoints(4, w, h);

  // Capture the pre-interaction shape as the restore target — inside the handlers, never during
  // render (writing refs mid-render breaks with the React compiler).
  const rememberBaseline = () => {
    if (lastValidJson.current !== null) return;
    const result = normalizePanelOutline(points, w, h);
    // WHY the raw fallback: a legacy pane saved before the gate existed can hold an invalid
    // outline — with no baseline at all, a refused drag would leave the preview stuck at the
    // dragged position; restoring the pre-gesture outline verbatim at least rewinds the gesture.
    lastValidJson.current = result.points
      ? serializePanelPolygonPoints(result.points)
      : serializePanelPolygonPoints(points);
  };

  // Every commit passes the shaped-pane gate HERE, where the interaction can self-heal: a refused
  // outline says why, and the scene (already showing the preview) snaps back to the last good
  // shape. The store's clampPanelPatch stays as the universal backstop for other producers.
  const commitOutline = (next: PanelPoint[]) => {
    const result = normalizePanelOutline(next, w, h);
    if (!result.points) {
      notifyPanelOutlineRejected(result.rejection);
      if (lastValidJson.current) onPreview(lastValidJson.current);
      return;
    }
    const json = serializePanelPolygonPoints(result.points);
    lastValidJson.current = json;
    onCommit(json);
  };

  // SVG is y-down; the panel frame is bottom-centred, y-up.
  const toSvgX = (x: number) => x + w / 2;
  const toSvgY = (y: number) => h - y;

  const toPanel = (clientX: number, clientY: number): PanelPoint => {
    const svg = svgRef.current;
    const ctm = svg?.getScreenCTM();
    if (!svg || !ctm) return { x: 0, y: 0 };
    // Map the click through the SVG's own screen matrix so it lands exactly under the
    // cursor regardless of preserveAspectRatio letterboxing (a manual rect ratio drifts).
    const pt = svg.createSVGPoint();
    pt.x = clientX;
    pt.y = clientY;
    const local = pt.matrixTransform(ctm.inverse());
    return {
      x: clamp(snap(local.x - w / 2), -w / 2, w / 2),
      y: clamp(snap(h - local.y), 0, h),
    };
  };

  const addVertex = (e: React.PointerEvent<SVGSVGElement>) => {
    if (dragIndex.current !== null) return;
    rememberBaseline();
    commitOutline([...points, toPanel(e.clientX, e.clientY)]);
  };

  const startDrag = (index: number) => (e: React.PointerEvent<SVGCircleElement>) => {
    e.stopPropagation();
    rememberBaseline();
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
    commitOutline(points);
    e.stopPropagation();
  };

  const removeVertex = (index: number) => (e: React.MouseEvent) => {
    e.stopPropagation();
    if (points.length <= 3) return;
    rememberBaseline();
    commitOutline(points.filter((_, i) => i !== index));
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
