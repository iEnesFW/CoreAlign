import { useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Check, ChevronDown, Plus, Tag as TagIcon } from 'lucide-react';
import { toastApiError } from '@/shared/lib/mutationToast';
import { TagChip } from './TagChip';
import { useCreateTag, useTagsQuery } from '../hooks/useTags';

interface TagMultiSelectProps {
  value: string[];
  onChange: (next: string[]) => void;
}

export const TagMultiSelect = ({ value, onChange }: TagMultiSelectProps) => {
  const { t } = useTranslation();
  const tagsQuery = useTagsQuery(true);
  const createTag = useCreateTag();

  const [open, setOpen] = useState(false);
  const [filter, setFilter] = useState('');
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) return;
    const onClickOutside = (e: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) setOpen(false);
    };
    document.addEventListener('mousedown', onClickOutside);
    return () => document.removeEventListener('mousedown', onClickOutside);
  }, [open]);

  const tags = tagsQuery.data?.data ?? [];
  const selectedSet = new Set(value);
  const selectedTags = tags.filter((tg) => selectedSet.has(tg.id));

  const q = filter.trim().toLowerCase();
  const filtered = q ? tags.filter((tg) => tg.name.toLowerCase().includes(q)) : tags;

  const exactMatch = tags.some((tg) => tg.name.toLowerCase() === filter.trim().toLowerCase());
  const canCreate = filter.trim().length > 0 && !exactMatch;

  const toggle = (id: string) => {
    onChange(selectedSet.has(id) ? value.filter((v) => v !== id) : [...value, id]);
  };

  const handleCreate = async () => {
    const name = filter.trim();
    if (!name) return;
    try {
      const result = await createTag.mutateAsync({ name });
      if (result.isSuccess && result.data) {
        onChange([...value, result.data.id]);
        setFilter('');
      }
    } catch (err) {
      toastApiError(err);
    }
  };

  return (
    <div ref={containerRef} className="relative">
      <button
        type="button"
        onClick={() => setOpen((o) => !o)}
        className="flex w-full items-center justify-between gap-2 rounded border border-slate-200 bg-white px-2.5 py-1.5 text-left text-[13px] text-slate-900 focus:border-indigo-500 focus:outline-none focus:ring-1 focus:ring-indigo-500 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-100"
      >
        <span className="flex flex-wrap items-center gap-1">
          {selectedTags.length === 0 ? (
            <span className="inline-flex items-center gap-1 text-slate-400">
              <TagIcon size={12} />
              {t('tags.selectPlaceholder')}
            </span>
          ) : (
            selectedTags.map((tg) => (
              <TagChip
                key={tg.id}
                name={tg.name}
                colorHex={tg.colorHex}
                onRemove={() => toggle(tg.id)}
              />
            ))
          )}
        </span>
        <ChevronDown size={14} className="shrink-0 text-slate-400" />
      </button>

      {open && (
        <div className="absolute z-20 mt-1 w-full rounded-lg border border-slate-200 bg-white shadow-lg dark:border-slate-700 dark:bg-slate-900">
          <div className="border-b border-slate-100 p-2 dark:border-slate-800">
            <input
              type="text"
              value={filter}
              onChange={(e) => setFilter(e.target.value)}
              placeholder={t('tags.searchOrCreate')}
              className="w-full rounded border border-slate-200 bg-white px-2 py-1 text-xs text-slate-900 focus:border-indigo-500 focus:outline-none dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100"
            />
          </div>
          <ul className="max-h-44 overflow-y-auto py-1">
            {filtered.map((tg) => {
              const checked = selectedSet.has(tg.id);
              return (
                <li key={tg.id}>
                  <button
                    type="button"
                    onClick={() => toggle(tg.id)}
                    className="flex w-full items-center justify-between gap-2 px-2.5 py-1.5 text-left text-xs hover:bg-slate-50 dark:hover:bg-slate-800"
                  >
                    <TagChip name={tg.name} colorHex={tg.colorHex} />
                    {checked && (
                      <Check size={13} className="text-indigo-600 dark:text-indigo-400" />
                    )}
                  </button>
                </li>
              );
            })}
            {filtered.length === 0 && !canCreate && (
              <li className="px-2.5 py-2 text-center text-[11px] text-slate-400">
                {t('tags.empty')}
              </li>
            )}
          </ul>
          {canCreate && (
            <button
              type="button"
              onClick={handleCreate}
              disabled={createTag.isPending}
              className="flex w-full items-center gap-1.5 border-t border-slate-100 px-2.5 py-2 text-left text-xs font-medium text-indigo-600 hover:bg-indigo-50 disabled:opacity-50 dark:border-slate-800 dark:text-indigo-300 dark:hover:bg-indigo-500/10"
            >
              <Plus size={12} />
              {t('tags.createNamed', { name: filter.trim() })}
            </button>
          )}
        </div>
      )}
    </div>
  );
};
