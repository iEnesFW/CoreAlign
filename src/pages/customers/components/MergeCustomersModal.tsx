import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { AlertTriangle, ArrowRight, Users, X } from 'lucide-react';
import {
  customersApi,
  type MergeCustomersInput,
  type MergeCustomersResult,
} from '@/features/customers/api/customersApi';
import { customerKeys } from '@/features/customers/hooks/customerKeys';
import type { Customer } from '@/features/customers/model/customer.types';
import { safeRequest } from '@/shared/lib/safeRequest';
import { logger } from '@/shared/lib/logger';
import { Modal } from '@/shared/ui/Modal/Modal';
import { Button } from '@/shared/ui/Button/Button';
import { fieldBaseClasses } from '@/shared/lib/fieldClasses';

interface MergeCustomersModalProps {
  open: boolean;
  onClose: () => void;
  initialSource?: Customer | null;
  initialTarget?: Customer | null;
  onMerged?: (result: MergeCustomersResult) => void;
}

const newOperationId = (): string => {
  const cryptoApi = typeof globalThis !== 'undefined' ? globalThis.crypto : undefined;
  if (cryptoApi && typeof cryptoApi.randomUUID === 'function') {
    return cryptoApi.randomUUID();
  }
  return `${Date.now().toString(16)}-${Math.random().toString(16).slice(2, 18)}`;
};

interface CustomerSearchListProps {
  label: string;
  selected: Customer | null;
  onSelect: (c: Customer | null) => void;
}

const CustomerSearchList = ({ label, selected, onSelect }: CustomerSearchListProps) => {
  const { t } = useTranslation();
  const [search, setSearch] = useState('');
  const trimmed = search.trim();

  const query = useQuery({
    queryKey: customerKeys.list({ page: 1, pageSize: 10, search: trimmed || undefined }),
    queryFn: () => customersApi.list({ page: 1, pageSize: 10, search: trimmed || undefined }),
    enabled: trimmed.length > 0,
  });

  const candidates = query.data?.data?.items ?? [];

  return (
    <div className="flex flex-col gap-2">
      <label className="text-xs font-semibold text-slate-700 dark:text-slate-200">{label}</label>
      <input
        type="search"
        value={search}
        onChange={(e) => setSearch(e.target.value)}
        placeholder={t('customers.merge.searchPlaceholder')}
        className={fieldBaseClasses(false)}
      />
      {selected ? (
        <div className="flex items-center justify-between gap-2 rounded-md border border-primary-300 bg-primary-50 px-2 py-1.5 text-sm dark:border-primary-700 dark:bg-primary-900/30">
          <span className="truncate font-medium text-primary-900 dark:text-primary-100">
            {selected.name}
          </span>
          <button
            type="button"
            onClick={() => onSelect(null)}
            className="rounded p-0.5 text-primary-700 hover:bg-primary-100 dark:text-primary-300 dark:hover:bg-primary-800"
            aria-label="Clear selection"
          >
            <X size={12} />
          </button>
        </div>
      ) : (
        <ul className="max-h-40 divide-y divide-slate-200 overflow-y-auto rounded-md border border-slate-200 dark:divide-slate-700 dark:border-slate-700">
          {trimmed.length === 0 ? (
            <li className="px-2 py-1.5 text-xs text-slate-500 dark:text-slate-400">
              {t('customers.merge.searchPlaceholder')}
            </li>
          ) : candidates.length === 0 ? (
            <li className="px-2 py-1.5 text-xs text-slate-500 dark:text-slate-400">
              {t('common.noResults', { defaultValue: 'No results.' })}
            </li>
          ) : (
            candidates.map((c) => (
              <li key={c.id}>
                <button
                  type="button"
                  onClick={() => onSelect(c)}
                  className="flex w-full items-center justify-between gap-2 px-2 py-1.5 text-left text-sm text-slate-700 transition hover:bg-slate-50 dark:text-slate-200 dark:hover:bg-slate-800"
                >
                  <span className="truncate">{c.name}</span>
                  {c.code && <span className="font-mono text-xs text-slate-400">{c.code}</span>}
                </button>
              </li>
            ))
          )}
        </ul>
      )}
    </div>
  );
};

export const MergeCustomersModal = ({
  open,
  onClose,
  initialSource,
  initialTarget,
  onMerged,
}: MergeCustomersModalProps) => {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [source, setSource] = useState<Customer | null>(initialSource ?? null);
  const [target, setTarget] = useState<Customer | null>(initialTarget ?? null);
  const [operationId] = useState(() => newOperationId());
  const [error, setError] = useState<string | null>(null);

  const canMerge = useMemo(
    () => Boolean(source && target && source.id !== target.id),
    [source, target],
  );

  const mergeMutation = useMutation({
    mutationFn: async (input: MergeCustomersInput) => {
      setError(null);
      const [response, requestError] = await safeRequest(customersApi.merge(input));
      if (requestError || !response) {
        logger.warn('Customer merge failed', { input, error: String(requestError) });
        throw requestError ?? new Error('Merge failed');
      }
      return response;
    },
    onSuccess: (response) => {
      queryClient.invalidateQueries({ queryKey: customerKeys.lists() });
      if (source) {
        queryClient.removeQueries({ queryKey: customerKeys.detail(source.id) });
      }
      if (target) {
        queryClient.invalidateQueries({ queryKey: customerKeys.detail(target.id) });
      }
      const data = response?.data;
      if (data && onMerged) onMerged(data);
      onClose();
    },
    onError: (err: unknown) => {
      setError(err instanceof Error ? err.message : 'Merge failed');
    },
  });

  const handleConfirm = () => {
    if (!source || !target) {
      setError(t('customers.merge.missingSelectionError'));
      return;
    }
    if (source.id === target.id) {
      setError(t('customers.merge.sameIdError'));
      return;
    }
    mergeMutation.mutate({
      operationId,
      sourceCustomerId: source.id,
      targetCustomerId: target.id,
      sourceUpdatedAtUtc: source.updatedAtUtc,
      targetUpdatedAtUtc: target.updatedAtUtc,
    });
  };

  return (
    <Modal
      open={open}
      title={t('customers.merge.title')}
      subtitle={t('customers.merge.subtitle')}
      icon={<Users size={18} />}
      onClose={onClose}
      size="xl"
      footer={
        <>
          <Button
            variant="ghost"
            type="button"
            onClick={onClose}
            disabled={mergeMutation.isPending}
          >
            {t('customers.merge.cancel')}
          </Button>
          <Button
            type="button"
            onClick={handleConfirm}
            disabled={!canMerge || mergeMutation.isPending}
            isLoading={mergeMutation.isPending}
          >
            {mergeMutation.isPending
              ? t('customers.merge.executing')
              : t('customers.merge.confirm')}
          </Button>
        </>
      }
    >
      <div className="mb-4 flex items-start gap-2 rounded-md border border-warning-200 bg-warning-50 p-2 dark:border-warning-700 dark:bg-warning-900/20">
        <AlertTriangle
          size={14}
          className="mt-0.5 flex-shrink-0 text-warning-600 dark:text-warning-400"
        />
        <p className="text-xs text-warning-800 dark:text-warning-200">
          {t('customers.merge.warning')}
        </p>
      </div>

      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
        <CustomerSearchList
          label={t('customers.merge.source')}
          selected={source}
          onSelect={setSource}
        />
        <CustomerSearchList
          label={t('customers.merge.target')}
          selected={target}
          onSelect={setTarget}
        />
      </div>

      {source && target && (
        <div className="mt-3 flex items-center justify-center gap-2 text-xs text-slate-500 dark:text-slate-400">
          <span className="truncate font-medium">{source.name}</span>
          <ArrowRight size={12} />
          <span className="truncate font-medium text-primary-600 dark:text-primary-400">
            {target.name}
          </span>
        </div>
      )}

      {error && <p className="mt-3 text-xs text-danger-600 dark:text-danger-400">{error}</p>}
    </Modal>
  );
};
