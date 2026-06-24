import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { Pencil, Plus, Trash2 } from 'lucide-react';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useConfirm } from '@/shared/ui/ConfirmDialog/useConfirm';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { Input } from '@/shared/ui/Input/Input';

export type FieldValue = string | boolean;
export type FieldValues = Record<string, FieldValue>;

export interface MdField {
  name: string;
  label: string;
  type: 'text' | 'number' | 'checkbox';
  required?: boolean;
  placeholder?: string;
}

export interface MdColumn<T> {
  key: string;
  label: string;
  align?: 'left' | 'right' | 'center';
  render?: (row: T) => React.ReactNode;
}

interface Props<T extends { id: string; isActive: boolean }> {
  title: string;
  queryKey: readonly unknown[];
  items: T[];
  isLoading: boolean;
  fields: MdField[];
  columns: MdColumn<T>[];
  toInitialValues: (row: T) => FieldValues;
  create: (values: FieldValues) => Promise<unknown>;
  update: (id: string, values: FieldValues, isActive: boolean) => Promise<unknown>;
  remove: (id: string) => Promise<unknown>;
}

const emptyValues = (fields: MdField[]): FieldValues =>
  Object.fromEntries(fields.map((f) => [f.name, f.type === 'checkbox' ? false : '']));

const ALIGN: Record<'left' | 'right' | 'center', string> = {
  left: 'text-left',
  right: 'text-right',
  center: 'text-center',
};

export function MasterDataManager<T extends { id: string; isActive: boolean }>({
  title,
  queryKey,
  items,
  isLoading,
  fields,
  columns,
  toInitialValues,
  create,
  update,
  remove,
}: Props<T>) {
  const { t } = useTranslation();
  const confirm = useConfirm();
  const qc = useQueryClient();
  const [editing, setEditing] = useState<T | 'new' | null>(null);
  const [values, setValues] = useState<FieldValues>(emptyValues(fields));
  const [saving, setSaving] = useState(false);

  const openCreate = () => {
    setValues(emptyValues(fields));
    setEditing('new');
  };
  const openEdit = (row: T) => {
    setValues(toInitialValues(row));
    setEditing(row);
  };

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    try {
      if (editing === 'new') {
        await create(values);
        toast.success(t('common.created', { defaultValue: 'Oluşturuldu.' }));
      } else if (editing) {
        await update(editing.id, values, editing.isActive);
        toast.success(t('common.updated', { defaultValue: 'Güncellendi.' }));
      }
      qc.invalidateQueries({ queryKey });
      setEditing(null);
    } catch (err) {
      toastApiError(err);
    } finally {
      setSaving(false);
    }
  };

  const onDelete = async (row: T) => {
    const ok = await confirm({
      title: t('common.delete', { defaultValue: 'Sil' }),
      message: t('masterData.deleteConfirm', { defaultValue: 'Bu kayıt silinsin mi?' }),
      confirmLabel: t('common.delete', { defaultValue: 'Sil' }),
      tone: 'danger',
    });
    if (!ok) return;
    try {
      await remove(row.id);
      qc.invalidateQueries({ queryKey });
      toast.success(t('common.deleted', { defaultValue: 'Silindi.' }));
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <section>
      <div className="mb-2 flex items-center justify-between gap-2">
        <h3 className="text-sm font-semibold text-slate-900 dark:text-slate-100">{title}</h3>
        <button
          type="button"
          onClick={openCreate}
          className="inline-flex items-center gap-1.5 rounded bg-primary-600 px-2.5 py-1.5 text-xs font-semibold text-white hover:bg-primary-700"
        >
          <Plus size={12} />
          {t('common.new', { defaultValue: 'Yeni' })}
        </button>
      </div>

      <div className="overflow-hidden rounded-lg border border-slate-200 dark:border-slate-800">
        <table className="w-full text-sm">
          <thead className="bg-slate-50 text-[10px] font-semibold uppercase text-slate-600 dark:bg-slate-800/50 dark:text-slate-300">
            <tr>
              {columns.map((c) => (
                <th key={c.key} className={`px-3 py-2 ${ALIGN[c.align ?? 'left']}`}>
                  {c.label}
                </th>
              ))}
              <th className="px-3 py-2" />
            </tr>
          </thead>
          <tbody>
            {items.map((row) => (
              <tr key={row.id} className="border-t border-slate-100 dark:border-slate-800">
                {columns.map((c) => (
                  <td
                    key={c.key}
                    className={`px-3 py-2 text-xs text-slate-700 dark:text-slate-300 ${ALIGN[c.align ?? 'left']}`}
                  >
                    {c.render
                      ? c.render(row)
                      : String((row as Record<string, unknown>)[c.key] ?? '—')}
                  </td>
                ))}
                <td className="px-3 py-2 text-right">
                  <div className="inline-flex items-center gap-1">
                    <button
                      type="button"
                      onClick={() => openEdit(row)}
                      className="rounded p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-700 dark:hover:bg-slate-800 dark:hover:text-slate-200"
                      title={t('common.edit', { defaultValue: 'Düzenle' })}
                    >
                      <Pencil size={13} />
                    </button>
                    <button
                      type="button"
                      onClick={() => onDelete(row)}
                      className="rounded p-1 text-slate-400 hover:bg-danger-50 hover:text-danger-700 dark:hover:bg-danger-500/10"
                      title={t('common.delete', { defaultValue: 'Sil' })}
                    >
                      <Trash2 size={13} />
                    </button>
                  </div>
                </td>
              </tr>
            ))}
            {items.length === 0 && !isLoading && (
              <tr>
                <td
                  colSpan={columns.length + 1}
                  className="px-3 py-4 text-center text-xs text-slate-500 dark:text-slate-400"
                >
                  {t('masterData.empty', { defaultValue: 'Henüz kayıt yok.' })}
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      <Modal
        open={editing !== null}
        title={
          editing === 'new'
            ? `${title} — ${t('common.new', { defaultValue: 'Yeni' })}`
            : `${title} — ${t('common.edit', { defaultValue: 'Düzenle' })}`
        }
        icon={<Plus size={18} />}
        onClose={() => setEditing(null)}
        size="md"
        footer={
          <>
            <Button type="button" variant="ghost" onClick={() => setEditing(null)}>
              {t('common.cancel', { defaultValue: 'İptal' })}
            </Button>
            <Button type="submit" form="master-data-form" isLoading={saving}>
              {saving
                ? t('common.saving', { defaultValue: 'Kaydediliyor…' })
                : t('common.save', { defaultValue: 'Kaydet' })}
            </Button>
          </>
        }
      >
        <form id="master-data-form" onSubmit={submit} className="space-y-3">
          {fields.map((f) =>
            f.type === 'checkbox' ? (
              <label
                key={f.name}
                className="flex items-center gap-2 text-xs text-slate-700 dark:text-slate-300"
              >
                <input
                  type="checkbox"
                  checked={Boolean(values[f.name])}
                  onChange={(e) => setValues((v) => ({ ...v, [f.name]: e.target.checked }))}
                />
                {f.label}
              </label>
            ) : (
              <Input
                key={f.name}
                label={f.label}
                type={f.type === 'number' ? 'number' : 'text'}
                step={f.type === 'number' ? 'any' : undefined}
                required={f.required}
                value={String(values[f.name] ?? '')}
                placeholder={f.placeholder}
                onChange={(e) => setValues((v) => ({ ...v, [f.name]: e.target.value }))}
              />
            ),
          )}
        </form>
      </Modal>
    </section>
  );
}
