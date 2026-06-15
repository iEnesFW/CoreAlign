import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import {
  AlignCenterHorizontal,
  AlignCenterVertical,
  AlignHorizontalSpaceAround,
  AlignVerticalSpaceAround,
  Copy,
  Link2,
  Lock,
  LockOpen,
  Plus,
  Ruler,
  Trash2,
  UnfoldVertical,
  Wand2,
} from 'lucide-react';
import { cn } from '@/shared/lib/cn';
import { useDesignerStore } from '@/features/glass-enclosure/model/designerStore';
import { useDesignerEntityActions } from '@/features/glass-enclosure/hooks/useDesignerEntityActions';
import { useMultiSelectionDelete } from '@/features/glass-enclosure/hooks/useMultiSelectionDelete';
import { useMultiAlignActions } from '@/features/glass-enclosure/hooks/useMultiAlignActions';
import { useWallAutofill } from '@/features/glass-enclosure/hooks/useWallAutofill';
import type { GlassTypeDto } from '@/features/glass-enclosure/model/glassEnclosure.types';

interface SelectionToolbarProps {
  glassTypes: GlassTypeDto[];
}

export function SelectionToolbar({ glassTypes }: SelectionToolbarProps) {
  const { t } = useTranslation();
  const selection = useDesignerStore((s) => s.selection);
  const runs = useDesignerStore((s) => s.scene.runs);
  const addHardware = useDesignerStore((s) => s.addHardware);
  const removeHardware = useDesignerStore((s) => s.removeHardware);
  const removeConnection = useDesignerStore((s) => s.removeConnection);
  const removeWall = useDesignerStore((s) => s.removeWall);
  const removeWallFeature = useDesignerStore((s) => s.removeWallFeature);
  const removeSlabFeature = useDesignerStore((s) => s.removeSlabFeature);
  const removeSurface = useDesignerStore((s) => s.removeSurface);
  const slabs = useDesignerStore((s) => s.scene.slabs ?? []);
  const surfaces = useDesignerStore((s) => s.scene.surfaces ?? []);
  const walls = useDesignerStore((s) => s.scene.walls ?? []);
  const removeSlab = useDesignerStore((s) => s.removeSlab);
  const updateWall = useDesignerStore((s) => s.updateWall);
  const updateSlab = useDesignerStore((s) => s.updateSlab);
  const updateSurface = useDesignerStore((s) => s.updateSurface);
  const setSelection = useDesignerStore((s) => s.setSelection);
  const { createPanel, deletePanel, deleteRun } = useDesignerEntityActions();
  const { autofill } = useWallAutofill();
  const multiSelection = useDesignerStore((s) => s.multiSelection);
  const { deleteMultiSelection } = useMultiSelectionDelete();
  const { alignCenters, distributeEvenly, joinEndToEnd, equalizeHeights, equalizeLengths } =
    useMultiAlignActions();

  const multiCount =
    multiSelection.runIds.length + multiSelection.wallIds.length + multiSelection.slabIds.length;

  if (multiCount > 0) {
    return (
      <div className="pointer-events-none absolute right-3 top-3 z-10 flex justify-end">
        <div className="pointer-events-auto flex items-center gap-1 rounded-lg border border-slate-200 bg-white/90 p-1 shadow-md backdrop-blur dark:border-slate-700 dark:bg-slate-900/90">
          <span className="px-2 text-xs font-medium text-slate-600 dark:text-slate-300">
            {t('GlassEnclosure.Designer.MultiSelect.Count', {
              defaultValue: '{{count}} öğe seçili',
              count: multiCount,
            })}
          </span>
          <ToolbarButton
            icon={<AlignCenterVertical size={13} />}
            label={t('GlassEnclosure.Designer.MultiSelect.AlignX', { defaultValue: 'X hizala' })}
            onClick={() => alignCenters('x')}
          />
          <ToolbarButton
            icon={<AlignCenterHorizontal size={13} />}
            label={t('GlassEnclosure.Designer.MultiSelect.AlignY', { defaultValue: 'Y hizala' })}
            onClick={() => alignCenters('y')}
          />
          <ToolbarButton
            icon={<AlignHorizontalSpaceAround size={13} />}
            label={t('GlassEnclosure.Designer.MultiSelect.DistributeX', {
              defaultValue: 'X eşit dağıt',
            })}
            onClick={() => distributeEvenly('x')}
          />
          <ToolbarButton
            icon={<AlignVerticalSpaceAround size={13} />}
            label={t('GlassEnclosure.Designer.MultiSelect.DistributeY', {
              defaultValue: 'Y eşit dağıt',
            })}
            onClick={() => distributeEvenly('y')}
          />
          <ToolbarButton
            icon={<Link2 size={13} />}
            label={t('GlassEnclosure.Designer.MultiSelect.Join', {
              defaultValue: 'Uç uca birleştir',
            })}
            onClick={() => joinEndToEnd()}
          />
          <ToolbarButton
            icon={<UnfoldVertical size={13} />}
            label={t('GlassEnclosure.Designer.MultiSelect.EqualHeights', {
              defaultValue: 'Yükseklikleri eşitle',
            })}
            onClick={() => equalizeHeights()}
          />
          <ToolbarButton
            icon={<Ruler size={13} />}
            label={t('GlassEnclosure.Designer.MultiSelect.EqualLengths', {
              defaultValue: 'Uzunlukları eşitle',
            })}
            onClick={() => equalizeLengths()}
          />
          <ToolbarButton
            icon={<Wand2 size={13} />}
            label={t('GlassEnclosure.Designer.MultiSelect.FillGaps', {
              defaultValue: 'Araları camla doldur',
            })}
            onClick={() => void autofill()}
          />
          <ToolbarButton
            icon={<Trash2 size={13} />}
            label={t('GlassEnclosure.Designer.MultiSelect.DeleteAll', {
              defaultValue: 'Seçilenleri sil',
            })}
            danger
            onClick={() => deleteMultiSelection()}
          />
        </div>
      </div>
    );
  }

  if (!selection.kind) return null;

  const run = runs.find((r) => r.id === selection.runId);
  const clear = () =>
    setSelection({ kind: null, runId: null, panelId: null, connectionId: null, hardwareId: null });

  const selectRun = (runId: string) =>
    setSelection({ kind: 'run', runId, panelId: null, connectionId: null, hardwareId: null });

  const handleAddPanel = async () => {
    if (!run) return;
    const template = run.panels[run.panels.length - 1];
    const created = await createPanel(run.id, template, glassTypes[0]?.id ?? '');
    if (created) selectRun(run.id);
  };

  const lockToggle = (locked: boolean, onToggle: () => void): ReactNode => (
    <ToolbarButton
      icon={locked ? <Lock size={13} /> : <LockOpen size={13} />}
      label={
        locked
          ? t('GlassEnclosure.Designer.Unlock', { defaultValue: 'Kilidi aç' })
          : t('GlassEnclosure.Designer.Lock', { defaultValue: 'Kilitle' })
      }
      onClick={onToggle}
    />
  );

  const renderActions = (): ReactNode => {
    if (selection.kind === 'run' && run) {
      return (
        <>
          <ToolbarButton
            icon={<Plus size={13} />}
            label={t('GlassEnclosure.Designer.AddPanel', { defaultValue: 'Add panel' })}
            onClick={() => void handleAddPanel()}
          />
          <ToolbarButton
            icon={<Trash2 size={13} />}
            label={t('GlassEnclosure.Designer.DeleteRun', { defaultValue: 'Delete run' })}
            danger
            onClick={() => {
              void deleteRun(run.id);
              clear();
            }}
          />
        </>
      );
    }

    if (selection.kind === 'panel' && run) {
      const panel = run.panels.find((p) => p.id === selection.panelId);
      return (
        <>
          <ToolbarButton
            icon={<Plus size={13} />}
            label={t('GlassEnclosure.Designer.AddPanel', { defaultValue: 'Add panel' })}
            onClick={() => void handleAddPanel()}
          />
          <ToolbarButton
            icon={<Trash2 size={13} />}
            label={t('GlassEnclosure.Designer.DeletePanel', { defaultValue: 'Delete panel' })}
            danger
            onClick={() => {
              if (!panel) return;
              void deletePanel(run.id, panel.id);
              selectRun(run.id);
            }}
          />
        </>
      );
    }

    if (selection.kind === 'hardware' && run) {
      const panel = run.panels.find((p) => p.id === selection.panelId);
      const item = panel?.hardware.find((h) => h.id === selection.hardwareId);
      return (
        <>
          <ToolbarButton
            icon={<Copy size={13} />}
            label={t('Common.Duplicate', { defaultValue: 'Duplicate' })}
            onClick={() => {
              if (!panel || !item) return;
              const clone = { ...item, id: crypto.randomUUID(), offsetXmm: item.offsetXmm + 30 };
              addHardware(run.id, panel.id, clone);
              setSelection({
                kind: 'hardware',
                runId: run.id,
                panelId: panel.id,
                connectionId: null,
                hardwareId: clone.id,
              });
            }}
          />
          <ToolbarButton
            icon={<Trash2 size={13} />}
            label={t('Common.Delete', { defaultValue: 'Delete' })}
            danger
            onClick={() => {
              if (!panel || !item) return;
              removeHardware(run.id, panel.id, item.id);
              setSelection({
                kind: 'panel',
                runId: run.id,
                panelId: panel.id,
                connectionId: null,
                hardwareId: null,
              });
            }}
          />
        </>
      );
    }

    if (selection.kind === 'wall' && selection.wallId) {
      const wallObj = walls.find((w) => w.id === selection.wallId);
      return (
        <>
          {lockToggle(Boolean(wallObj?.locked), () =>
            updateWall(selection.wallId as string, { locked: !wallObj?.locked }),
          )}
          <ToolbarButton
            icon={<Wand2 size={13} />}
            label={t('GlassEnclosure.Designer.Wall.Autofill', {
              defaultValue: 'Boşlukları camla doldur',
            })}
            onClick={() => void autofill()}
          />
          <ToolbarButton
            icon={<Trash2 size={13} />}
            label={t('GlassEnclosure.Designer.Wall.Delete', { defaultValue: 'Duvarı sil' })}
            danger
            onClick={() => {
              removeWall(selection.wallId as string);
              clear();
            }}
          />
        </>
      );
    }

    if (selection.kind === 'wallFeature' && selection.wallId && selection.featureId) {
      return (
        <ToolbarButton
          icon={<Trash2 size={13} />}
          label={t('GlassEnclosure.Designer.WallFeature.Remove', { defaultValue: 'Katmanı sil' })}
          danger
          onClick={() => {
            removeWallFeature(selection.wallId as string, selection.featureId as string);
            setSelection({
              kind: 'wall',
              runId: null,
              panelId: null,
              connectionId: null,
              hardwareId: null,
              wallId: selection.wallId,
            });
          }}
        />
      );
    }

    if (selection.kind === 'slabFeature' && selection.slabId && selection.featureId) {
      return (
        <ToolbarButton
          icon={<Trash2 size={13} />}
          label={t('GlassEnclosure.Designer.WallFeature.Remove', { defaultValue: 'Katmanı sil' })}
          danger
          onClick={() => {
            removeSlabFeature(selection.slabId as string, selection.featureId as string);
            setSelection({
              kind: 'slab',
              runId: null,
              panelId: null,
              connectionId: null,
              hardwareId: null,
              wallId: null,
              slabId: selection.slabId,
            });
          }}
        />
      );
    }

    if (selection.kind === 'slab' && selection.slabId) {
      const slab = slabs.find((item) => item.id === selection.slabId);
      return (
        <>
          {lockToggle(Boolean(slab?.locked), () =>
            updateSlab(selection.slabId as string, { locked: !slab?.locked }),
          )}
          <ToolbarButton
            icon={<Trash2 size={13} />}
            label={
              slab?.kind === 'roof'
                ? t('GlassEnclosure.Designer.Slab.DeleteRoof', { defaultValue: 'Çatıyı sil' })
                : t('GlassEnclosure.Designer.Slab.DeleteFloor', { defaultValue: 'Zemini sil' })
            }
            danger
            onClick={() => {
              removeSlab(selection.slabId as string);
              clear();
            }}
          />
        </>
      );
    }

    if (selection.kind === 'surface' && selection.surfaceId) {
      const surfaceObj = surfaces.find((s) => s.id === selection.surfaceId);
      return (
        <>
          {lockToggle(Boolean(surfaceObj?.locked), () =>
            updateSurface(selection.surfaceId as string, { locked: !surfaceObj?.locked }),
          )}
          <ToolbarButton
            icon={<Trash2 size={13} />}
            label={t('GlassEnclosure.Designer.Surface.Delete', { defaultValue: 'Yüzeyi sil' })}
            danger
            onClick={() => {
              removeSurface(selection.surfaceId as string);
              clear();
            }}
          />
        </>
      );
    }

    if (selection.kind === 'connection' && selection.connectionId) {
      return (
        <ToolbarButton
          icon={<Trash2 size={13} />}
          label={t('Common.Delete', { defaultValue: 'Delete' })}
          danger
          onClick={() => {
            removeConnection(selection.connectionId as string);
            clear();
          }}
        />
      );
    }

    return null;
  };

  const actions = renderActions();
  if (!actions) return null;

  return (
    <div className="pointer-events-none absolute right-3 top-3 z-10 flex justify-end">
      <div className="pointer-events-auto flex items-center gap-1 rounded-lg border border-slate-200 bg-white/90 p-1 shadow-md backdrop-blur dark:border-slate-700 dark:bg-slate-900/90">
        {actions}
      </div>
    </div>
  );
}

interface ToolbarButtonProps {
  icon: ReactNode;
  label: string;
  onClick: () => void;
  danger?: boolean;
}

const ToolbarButton = ({ icon, label, onClick, danger }: ToolbarButtonProps) => (
  <button
    type="button"
    onClick={onClick}
    title={label}
    aria-label={label}
    className={cn(
      'inline-flex h-7 items-center gap-1 rounded-md px-2 text-xs font-medium transition',
      danger
        ? 'text-red-600 hover:bg-red-50 dark:hover:bg-red-950/30'
        : 'text-slate-700 hover:bg-slate-100 dark:text-slate-200 dark:hover:bg-slate-800',
    )}
  >
    {icon}
    <span className="hidden md:inline">{label}</span>
  </button>
);
