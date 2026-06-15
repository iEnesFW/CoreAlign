import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Plus, X } from 'lucide-react';
import { customersApi } from '@/features/customers/api/customersApi';
import { useTagsQuery } from '@/features/tags/hooks/useTags';
import { TagChip } from '@/features/tags/ui/TagChip';
import { safeRequest } from '@/shared/lib/safeRequest';
import { logger } from '@/shared/lib/logger';

interface CustomerTagsEditorProps {
  customerId: string;
}

const customerTagsKey = (id: string) => ['customers', 'tags', id] as const;

export const CustomerTagsEditor = ({ customerId }: CustomerTagsEditorProps) => {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [showPicker, setShowPicker] = useState(false);

  const customerTagsQuery = useQuery({
    queryKey: customerTagsKey(customerId),
    queryFn: () => customersApi.listTags(customerId),
  });
  const tagsQuery = useTagsQuery(true);

  const attachedTags = useMemo(() => customerTagsQuery.data?.data ?? [], [customerTagsQuery.data]);
  const attachedTagIds = useMemo(() => new Set(attachedTags.map((tag) => tag.id)), [attachedTags]);
  const availableTags = (tagsQuery.data?.data ?? []).filter((tag) => !attachedTagIds.has(tag.id));

  const attachMutation = useMutation({
    mutationFn: async (tagId: string) => {
      const [, error] = await safeRequest(customersApi.attachTag(customerId, tagId));
      if (error) {
        logger.warn('Attach tag failed', { customerId, tagId, error: String(error) });
        throw error;
      }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: customerTagsKey(customerId) });
    },
  });

  const detachMutation = useMutation({
    mutationFn: async (tagId: string) => {
      const [, error] = await safeRequest(customersApi.detachTag(customerId, tagId));
      if (error) {
        logger.warn('Detach tag failed', { customerId, tagId, error: String(error) });
        throw error;
      }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: customerTagsKey(customerId) });
    },
  });

  return (
    <div className="rounded-lg border border-slate-200 bg-white p-3 dark:border-slate-800 dark:bg-slate-900">
      <div className="mb-2 flex items-center justify-between">
        <h3 className="text-sm font-semibold text-slate-900 dark:text-slate-100">
          {t('customers.tagsEditor.title')}
        </h3>
        <button
          type="button"
          onClick={() => setShowPicker((prev) => !prev)}
          className="inline-flex items-center gap-1 rounded-md border border-slate-300 bg-white px-2 py-1 text-xs font-medium text-slate-700 transition hover:bg-slate-50 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-300 dark:hover:bg-slate-700"
          aria-label={t('customers.tagsEditor.add')}
        >
          <Plus size={12} />
          {t('customers.tagsEditor.add')}
        </button>
      </div>

      <div className="flex flex-wrap gap-1.5">
        {attachedTags.length === 0 ? (
          <span className="text-xs text-slate-500 dark:text-slate-400">
            {t('customers.tagsEditor.noTags')}
          </span>
        ) : (
          attachedTags.map((tag) => (
            <span key={tag.id} className="inline-flex items-center gap-1">
              <TagChip
                name={tag.name}
                colorHex={tag.colorHex}
                onRemove={() => detachMutation.mutate(tag.id)}
              />
            </span>
          ))
        )}
      </div>

      {showPicker && (
        <div className="mt-3 border-t border-slate-200 pt-3 dark:border-slate-800">
          <p className="mb-2 text-xs font-medium text-slate-600 dark:text-slate-400">
            {t('customers.tagsEditor.available')}
          </p>
          {availableTags.length === 0 ? (
            <span className="text-xs text-slate-500 dark:text-slate-400">
              {t('customers.tagsEditor.noTags')}
            </span>
          ) : (
            <div className="flex flex-wrap gap-1.5">
              {availableTags.map((tag) => (
                <button
                  key={tag.id}
                  type="button"
                  onClick={() => attachMutation.mutate(tag.id)}
                  disabled={attachMutation.isPending}
                  className="inline-flex items-center gap-1 rounded-full border border-dashed border-slate-300 px-2 py-0.5 text-[11px] font-medium text-slate-600 transition hover:border-indigo-400 hover:text-indigo-600 disabled:cursor-not-allowed disabled:opacity-60 dark:border-slate-700 dark:text-slate-300 dark:hover:border-indigo-400 dark:hover:text-indigo-300"
                  aria-label={t('customers.tagsEditor.add')}
                >
                  <Plus size={10} />
                  {tag.name}
                </button>
              ))}
            </div>
          )}
        </div>
      )}

      {(attachMutation.isError || detachMutation.isError) && (
        <p className="mt-2 inline-flex items-center gap-1 text-xs text-rose-600 dark:text-rose-400">
          <X size={11} />
          {t('common.error', { defaultValue: 'Action failed.' })}
        </p>
      )}
    </div>
  );
};
