import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Pencil, Plus, Trash2 } from 'lucide-react';
import { toastApiError } from '@/shared/lib/mutationToast';
import { useConfirm } from '@/shared/ui/ConfirmDialog/useConfirm';
import { TagChip } from './TagChip';
import { useCreateTag, useDeleteTag, useTagsQuery, useUpdateTag } from '../hooks/useTags';
import type { Tag } from '../model/tag.types';

interface FormState {
  name: string;
  colorHex: string;
  isActive: boolean;
}

const DEFAULT_COLOR = '#6366f1';
const emptyForm: FormState = { name: '', colorHex: DEFAULT_COLOR, isActive: true };

export const TagManager = () => {
  const { t } = useTranslation();
  const confirm = useConfirm();
  const tagsQuery = useTagsQuery();
  const createTag = useCreateTag();
  const updateTag = useUpdateTag();
  const deleteTag = useDeleteTag();

  const [editing, setEditing] = useState<Tag | 'new' | null>(null);
  const [form, setForm] = useState<FormState>(emptyForm);

  const tags = tagsQuery.data?.data ?? [];
  const saving = createTag.isPending || updateTag.isPending;

  const openCreate = () => {
    setForm(emptyForm);
    setEditing('new');
  };

  const openEdit = (tag: Tag) => {
    setForm({ name: tag.name, colorHex: tag.colorHex ?? DEFAULT_COLOR, isActive: tag.isActive });
    setEditing(tag);
  };

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    const name = form.name.trim();
    if (!name) return;
    try {
      if (editing === 'new') {
        await createTag.mutateAsync({ name, colorHex: form.colorHex });
        toast.success(t('common.created'));
      } else if (editing) {
        await updateTag.mutateAsync({
          id: editing.id,
          name,
          colorHex: form.colorHex,
          isActive: form.isActive,
        });
        toast.success(t('common.updated'));
      }
      setEditing(null);
    } catch (err) {
      toastApiError(err);
    }
  };

  const onDelete = async (tag: Tag) => {
    const ok = await confirm({
      title: t('common.delete'),
      message: t('tags.deleteConfirm', { name: tag.name }),
      confirmLabel: t('common.delete'),
      tone: 'danger',
    });
    if (!ok) return;
    try {
      await deleteTag.mutateAsync(tag.id);
      toast.success(t('common.deleted'));
    } catch (err) {
      toastApiError(err);
    }
  };

  const inputClass =
    'mt-1 w-full rounded border border-slate-300 bg-white px-2 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100';

  return (
    <section>
      <div className="mb-2 flex items-center justify-between gap-2">
        <h3 className="text-sm font-semibold text-slate-900 dark:text-slate-100">
          {t('tags.manageTitle')}
        </h3>
        <button
          type="button"
          onClick={openCreate}
          className="inline-flex items-center gap-1.5 rounded bg-indigo-600 px-2.5 py-1.5 text-xs font-semibold text-white hover:bg-indigo-700"
        >
          <Plus size={12} />
          {t('common.new')}
        </button>
      </div>

      <div className="overflow-hidden rounded-lg border border-slate-200 dark:border-slate-800">
        <table className="w-full text-sm">
          <thead className="bg-slate-50 text-[10px] font-semibold uppercase text-slate-600 dark:bg-slate-800/50 dark:text-slate-300">
            <tr>
              <th className="px-3 py-2 text-left">{t('tags.fields.name')}</th>
              <th className="px-3 py-2 text-center">{t('common.active')}</th>
              <th className="px-3 py-2" />
            </tr>
          </thead>
          <tbody>
            {tags.map((tag) => (
              <tr key={tag.id} className="border-t border-slate-100 dark:border-slate-800">
                <td className="px-3 py-2">
                  <TagChip name={tag.name} colorHex={tag.colorHex} />
                </td>
                <td className="px-3 py-2 text-center text-xs text-slate-700 dark:text-slate-300">
                  {tag.isActive ? t('common.active') : t('common.inactive')}
                </td>
                <td className="px-3 py-2 text-right">
                  <div className="inline-flex items-center gap-1">
                    <button
                      type="button"
                      onClick={() => openEdit(tag)}
                      className="rounded p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-700 dark:hover:bg-slate-800 dark:hover:text-slate-200"
                      title={t('common.edit')}
                    >
                      <Pencil size={13} />
                    </button>
                    <button
                      type="button"
                      onClick={() => onDelete(tag)}
                      className="rounded p-1 text-slate-400 hover:bg-rose-50 hover:text-rose-700 dark:hover:bg-rose-500/10"
                      title={t('common.delete')}
                    >
                      <Trash2 size={13} />
                    </button>
                  </div>
                </td>
              </tr>
            ))}
            {tags.length === 0 && !tagsQuery.isPending && (
              <tr>
                <td
                  colSpan={3}
                  className="px-3 py-4 text-center text-xs text-slate-500 dark:text-slate-400"
                >
                  {t('tags.empty')}
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {editing !== null && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 p-4">
          <form
            onSubmit={submit}
            className="w-full max-w-md space-y-3 rounded-lg bg-white p-4 shadow-xl dark:bg-slate-900"
          >
            <h3 className="text-sm font-semibold text-slate-900 dark:text-slate-100">
              {editing === 'new'
                ? `${t('tags.manageTitle')} — ${t('common.new')}`
                : `${t('tags.manageTitle')} — ${t('common.edit')}`}
            </h3>
            <div>
              <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
                {t('tags.fields.name')} *
              </label>
              <input
                type="text"
                required
                value={form.name}
                onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
                className={inputClass}
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-slate-700 dark:text-slate-300">
                {t('tags.fields.color')}
              </label>
              <div className="mt-1 flex items-center gap-2">
                <input
                  type="color"
                  value={form.colorHex}
                  onChange={(e) => setForm((f) => ({ ...f, colorHex: e.target.value }))}
                  className="h-8 w-12 cursor-pointer rounded border border-slate-300 dark:border-slate-700"
                />
                <TagChip name={form.name || t('tags.preview')} colorHex={form.colorHex} />
              </div>
            </div>
            {editing !== 'new' && (
              <label className="flex items-center gap-2 text-xs text-slate-700 dark:text-slate-300">
                <input
                  type="checkbox"
                  checked={form.isActive}
                  onChange={(e) => setForm((f) => ({ ...f, isActive: e.target.checked }))}
                />
                {t('common.active')}
              </label>
            )}
            <div className="flex justify-end gap-2 border-t border-slate-200 pt-3 dark:border-slate-800">
              <button
                type="button"
                onClick={() => setEditing(null)}
                className="rounded border border-slate-200 bg-white px-3 py-1.5 text-xs font-medium text-slate-700 hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
              >
                {t('common.cancel')}
              </button>
              <button
                type="submit"
                disabled={saving}
                className="rounded bg-indigo-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-indigo-700 disabled:opacity-50"
              >
                {saving ? t('common.saving') : t('common.save')}
              </button>
            </div>
          </form>
        </div>
      )}
    </section>
  );
};
