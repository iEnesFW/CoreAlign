import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { safeRequestWithNotify } from '@/shared/lib/safeRequest';
import { useConfigureEnclosureMutation } from '../hooks/useGlassProjectQueries';
import { parsePolygonVertices, polygonIsClosedValid } from '../model/polygonGeometry';
import type { GlassProjectDto, PolygonVertex } from '../model/project.types';

interface PolygonInspectorProps {
  project: GlassProjectDto;
}

interface VertexRow {
  x: string;
  y: string;
}

const MIN_VERTEX_ROWS = 3;
const DEFAULT_HEIGHT_MM = 2600;
const DEFAULT_VERTICES: PolygonVertex[] = [
  { xMm: 0, yMm: 0 },
  { xMm: 4000, yMm: 0 },
  { xMm: 2000, yMm: 3000 },
];

const toRows = (vertices: PolygonVertex[]): VertexRow[] =>
  vertices.map((vertex) => ({ x: String(vertex.xMm), y: String(vertex.yMm) }));

const initialRows = (json: string | null): VertexRow[] => {
  const parsed = parsePolygonVertices(json);
  return toRows(parsed.length >= MIN_VERTEX_ROWS ? parsed : DEFAULT_VERTICES);
};

const rowsToVertices = (rows: VertexRow[]): PolygonVertex[] | null => {
  const vertices = rows.map((row) => ({ xMm: Number(row.x), yMm: Number(row.y) }));
  const allFinite = vertices.every(
    (vertex) => Number.isFinite(vertex.xMm) && Number.isFinite(vertex.yMm),
  );
  return allFinite ? vertices : null;
};

export function PolygonInspector({ project }: PolygonInspectorProps) {
  const { t } = useTranslation();
  const configureMutation = useConfigureEnclosureMutation();
  const [rows, setRows] = useState<VertexRow[]>(() => initialRows(project.polygonVerticesJson));
  const [trackedProjectId, setTrackedProjectId] = useState(project.id);
  if (trackedProjectId !== project.id) {
    setTrackedProjectId(project.id);
    setRows(initialRows(project.polygonVerticesJson));
  }

  const heightMm = project.eaveHeightMm ?? DEFAULT_HEIGHT_MM;
  const vertices = rowsToVertices(rows);
  const isValid = vertices !== null && polygonIsClosedValid(vertices);

  const updateRow = (index: number, axis: keyof VertexRow, value: string) =>
    setRows((prev) => prev.map((row, i) => (i === index ? { ...row, [axis]: value } : row)));

  const addRow = () => setRows((prev) => [...prev, { x: '0', y: '0' }]);

  const removeRow = (index: number) =>
    setRows((prev) => (prev.length > MIN_VERTEX_ROWS ? prev.filter((_, i) => i !== index) : prev));

  const save = async () => {
    if (!vertices) return;
    await safeRequestWithNotify(
      configureMutation.mutateAsync({
        id: project.id,
        input: {
          category: project.enclosureCategory ?? 'Special',
          subtype: project.enclosureSubtype ?? 'FreeForm',
          geometryMode: 'FreeForm',
          mountingTopology: project.mountingTopology,
          roofPitchDeg: project.roofPitchDeg,
          ridgeHeightMm: project.ridgeHeightMm,
          eaveHeightMm: project.eaveHeightMm,
          polygonVerticesJson: JSON.stringify(vertices),
        },
      }),
      {
        successMessage: t('GlassEnclosure.Designer.Polygon.Saved', {
          defaultValue: 'Polygon saved',
        }),
        showSuccessNotification: true,
      },
    );
  };

  return (
    <section
      className="space-y-3 rounded-md border border-sky-200 bg-sky-50/60 p-3 dark:border-sky-900/50 dark:bg-sky-950/20"
      aria-label={t('GlassEnclosure.Designer.Polygon.SectionTitle', {
        defaultValue: 'Polygon vertices',
      })}
    >
      <h4 className="text-xs font-semibold uppercase tracking-wide text-sky-700 dark:text-sky-300">
        {t('GlassEnclosure.Designer.Polygon.SectionTitle', { defaultValue: 'Polygon vertices' })}
      </h4>

      <table className="w-full text-xs">
        <thead>
          <tr className="text-left text-[10px] uppercase tracking-wide text-slate-500 dark:text-slate-400">
            <th className="pb-1 pr-2">
              {t('GlassEnclosure.Designer.Polygon.VertexX', { defaultValue: 'X (mm)' })}
            </th>
            <th className="pb-1 pr-2">
              {t('GlassEnclosure.Designer.Polygon.VertexY', { defaultValue: 'Y (mm)' })}
            </th>
            <th className="pb-1" />
          </tr>
        </thead>
        <tbody>
          {rows.map((row, index) => (
            <tr key={index}>
              <td className="pr-2 pb-1">
                <VertexInput value={row.x} onChange={(value) => updateRow(index, 'x', value)} />
              </td>
              <td className="pr-2 pb-1">
                <VertexInput value={row.y} onChange={(value) => updateRow(index, 'y', value)} />
              </td>
              <td className="pb-1 text-right">
                <button
                  type="button"
                  onClick={() => removeRow(index)}
                  disabled={rows.length <= MIN_VERTEX_ROWS}
                  className="rounded px-1.5 py-0.5 text-[10px] text-danger-600 hover:bg-danger-50 disabled:cursor-not-allowed disabled:opacity-40 dark:text-danger-400 dark:hover:bg-danger-950/30"
                >
                  {t('GlassEnclosure.Designer.Polygon.RemoveVertex', { defaultValue: 'Remove' })}
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>

      <div className="flex items-center justify-between gap-2">
        <button
          type="button"
          onClick={addRow}
          className="rounded border border-slate-300 px-2 py-1 text-[11px] text-slate-700 hover:bg-slate-100 dark:border-slate-600 dark:text-slate-200 dark:hover:bg-slate-800"
        >
          {t('GlassEnclosure.Designer.Polygon.AddVertex', { defaultValue: 'Add vertex' })}
        </button>
        <button
          type="button"
          onClick={save}
          disabled={!isValid || configureMutation.isPending}
          className="rounded bg-primary-600 px-3 py-1 text-[11px] font-medium text-white hover:bg-primary-700 disabled:cursor-not-allowed disabled:opacity-50"
        >
          {t('GlassEnclosure.Designer.Polygon.Save', { defaultValue: 'Save polygon' })}
        </button>
      </div>

      {!isValid ? (
        <p className="text-[10px] text-danger-600 dark:text-danger-400">
          {t('GlassEnclosure.Designer.Polygon.Invalid', {
            defaultValue: 'At least 3 vertices with non-zero edges are required',
          })}
        </p>
      ) : null}

      <p className="text-[10px] text-slate-500 dark:text-slate-400">
        {t('GlassEnclosure.Designer.Polygon.HeightNote', {
          defaultValue: 'Facade height: {{height}} mm',
          height: heightMm,
        })}
      </p>
    </section>
  );
}

interface VertexInputProps {
  value: string;
  onChange: (value: string) => void;
}

const VertexInput = ({ value, onChange }: VertexInputProps) => (
  <input
    type="number"
    value={value}
    step={50}
    onChange={(e) => onChange(e.target.value)}
    className="w-full rounded border border-slate-300 bg-white px-2 py-1 text-xs focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500 dark:border-slate-600 dark:bg-slate-800 dark:text-slate-100"
  />
);
