import { useTranslation } from 'react-i18next';
import {
  BrickWall,
  Circle,
  Egg,
  Eraser,
  Hexagon,
  Home,
  LassoSelect,
  Layers,
  Minus,
  MousePointer2,
  Move,
  MoveHorizontal,
  Paintbrush,
  PenTool,
  RectangleHorizontal,
  RotateCw,
  Ruler,
  Shapes,
  Spline,
  Square,
  SquareSplitHorizontal,
  Triangle,
  Wand2,
} from 'lucide-react';
import { PROCEDURAL_MATERIAL_KEYS } from '@/shared/three-engine';
import { cn } from '@/shared/lib/cn';
import {
  useDesignerStore,
  type DesignerTool,
  type PlacementKind,
  type WallDrawShape,
} from '@/features/glass-enclosure/model/designerStore';
import { useWallAutofill } from '@/features/glass-enclosure/hooks/useWallAutofill';
import { useColorOptionsQuery } from '@/features/glass-enclosure/hooks/useGlassEnclosureQueries';

interface ToolDef {
  tool: DesignerTool;
  labelKey: string;
  defaultLabel: string;
  shortcut: string;
  Icon: typeof Move;
}

const TOOLS: ToolDef[] = [
  { tool: 'select', labelKey: 'Select', defaultLabel: 'Seç', shortcut: 'V', Icon: MousePointer2 },
  {
    tool: 'multiselect',
    labelKey: 'MultiSelect',
    defaultLabel: 'Çoklu seç (lasso)',
    shortcut: 'L',
    Icon: LassoSelect,
  },
  { tool: 'move', labelKey: 'Move', defaultLabel: 'Taşı', shortcut: 'M', Icon: Move },
  { tool: 'rotate', labelKey: 'Rotate', defaultLabel: 'Döndür', shortcut: 'R', Icon: RotateCw },
  {
    tool: 'stretch',
    labelKey: 'Stretch',
    defaultLabel: 'Genişlet',
    shortcut: 'S',
    Icon: MoveHorizontal,
  },
  { tool: 'draw', labelKey: 'Draw', defaultLabel: 'Yüzeye çiz', shortcut: 'D', Icon: Shapes },
  { tool: 'paint', labelKey: 'Paint', defaultLabel: 'Boya', shortcut: 'B', Icon: Paintbrush },
  { tool: 'erase', labelKey: 'Erase', defaultLabel: 'Sil', shortcut: 'E', Icon: Eraser },
  { tool: 'measure', labelKey: 'Measure', defaultLabel: 'Ölç', shortcut: 'K', Icon: Ruler },
];

const PLACEMENTS: {
  kind: PlacementKind;
  labelKey: string;
  defaultLabel: string;
  shortcut: string;
  Icon: typeof Move;
}[] = [
  {
    kind: 'run',
    labelKey: 'AddRun',
    defaultLabel: 'Hat ekle',
    shortcut: '1',
    Icon: RectangleHorizontal,
  },
  { kind: 'wall', labelKey: 'AddWall', defaultLabel: 'Duvar ekle', shortcut: '2', Icon: BrickWall },
  { kind: 'floor', labelKey: 'AddFloor', defaultLabel: 'Zemin ekle', shortcut: '3', Icon: Square },
  { kind: 'roof', labelKey: 'AddRoof', defaultLabel: 'Çatı ekle', shortcut: '4', Icon: Home },
  { kind: 'pen', labelKey: 'Pen', defaultLabel: 'Kalemle yüzey çiz', shortcut: 'P', Icon: PenTool },
];

const MATERIAL_FALLBACKS: Record<string, string> = {
  wood: 'Ahşap',
  marble: 'Mermer',
  concrete: 'Beton',
  panel: 'Panel',
  grass: 'Çim',
  asphalt: 'Asfalt',
  brick: 'Tuğla',
  plaster: 'Sıva',
};

const DRAW_SHAPES: {
  shape: WallDrawShape;
  labelKey: string;
  defaultLabel: string;
  Icon: typeof Move;
}[] = [
  { shape: 'rect', labelKey: 'ShapeRect', defaultLabel: 'Dikdörtgen', Icon: Square },
  { shape: 'circle', labelKey: 'ShapeCircle', defaultLabel: 'Daire', Icon: Circle },
  { shape: 'ellipse', labelKey: 'ShapeEllipse', defaultLabel: 'Oval', Icon: Egg },
  { shape: 'triangle', labelKey: 'ShapeTriangle', defaultLabel: 'Üçgen', Icon: Triangle },
  { shape: 'polygon', labelKey: 'ShapePolygon', defaultLabel: 'Çokgen', Icon: Hexagon },
  { shape: 'free', labelKey: 'ShapeFree', defaultLabel: 'Serbest çizim', Icon: PenTool },
  {
    shape: 'split',
    labelKey: 'ShapeSplit',
    defaultLabel: 'Duvarı böl',
    Icon: SquareSplitHorizontal,
  },
];

export function ToolPalette() {
  const { t } = useTranslation();
  const activeTool = useDesignerStore((s) => s.activeTool);
  const placement = useDesignerStore((s) => s.placement);
  const paintColor = useDesignerStore((s) => s.paintColor);
  const paintMaterial = useDesignerStore((s) => s.paintMaterial);
  const drawShape = useDesignerStore((s) => s.drawShape);
  const stackOnDrop = useDesignerStore((s) => s.stackOnDrop);
  const toggleStackOnDrop = useDesignerStore((s) => s.toggleStackOnDrop);
  const setActiveTool = useDesignerStore((s) => s.setActiveTool);
  const setPlacement = useDesignerStore((s) => s.setPlacement);
  const setPaintColor = useDesignerStore((s) => s.setPaintColor);
  const setPaintMaterial = useDesignerStore((s) => s.setPaintMaterial);
  const setDrawShape = useDesignerStore((s) => s.setDrawShape);
  const placementShape = useDesignerStore((s) => s.placementShape);
  const setPlacementShape = useDesignerStore((s) => s.setPlacementShape);
  const { autofill } = useWallAutofill();
  const colorsQuery = useColorOptionsQuery();
  const colors = colorsQuery.data?.data ?? [];

  const label = (key: string, defaultValue: string) =>
    t(`GlassEnclosure.Designer.Tool.${key}`, { defaultValue });

  const withShortcut = (text: string, shortcut: string) => `${text} (${shortcut})`;

  return (
    <div className="pointer-events-none absolute left-3 top-3 z-20 flex flex-col items-start gap-1.5">
      <div className="pointer-events-auto flex items-center gap-0.5 rounded-lg border border-slate-200 bg-white/95 p-1 shadow-md backdrop-blur dark:border-slate-700 dark:bg-slate-900/95">
        {TOOLS.map(({ tool, labelKey, defaultLabel, shortcut, Icon }) => (
          <PaletteButton
            key={tool}
            title={withShortcut(label(labelKey, defaultLabel), shortcut)}
            active={activeTool === tool && !placement}
            onClick={() => setActiveTool(tool)}
          >
            <Icon size={15} />
          </PaletteButton>
        ))}
        <PaletteButton
          title={withShortcut(label('Autofill', 'Boşlukları camla doldur'), 'F')}
          active={false}
          onClick={() => void autofill()}
        >
          <Wand2 size={15} />
        </PaletteButton>
        <PaletteButton
          title={label(
            'StackOnDrop',
            'Üst üste bırak — sürüklediğin objeyi üzerine geldiğinin üstüne koyar (Alt ile de olur)',
          )}
          active={stackOnDrop}
          onClick={toggleStackOnDrop}
        >
          <Layers size={15} />
        </PaletteButton>
        <span className="mx-0.5 h-5 w-px bg-slate-300 dark:bg-slate-700" />
        {PLACEMENTS.map(({ kind, labelKey, defaultLabel, shortcut, Icon }) => (
          <PaletteButton
            key={kind}
            title={withShortcut(label(labelKey, defaultLabel), shortcut)}
            active={placement === kind}
            onClick={() => setPlacement(placement === kind ? null : kind)}
          >
            <Icon size={15} />
          </PaletteButton>
        ))}
      </div>
      {placement === 'pen' && (
        <span className="pointer-events-auto rounded bg-slate-900/80 px-2 py-0.5 text-[10px] font-medium text-white">
          {label(
            'PenHint',
            'Zemine ya da bir obje yüzeyine tıkla → köşe ekle · Shift düz · ilk noktaya tıkla / çift tık / Enter bitir · Esc iptal',
          )}
        </span>
      )}
      {placement && placement !== 'pen' && (
        <div className="pointer-events-auto flex items-center gap-0.5 rounded-lg border border-slate-200 bg-white/95 p-1 shadow-md backdrop-blur dark:border-slate-700 dark:bg-slate-900/95">
          <PaletteButton
            title={label('ShapeFlat', 'Düz')}
            active={placementShape === 'flat'}
            onClick={() => setPlacementShape('flat')}
          >
            <Minus size={15} />
          </PaletteButton>
          <PaletteButton
            title={label('ShapeCurved', 'Yay / kavisli')}
            active={placementShape === 'curved'}
            onClick={() => setPlacementShape('curved')}
          >
            <Spline size={15} />
          </PaletteButton>
        </div>
      )}
      {activeTool === 'draw' && !placement && (
        <div className="pointer-events-auto flex flex-col items-center gap-1">
          <div className="flex items-center gap-0.5 rounded-lg border border-slate-200 bg-white/95 p-1 shadow-md backdrop-blur dark:border-slate-700 dark:bg-slate-900/95">
            {DRAW_SHAPES.map(({ shape, labelKey, defaultLabel, Icon }) => (
              <PaletteButton
                key={shape}
                title={label(labelKey, defaultLabel)}
                active={drawShape === shape}
                onClick={() => setDrawShape(shape)}
              >
                <Icon size={15} />
              </PaletteButton>
            ))}
          </div>
          <span className="rounded bg-slate-900/80 px-2 py-0.5 text-[10px] font-medium text-white">
            {label(
              'DrawHint',
              'Yüzeyde bölge çiz → Genişlet (S) aracıyla girinti/çıkıntı derinliği ver',
            )}
          </span>
        </div>
      )}
      {activeTool === 'paint' && !placement && (
        <div className="pointer-events-auto flex flex-col items-start gap-1">
          <div className="flex max-w-[18rem] flex-wrap items-center gap-1 rounded-lg border border-slate-200 bg-white/95 p-1 shadow-md backdrop-blur dark:border-slate-700 dark:bg-slate-900/95">
            {colors.map((color) => (
              <button
                key={color.id}
                type="button"
                title={color.name}
                aria-label={color.name}
                onClick={() => setPaintColor({ id: color.id, hex: color.hexColor })}
                className={cn(
                  'h-6 w-6 rounded border',
                  paintColor?.hex === color.hexColor
                    ? 'border-primary-500 ring-2 ring-primary-400/60'
                    : 'border-slate-300 dark:border-slate-600',
                )}
                style={{ backgroundColor: color.hexColor }}
              />
            ))}
            {colors.length === 0 && (
              <span className="px-2 text-[11px] text-slate-400">
                {label('NoColors', 'Renk kataloğu boş')}
              </span>
            )}
          </div>
          <div className="flex max-w-[18rem] flex-wrap items-center gap-1 rounded-lg border border-slate-200 bg-white/95 p-1 shadow-md backdrop-blur dark:border-slate-700 dark:bg-slate-900/95">
            {PROCEDURAL_MATERIAL_KEYS.map((key) => (
              <button
                key={key}
                type="button"
                aria-pressed={paintMaterial === key}
                onClick={() => setPaintMaterial(key)}
                className={cn(
                  'rounded border px-2 py-1 text-[11px] font-medium transition',
                  paintMaterial === key
                    ? 'border-primary-600 bg-primary-600 text-white'
                    : 'border-slate-300 text-slate-600 hover:bg-slate-50 dark:border-slate-600 dark:text-slate-300 dark:hover:bg-slate-800',
                )}
              >
                {label(`Material_${key}`, MATERIAL_FALLBACKS[key])}
              </button>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

const PaletteButton = ({
  title,
  active,
  onClick,
  children,
}: {
  title: string;
  active: boolean;
  onClick: () => void;
  children: React.ReactNode;
}) => (
  <button
    type="button"
    title={title}
    aria-label={title}
    aria-pressed={active}
    onClick={onClick}
    className={cn(
      'inline-flex h-8 w-8 items-center justify-center rounded-md transition',
      active
        ? 'bg-primary-600 text-white'
        : 'text-slate-600 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-800',
    )}
  >
    {children}
  </button>
);
