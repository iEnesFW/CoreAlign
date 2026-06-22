import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Check,
  CheckCircle2,
  FileDown,
  Pencil,
  Plus,
  RotateCcw,
  RotateCw,
  Trash2,
  Upload,
  X,
} from 'lucide-react';
import { safeRequestWithNotify } from '@/shared/lib/safeRequest';
import type { AddManualBomLineInput, BOMLineDto, BOMSummaryDto } from '../model/engineering.types';
import type { GlassProjectDto } from '../model/project.types';
import {
  useAddManualBomLineMutation,
  useDeleteManualBomLineMutation,
  useOverrideBomLinePriceMutation,
  usePushBomLinePriceToCatalogMutation,
} from '../hooks/useGlassProjectQueries';

interface QuoteSummaryViewProps {
  project: GlassProjectDto;
  bom: BOMSummaryDto | null;
  isLoading: boolean;
  onRecompute: () => void;
  isRecomputing: boolean;
}

type GroupKey =
  | 'ProfileCut'
  | 'GlassPiece'
  | 'HardwarePiece'
  | 'Labor'
  | 'Transport'
  | 'Installation'
  | 'Other';

const GROUP_ORDER: GroupKey[] = [
  'ProfileCut',
  'GlassPiece',
  'HardwarePiece',
  'Labor',
  'Transport',
  'Installation',
  'Other',
];

const CATALOG_KINDS: ReadonlySet<BOMLineDto['kind']> = new Set([
  'ProfileCut',
  'GlassPiece',
  'HardwarePiece',
]);

const MANUAL_KIND_OPTIONS: BOMLineDto['kind'][] = [
  'HardwarePiece',
  'Labor',
  'Transport',
  'Installation',
];

const effectiveUnitPrice = (line: BOMLineDto) => line.unitPriceOverride ?? line.unitCost;

const canPushToCatalog = (line: BOMLineDto) =>
  line.unitPriceOverride !== null &&
  !line.isManual &&
  !line.isService &&
  line.refId !== null &&
  CATALOG_KINDS.has(line.kind);

export function QuoteSummaryView({
  project,
  bom,
  isLoading,
  onRecompute,
  isRecomputing,
}: QuoteSummaryViewProps) {
  const { t, i18n } = useTranslation();

  const overrideMutation = useOverrideBomLinePriceMutation();
  const addMutation = useAddManualBomLineMutation();
  const deleteMutation = useDeleteManualBomLineMutation();
  const pushMutation = usePushBomLinePriceToCatalogMutation();
  const isMutating =
    overrideMutation.isPending ||
    addMutation.isPending ||
    deleteMutation.isPending ||
    pushMutation.isPending;

  const [editingLineId, setEditingLineId] = useState<string | null>(null);
  const [editPrice, setEditPrice] = useState('');
  const [showAddForm, setShowAddForm] = useState(false);

  const formatter = useMemo(
    () =>
      new Intl.NumberFormat(i18n.language, {
        style: 'currency',
        currency: bom?.currency ?? project.currency ?? 'TRY',
        maximumFractionDigits: 2,
      }),
    [i18n.language, bom?.currency, project.currency],
  );
  const numberFormatter = useMemo(
    () => new Intl.NumberFormat(i18n.language, { maximumFractionDigits: 3 }),
    [i18n.language],
  );
  const dateFormatter = new Intl.DateTimeFormat(i18n.language, { dateStyle: 'long' });

  const groupedLines = useMemo(() => {
    const groups: Record<GroupKey, BOMLineDto[]> = {
      ProfileCut: [],
      GlassPiece: [],
      HardwarePiece: [],
      Labor: [],
      Transport: [],
      Installation: [],
      Other: [],
    };
    for (const line of bom?.lines ?? []) {
      const key: GroupKey = GROUP_ORDER.includes(line.kind as GroupKey)
        ? (line.kind as GroupKey)
        : 'Other';
      groups[key].push(line);
    }
    return groups;
  }, [bom]);

  const startEdit = (line: BOMLineDto) => {
    setEditingLineId(line.id);
    setEditPrice(String(effectiveUnitPrice(line)));
  };

  const cancelEdit = () => {
    setEditingLineId(null);
    setEditPrice('');
  };

  const saveOverride = async (line: BOMLineDto) => {
    const parsed = Number(editPrice);
    if (!Number.isFinite(parsed) || parsed < 0) return;
    const [data] = await safeRequestWithNotify(
      overrideMutation.mutateAsync({
        id: project.id,
        lineId: line.id,
        unitPriceOverride: parsed,
      }),
      {
        successMessage: t('GlassEnclosure.Quote.PriceUpdated', {
          defaultValue: 'Unit price updated.',
        }),
        showSuccessNotification: true,
      },
    );
    if (data) cancelEdit();
  };

  const clearOverride = async (line: BOMLineDto) => {
    await safeRequestWithNotify(
      overrideMutation.mutateAsync({ id: project.id, lineId: line.id, unitPriceOverride: null }),
      {
        successMessage: t('GlassEnclosure.Quote.OverrideCleared', {
          defaultValue: 'Manual price removed; list price restored.',
        }),
        showSuccessNotification: true,
      },
    );
  };

  const pushToCatalog = async (line: BOMLineDto) => {
    const confirmed = window.confirm(
      t('GlassEnclosure.Quote.PushToCatalogConfirm', {
        defaultValue:
          'The catalog price of this item will be updated with the manual price. Continue?',
      }),
    );
    if (!confirmed) return;
    await safeRequestWithNotify(pushMutation.mutateAsync({ id: project.id, lineId: line.id }), {
      successMessage: t('GlassEnclosure.Quote.CatalogPriceUpdated', {
        defaultValue: 'Catalog price updated.',
      }),
      showSuccessNotification: true,
    });
  };

  const deleteManualLine = async (line: BOMLineDto) => {
    const confirmed = window.confirm(
      t('GlassEnclosure.Quote.DeleteCustomLineConfirm', {
        defaultValue: 'This custom line will be removed from the quote. Continue?',
      }),
    );
    if (!confirmed) return;
    await safeRequestWithNotify(deleteMutation.mutateAsync({ id: project.id, lineId: line.id }), {
      successMessage: t('GlassEnclosure.Quote.CustomLineDeleted', {
        defaultValue: 'Custom line removed.',
      }),
      showSuccessNotification: true,
    });
  };

  const addManualLine = async (input: AddManualBomLineInput) => {
    const [data] = await safeRequestWithNotify(addMutation.mutateAsync({ id: project.id, input }), {
      successMessage: t('GlassEnclosure.Quote.CustomLineAdded', {
        defaultValue: 'Custom line added to the quote.',
      }),
      showSuccessNotification: true,
    });
    if (data) setShowAddForm(false);
  };

  if (isLoading) {
    return (
      <div className="flex h-full items-center justify-center text-sm text-slate-500">
        {t('Common.Loading')}
      </div>
    );
  }

  if (!bom) {
    return (
      <div className="flex flex-col items-center justify-center gap-3 p-8 text-center">
        <div className="text-sm text-slate-500 dark:text-slate-400">
          {t('GlassEnclosure.Quote.NoBom')}
        </div>
        <button
          type="button"
          onClick={onRecompute}
          disabled={isRecomputing}
          className="inline-flex items-center gap-2 rounded-md bg-primary-600 px-4 py-2 text-sm font-medium text-white hover:bg-primary-700 disabled:opacity-50"
        >
          <RotateCw size={14} className={isRecomputing ? 'animate-spin' : ''} />
          {t('GlassEnclosure.Quote.Recompute')}
        </button>
      </div>
    );
  }

  return (
    <section className="space-y-5 p-4">
      <header className="flex flex-wrap items-start justify-between gap-3 border-b border-slate-200 pb-3 dark:border-slate-700">
        <div>
          <h2 className="text-xl font-semibold text-slate-900 dark:text-slate-100">
            {t('GlassEnclosure.Quote.Title')}
          </h2>
          <p className="text-sm text-slate-500 dark:text-slate-400">
            {project.code} · {project.customerName ?? '—'}
          </p>
          {project.validUntilDate && (
            <p className="text-xs text-slate-500 dark:text-slate-400">
              {t('GlassEnclosure.Quote.ValidUntil')}:{' '}
              {dateFormatter.format(new Date(project.validUntilDate))}
            </p>
          )}
        </div>
        <div className="flex flex-wrap gap-2">
          <button
            type="button"
            onClick={onRecompute}
            disabled={isRecomputing}
            className="inline-flex shrink-0 items-center gap-1.5 whitespace-nowrap rounded-md border border-slate-300 px-3 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-50 disabled:opacity-50 dark:border-slate-700 dark:text-slate-200 dark:hover:bg-slate-800"
          >
            <RotateCw size={14} className={isRecomputing ? 'animate-spin' : ''} />
            {t('GlassEnclosure.Quote.Recompute')}
          </button>
          <button
            type="button"
            onClick={() => setShowAddForm((prev) => !prev)}
            className="inline-flex shrink-0 items-center gap-1.5 whitespace-nowrap rounded-md border border-success-300 bg-success-50 px-3 py-1.5 text-sm font-medium text-success-700 hover:bg-success-100 dark:border-success-700/50 dark:bg-success-950/40 dark:text-success-300 dark:hover:bg-success-900/40"
          >
            <Plus size={14} />
            {t('GlassEnclosure.Quote.AddCustomLine', { defaultValue: 'Add custom line' })}
          </button>
          <button
            type="button"
            onClick={() => exportQuoteCsv(bom, project)}
            className="inline-flex shrink-0 items-center gap-1.5 whitespace-nowrap rounded-md bg-primary-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-primary-700"
          >
            <FileDown size={14} />
            {t('GlassEnclosure.Quote.ExportCsv')}
          </button>
        </div>
      </header>

      {showAddForm && (
        <AddManualLineForm
          isPending={addMutation.isPending}
          onSubmit={addManualLine}
          onCancel={() => setShowAddForm(false)}
        />
      )}

      <div className="grid grid-cols-2 gap-2 xl:grid-cols-4">
        <Stat label={t('GlassEnclosure.Quote.Panels')} value={bom.totalPanels.toString()} />
        <Stat
          label={t('GlassEnclosure.Quote.Area')}
          value={`${numberFormatter.format(bom.totalAreaM2)} m²`}
        />
        <Stat
          label={t('GlassEnclosure.Quote.Weight')}
          value={`${numberFormatter.format(bom.totalWeightKg)} kg`}
        />
        <Stat label={t('GlassEnclosure.Quote.Lines')} value={bom.lines.length.toString()} />
      </div>

      <div className="space-y-4">
        {GROUP_ORDER.map((group) => {
          const lines = groupedLines[group];
          if (lines.length === 0) return null;
          const subtotal = lines.reduce((sum, l) => sum + l.lineCost, 0);
          return (
            <section
              key={group}
              className="overflow-hidden rounded-lg border border-slate-200 bg-white shadow-sm dark:border-slate-700 dark:bg-slate-800"
            >
              <header className="flex items-center justify-between bg-slate-50 px-4 py-2 dark:bg-slate-900/50">
                <h3 className="text-xs font-semibold uppercase tracking-wide text-slate-700 dark:text-slate-200">
                  {t(`GlassEnclosure.Quote.Group.${group}` as never)}
                </h3>
                <span className="font-mono text-sm font-semibold text-slate-900 dark:text-slate-100">
                  {formatter.format(subtotal)}
                </span>
              </header>
              <table className="w-full text-sm">
                <thead className="bg-slate-50/50 dark:bg-slate-900/30">
                  <tr>
                    <Th>{t('GlassEnclosure.Quote.Description')}</Th>
                    <Th align="right">{t('GlassEnclosure.Quote.Qty')}</Th>
                    <Th>{t('GlassEnclosure.Quote.Unit')}</Th>
                    <Th align="right">{t('GlassEnclosure.Quote.UnitCost')}</Th>
                    <Th align="right">{t('GlassEnclosure.Quote.LineCost')}</Th>
                    <Th align="right">
                      {t('GlassEnclosure.Quote.Actions', { defaultValue: 'Actions' })}
                    </Th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-200 dark:divide-slate-700">
                  {lines.map((line) => (
                    <BomLineRow
                      key={line.id}
                      line={line}
                      formatter={formatter}
                      numberFormatter={numberFormatter}
                      isEditing={editingLineId === line.id}
                      editPrice={editPrice}
                      onEditPriceChange={setEditPrice}
                      onStartEdit={() => startEdit(line)}
                      onCancelEdit={cancelEdit}
                      onSaveOverride={() => void saveOverride(line)}
                      onClearOverride={() => void clearOverride(line)}
                      onPushToCatalog={() => void pushToCatalog(line)}
                      onDelete={() => void deleteManualLine(line)}
                      disabled={isMutating}
                    />
                  ))}
                </tbody>
              </table>
            </section>
          );
        })}
      </div>

      <section className="rounded-lg border border-slate-200 bg-white p-4 shadow-sm dark:border-slate-700 dark:bg-slate-800">
        <h3 className="mb-3 text-xs font-semibold uppercase tracking-wide text-slate-700 dark:text-slate-200">
          {t('GlassEnclosure.Quote.Totals')}
        </h3>
        <dl className="space-y-1.5 text-sm">
          <TotalRow
            label={t('GlassEnclosure.Cost.Materials')}
            value={formatter.format(bom.profileCost)}
          />
          <TotalRow
            label={t('GlassEnclosure.Cost.Glass')}
            value={formatter.format(bom.glassCost)}
          />
          <TotalRow
            label={t('GlassEnclosure.Cost.Hardware')}
            value={formatter.format(bom.hardwareCost)}
          />
          {bom.wasteCost > 0 && (
            <TotalRow
              label={t('GlassEnclosure.Cost.Waste')}
              value={formatter.format(bom.wasteCost)}
              muted
            />
          )}
          {bom.laborCost > 0 && (
            <TotalRow
              label={t('GlassEnclosure.Cost.Labor')}
              value={formatter.format(bom.laborCost)}
            />
          )}
          {bom.transportCost > 0 && (
            <TotalRow
              label={t('GlassEnclosure.Cost.Transport')}
              value={formatter.format(bom.transportCost)}
            />
          )}
          {bom.scaffoldingCost > 0 && (
            <TotalRow
              label={t('GlassEnclosure.Cost.Scaffolding')}
              value={formatter.format(bom.scaffoldingCost)}
            />
          )}
          {bom.craneCost > 0 && (
            <TotalRow
              label={t('GlassEnclosure.Cost.Crane')}
              value={formatter.format(bom.craneCost)}
            />
          )}
          <Divider />
          <TotalRow
            label={t('GlassEnclosure.Cost.BaseCost')}
            value={formatter.format(bom.subtotal)}
            bold
          />
          {bom.marginAmount > 0 && (
            <TotalRow
              label={t('GlassEnclosure.Cost.Margin')}
              value={formatter.format(bom.marginAmount)}
              muted
            />
          )}
          {bom.taxAmount > 0 && (
            <TotalRow
              label={t('GlassEnclosure.Cost.Tax')}
              value={formatter.format(bom.taxAmount)}
              muted
            />
          )}
          <Divider />
          <TotalRow
            label={t('GlassEnclosure.Cost.GrandTotal')}
            value={formatter.format(bom.grandTotal)}
            accent
          />
        </dl>
      </section>

      <footer className="rounded-lg border border-success-200 bg-success-50 p-3 text-xs text-success-700 dark:border-success-700/40 dark:bg-success-950/30 dark:text-success-300">
        <CheckCircle2 size={14} className="mr-1 inline" />
        {t('GlassEnclosure.Quote.SourceNote')}
      </footer>
    </section>
  );
}

interface BomLineRowProps {
  line: BOMLineDto;
  formatter: Intl.NumberFormat;
  numberFormatter: Intl.NumberFormat;
  isEditing: boolean;
  editPrice: string;
  onEditPriceChange: (value: string) => void;
  onStartEdit: () => void;
  onCancelEdit: () => void;
  onSaveOverride: () => void;
  onClearOverride: () => void;
  onPushToCatalog: () => void;
  onDelete: () => void;
  disabled: boolean;
}

const BomLineRow = ({
  line,
  formatter,
  numberFormatter,
  isEditing,
  editPrice,
  onEditPriceChange,
  onStartEdit,
  onCancelEdit,
  onSaveOverride,
  onClearOverride,
  onPushToCatalog,
  onDelete,
  disabled,
}: BomLineRowProps) => {
  const { t } = useTranslation();
  const isOverridden = line.unitPriceOverride !== null;

  return (
    <tr>
      <Td>
        <span className="inline-flex flex-wrap items-center gap-1.5">
          {line.description}
          {line.isManual && (
            <Chip variant="violet">
              {t('GlassEnclosure.Quote.CustomBadge', { defaultValue: 'Custom' })}
            </Chip>
          )}
          {isOverridden && (
            <Chip variant="amber">
              {t('GlassEnclosure.Quote.ManualBadge', { defaultValue: 'Manual' })}
            </Chip>
          )}
        </span>
      </Td>
      <Td align="right" mono>
        {numberFormatter.format(line.quantity)}
      </Td>
      <Td>{line.unit}</Td>
      <Td align="right" mono>
        {isEditing ? (
          <input
            type="number"
            min={0}
            step="0.01"
            value={editPrice}
            onChange={(e) => onEditPriceChange(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === 'Enter') onSaveOverride();
              if (e.key === 'Escape') onCancelEdit();
            }}
            autoFocus
            className="w-28 rounded border border-primary-400 bg-white px-2 py-0.5 text-right font-mono text-sm text-slate-900 focus:outline-none focus:ring-2 focus:ring-primary-500 dark:border-primary-600 dark:bg-slate-900 dark:text-slate-100"
            aria-label={t('GlassEnclosure.Quote.EditPrice', { defaultValue: 'Edit unit price' })}
          />
        ) : (
          <span className="inline-flex items-center gap-1.5">
            {isOverridden && (
              <span
                className="text-xs text-slate-400 line-through dark:text-slate-500"
                title={t('GlassEnclosure.Quote.OriginalPrice', {
                  defaultValue: 'List price',
                })}
              >
                {formatter.format(line.unitCost)}
              </span>
            )}
            {formatter.format(effectiveUnitPrice(line))}
          </span>
        )}
      </Td>
      <Td align="right" mono bold>
        {formatter.format(line.lineCost)}
      </Td>
      <Td align="right">
        <span className="inline-flex items-center justify-end gap-1">
          {isEditing ? (
            <>
              <IconButton
                label={t('Common.Save', { defaultValue: 'Save' })}
                onClick={onSaveOverride}
                disabled={disabled}
                variant="confirm"
              >
                <Check size={14} />
              </IconButton>
              <IconButton
                label={t('Common.Cancel', { defaultValue: 'Cancel' })}
                onClick={onCancelEdit}
                disabled={disabled}
              >
                <X size={14} />
              </IconButton>
            </>
          ) : (
            <>
              <IconButton
                label={t('GlassEnclosure.Quote.EditPrice', { defaultValue: 'Edit unit price' })}
                onClick={onStartEdit}
                disabled={disabled}
              >
                <Pencil size={14} />
              </IconButton>
              {isOverridden && (
                <IconButton
                  label={t('GlassEnclosure.Quote.ClearOverride', {
                    defaultValue: 'Restore list price',
                  })}
                  onClick={onClearOverride}
                  disabled={disabled}
                >
                  <RotateCcw size={14} />
                </IconButton>
              )}
              {canPushToCatalog(line) && (
                <IconButton
                  label={t('GlassEnclosure.Quote.PushToCatalog', {
                    defaultValue: 'Update price list',
                  })}
                  onClick={onPushToCatalog}
                  disabled={disabled}
                  variant="accent"
                >
                  <Upload size={14} />
                </IconButton>
              )}
              {line.isManual && (
                <IconButton
                  label={t('GlassEnclosure.Quote.DeleteCustomLine', {
                    defaultValue: 'Remove custom line',
                  })}
                  onClick={onDelete}
                  disabled={disabled}
                  variant="danger"
                >
                  <Trash2 size={14} />
                </IconButton>
              )}
            </>
          )}
        </span>
      </Td>
    </tr>
  );
};

interface AddManualLineFormProps {
  isPending: boolean;
  onSubmit: (input: AddManualBomLineInput) => void | Promise<void>;
  onCancel: () => void;
}

const AddManualLineForm = ({ isPending, onSubmit, onCancel }: AddManualLineFormProps) => {
  const { t } = useTranslation();
  const [description, setDescription] = useState('');
  const [quantity, setQuantity] = useState('1');
  const [unit, setUnit] = useState('Piece');
  const [unitPrice, setUnitPrice] = useState('');
  const [kind, setKind] = useState<BOMLineDto['kind']>('HardwarePiece');

  const parsedQuantity = Number(quantity);
  const parsedUnitPrice = Number(unitPrice);
  const isValid =
    description.trim().length > 0 &&
    unit.trim().length > 0 &&
    Number.isFinite(parsedQuantity) &&
    parsedQuantity > 0 &&
    Number.isFinite(parsedUnitPrice) &&
    parsedUnitPrice >= 0;

  const submit = () => {
    if (!isValid) return;
    void onSubmit({
      description: description.trim(),
      quantity: parsedQuantity,
      unit: unit.trim(),
      unitPrice: parsedUnitPrice,
      kind,
    });
  };

  const inputClass =
    'rounded border border-slate-300 bg-white px-2 py-1.5 text-sm text-slate-900 focus:outline-none focus:ring-2 focus:ring-success-500 dark:border-slate-600 dark:bg-slate-900 dark:text-slate-100';

  return (
    <section className="rounded-lg border border-success-200 bg-success-50/50 p-3 dark:border-success-700/40 dark:bg-success-950/20">
      <h3 className="mb-2 text-xs font-semibold uppercase tracking-wide text-success-700 dark:text-success-300">
        {t('GlassEnclosure.Quote.AddCustomLine', { defaultValue: 'Add custom line' })}
      </h3>
      <div className="grid grid-cols-2 gap-2 md:grid-cols-5 xl:grid-cols-6">
        <input
          type="text"
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          placeholder={t('GlassEnclosure.Quote.Description')}
          className={`${inputClass} col-span-2`}
          maxLength={500}
        />
        <input
          type="number"
          min={0}
          step="0.001"
          value={quantity}
          onChange={(e) => setQuantity(e.target.value)}
          placeholder={t('GlassEnclosure.Quote.Qty')}
          className={inputClass}
        />
        <input
          type="text"
          value={unit}
          onChange={(e) => setUnit(e.target.value)}
          placeholder={t('GlassEnclosure.Quote.Unit')}
          className={inputClass}
          maxLength={20}
        />
        <input
          type="number"
          min={0}
          step="0.01"
          value={unitPrice}
          onChange={(e) => setUnitPrice(e.target.value)}
          placeholder={t('GlassEnclosure.Quote.UnitCost')}
          className={inputClass}
        />
        <select
          value={kind}
          onChange={(e) => setKind(e.target.value as BOMLineDto['kind'])}
          className={inputClass}
          aria-label={t('GlassEnclosure.Quote.Category', { defaultValue: 'Category' })}
        >
          {MANUAL_KIND_OPTIONS.map((option) => (
            <option key={option} value={option}>
              {t(`GlassEnclosure.Quote.Group.${option}` as never)}
            </option>
          ))}
        </select>
      </div>
      <div className="mt-2 flex justify-end gap-2">
        <button
          type="button"
          onClick={onCancel}
          disabled={isPending}
          className="rounded-md border border-slate-300 px-3 py-1.5 text-sm font-medium text-slate-700 hover:bg-slate-50 disabled:opacity-50 dark:border-slate-600 dark:text-slate-200 dark:hover:bg-slate-800"
        >
          {t('Common.Cancel', { defaultValue: 'Cancel' })}
        </button>
        <button
          type="button"
          onClick={submit}
          disabled={isPending || !isValid}
          className="inline-flex items-center gap-1.5 rounded-md bg-success-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-success-700 disabled:opacity-50"
        >
          <Plus size={14} />
          {t('GlassEnclosure.Quote.AddLine', { defaultValue: 'Add line' })}
        </button>
      </div>
    </section>
  );
};

const Chip = ({
  children,
  variant,
}: {
  children: React.ReactNode;
  variant: 'amber' | 'violet';
}) => (
  <span
    className={`inline-flex items-center rounded-full border px-1.5 py-0.5 text-[10px] font-medium ${
      variant === 'amber'
        ? 'border-warning-300 bg-warning-50 text-warning-700 dark:border-warning-700/50 dark:bg-warning-950/40 dark:text-warning-300'
        : 'border-violet-300 bg-violet-50 text-violet-700 dark:border-violet-700/50 dark:bg-violet-950/40 dark:text-violet-300'
    }`}
  >
    {children}
  </span>
);

const IconButton = ({
  label,
  onClick,
  disabled,
  variant,
  children,
}: {
  label: string;
  onClick: () => void;
  disabled?: boolean;
  variant?: 'confirm' | 'accent' | 'danger';
  children: React.ReactNode;
}) => (
  <button
    type="button"
    onClick={onClick}
    disabled={disabled}
    title={label}
    aria-label={label}
    className={`rounded p-1 transition disabled:opacity-40 ${
      variant === 'danger'
        ? 'text-danger-500 hover:bg-danger-50 dark:hover:bg-danger-950/40'
        : variant === 'accent'
          ? 'text-primary-600 hover:bg-primary-50 dark:text-primary-400 dark:hover:bg-primary-950/40'
          : variant === 'confirm'
            ? 'text-success-600 hover:bg-success-50 dark:text-success-400 dark:hover:bg-success-950/40'
            : 'text-slate-500 hover:bg-slate-100 dark:text-slate-400 dark:hover:bg-slate-700'
    }`}
  >
    {children}
  </button>
);

const Stat = ({ label, value }: { label: string; value: string }) => (
  <div className="min-w-0 rounded border border-slate-200 bg-white p-2 dark:border-slate-700 dark:bg-slate-800">
    <dt className="truncate text-[10px] uppercase tracking-wide text-slate-500 dark:text-slate-400">
      {label}
    </dt>
    <dd className="truncate font-mono text-base font-semibold text-slate-900 dark:text-slate-100">
      {value}
    </dd>
  </div>
);

const Th = ({ children, align }: { children: React.ReactNode; align?: 'right' }) => (
  <th
    className={`px-3 py-2 text-[10px] font-medium uppercase tracking-wide text-slate-500 dark:text-slate-400 ${
      align === 'right' ? 'text-right' : 'text-left'
    }`}
  >
    {children}
  </th>
);

const Td = ({
  children,
  align,
  mono,
  bold,
}: {
  children: React.ReactNode;
  align?: 'right';
  mono?: boolean;
  bold?: boolean;
}) => (
  <td
    className={`px-3 py-1.5 text-slate-700 dark:text-slate-300 ${align === 'right' ? 'text-right' : 'text-left'} ${
      mono ? 'font-mono' : ''
    } ${bold ? 'font-semibold text-slate-900 dark:text-slate-100' : ''}`}
  >
    {children}
  </td>
);

const TotalRow = ({
  label,
  value,
  bold,
  muted,
  accent,
}: {
  label: string;
  value: string;
  bold?: boolean;
  muted?: boolean;
  accent?: boolean;
}) => (
  <div className="flex items-center justify-between">
    <dt
      className={`${
        muted ? 'text-slate-400 dark:text-slate-500' : 'text-slate-600 dark:text-slate-400'
      } ${accent ? 'text-base font-semibold text-slate-900 dark:text-slate-100' : ''}`}
    >
      {label}
    </dt>
    <dd
      className={`font-mono ${
        accent
          ? 'text-lg font-bold text-success-700 dark:text-success-300'
          : bold
            ? 'font-semibold text-slate-900 dark:text-slate-100'
            : muted
              ? 'text-slate-500 dark:text-slate-400'
              : 'text-slate-800 dark:text-slate-200'
      }`}
    >
      {value}
    </dd>
  </div>
);

const Divider = () => <div className="my-1 border-t border-slate-200 dark:border-slate-700" />;

const exportQuoteCsv = (bom: BOMSummaryDto, project: GlassProjectDto) => {
  const lines: string[] = [
    `${project.code},${project.projectName},${project.customerName ?? ''}`,
    '',
    'group,description,quantity,unit,unit_cost,line_cost,currency',
  ];
  for (const line of bom.lines) {
    const sanitized = line.description.replace(/"/g, "'");
    lines.push(
      `${line.kind},"${sanitized}",${line.quantity},${line.unit},${effectiveUnitPrice(line)},${line.lineCost},${line.currency}`,
    );
  }
  lines.push('');
  lines.push(`,,,,SUBTOTAL,${bom.subtotal},${bom.currency}`);
  lines.push(`,,,,MARGIN,${bom.marginAmount},${bom.currency}`);
  lines.push(`,,,,TAX,${bom.taxAmount},${bom.currency}`);
  lines.push(`,,,,GRAND_TOTAL,${bom.grandTotal},${bom.currency}`);

  const blob = new Blob([lines.join('\n')], { type: 'text/csv;charset=utf-8' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = `quote-${project.code}.csv`;
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
  URL.revokeObjectURL(url);
};
