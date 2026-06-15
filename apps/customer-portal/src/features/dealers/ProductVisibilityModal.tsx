import { Check, Search, X } from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { toast } from 'sonner';
import { Button } from '@/shared/ui/Button';
import { Input } from '@/shared/ui/Input';
import { Modal } from '@/shared/ui/Modal';
import { Spinner } from '@/shared/ui/Spinner';
import type {
  CatalogProductSummary,
  DealerProductVisibility,
  DealerProductVisibilityMode,
} from './api';
import {
  useCustomerCatalogProducts,
  useDealerVisibility,
  useSetDealerVisibility,
} from './visibilityHooks';

interface ProductVisibilityModalProps {
  open: boolean;
  onClose: () => void;
  linkId: string;
  dealerName: string;
}

const DEBOUNCE_MS = 250;

export const ProductVisibilityModal = ({
  open,
  onClose,
  linkId,
  dealerName,
}: ProductVisibilityModalProps) => {
  const { t } = useTranslation();
  const visibilityQuery = useDealerVisibility(open ? linkId : undefined);

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={t('dealers.visibility.title')}
      description={dealerName}
      size="lg"
    >
      {visibilityQuery.isLoading || !visibilityQuery.data ? (
        <div className="flex items-center gap-2 py-6 text-sm text-slate-500">
          <Spinner /> {t('common.loading')}
        </div>
      ) : (
        <ProductVisibilityEditor
          key={`${linkId}:${visibilityQuery.data.mode}:${visibilityQuery.data.visibleProductIds.length}`}
          linkId={linkId}
          initial={visibilityQuery.data}
          onClose={onClose}
        />
      )}
    </Modal>
  );
};

interface ProductVisibilityEditorProps {
  linkId: string;
  initial: DealerProductVisibility;
  onClose: () => void;
}

const ProductVisibilityEditor = ({ linkId, initial, onClose }: ProductVisibilityEditorProps) => {
  const { t } = useTranslation();
  const setVisibility = useSetDealerVisibility(linkId);

  const [mode, setMode] = useState<DealerProductVisibilityMode>(initial.mode);
  const [selected, setSelected] = useState<Map<string, CatalogProductSummary>>(() => {
    const map = new Map<string, CatalogProductSummary>();
    initial.visibleProductIds.forEach((id) => {
      map.set(id, { id, sku: '', name: id });
    });
    return map;
  });
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');

  useEffect(() => {
    const handle = window.setTimeout(() => setDebouncedSearch(search.trim()), DEBOUNCE_MS);
    return () => window.clearTimeout(handle);
  }, [search]);

  const catalogQuery = useCustomerCatalogProducts(debouncedSearch, mode === 'Whitelist');

  const selectedList = useMemo(() => Array.from(selected.values()), [selected]);

  const onToggleProduct = (product: CatalogProductSummary) => {
    setSelected((prev) => {
      const next = new Map(prev);
      if (next.has(product.id)) {
        next.delete(product.id);
      } else {
        next.set(product.id, product);
      }
      return next;
    });
  };

  const onRemoveSelected = (id: string) => {
    setSelected((prev) => {
      const next = new Map(prev);
      next.delete(id);
      return next;
    });
  };

  const handleMutationError = (caught: unknown) => {
    const err = caught as { normalizedMessage?: string; message?: string; status?: number };
    if (err.status !== 401 && err.status !== 403) {
      toast.error(err.normalizedMessage ?? err.message ?? t('errors.unknown'));
    }
  };

  const onSave = () => {
    if (mode === 'Whitelist' && selected.size === 0) {
      toast.error(t('dealers.visibility.emptyWhitelist'));
      return;
    }
    setVisibility.mutate(
      {
        mode,
        productIds: mode === 'Whitelist' ? Array.from(selected.keys()) : [],
      },
      {
        onSuccess: () => {
          toast.success(t('dealers.visibility.saved'));
          onClose();
        },
        onError: handleMutationError,
      },
    );
  };

  return (
    <div className="flex flex-col gap-5">
      <fieldset className="flex flex-col gap-2">
        <legend className="text-sm font-medium text-slate-700 dark:text-slate-200">
          {t('dealers.visibility.modeLabel')}
        </legend>
        <label className="flex cursor-pointer items-start gap-3 rounded-xl border border-slate-200 px-4 py-3 text-sm transition hover:border-sky-400 dark:border-slate-700 dark:hover:border-sky-500">
          <input
            type="radio"
            name="visibility-mode"
            className="mt-1"
            checked={mode === 'All'}
            onChange={() => setMode('All')}
            disabled={setVisibility.isPending}
          />
          <div>
            <p className="font-medium text-slate-900 dark:text-slate-100">
              {t('dealers.visibility.modeAllTitle')}
            </p>
            <p className="text-xs text-slate-500 dark:text-slate-400">
              {t('dealers.visibility.modeAllDescription')}
            </p>
          </div>
        </label>
        <label className="flex cursor-pointer items-start gap-3 rounded-xl border border-slate-200 px-4 py-3 text-sm transition hover:border-sky-400 dark:border-slate-700 dark:hover:border-sky-500">
          <input
            type="radio"
            name="visibility-mode"
            className="mt-1"
            checked={mode === 'Whitelist'}
            onChange={() => setMode('Whitelist')}
            disabled={setVisibility.isPending}
          />
          <div>
            <p className="font-medium text-slate-900 dark:text-slate-100">
              {t('dealers.visibility.modeWhitelistTitle')}
            </p>
            <p className="text-xs text-slate-500 dark:text-slate-400">
              {t('dealers.visibility.modeWhitelistDescription')}
            </p>
          </div>
        </label>
      </fieldset>

      {mode === 'Whitelist' ? (
        <div className="flex flex-col gap-3">
          <div className="relative">
            <Search
              size={14}
              className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-slate-400"
            />
            <Input
              type="search"
              placeholder={t('dealers.visibility.searchPlaceholder')}
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              className="pl-9"
              disabled={setVisibility.isPending}
            />
          </div>

          <div className="rounded-xl border border-slate-200 dark:border-slate-700">
            <p className="border-b border-slate-100 px-3 py-2 text-xs font-medium uppercase tracking-wide text-slate-500 dark:border-slate-800 dark:text-slate-400">
              {t('dealers.visibility.catalogResults')}
            </p>
            {catalogQuery.isFetching ? (
              <div className="flex items-center gap-2 px-3 py-4 text-xs text-slate-500">
                <Spinner size={12} /> {t('common.loading')}
              </div>
            ) : (catalogQuery.data?.items.length ?? 0) === 0 ? (
              <p className="px-3 py-4 text-xs text-slate-500">
                {t('dealers.visibility.noResults')}
              </p>
            ) : (
              <ul className="max-h-56 divide-y divide-slate-100 overflow-y-auto dark:divide-slate-800">
                {catalogQuery.data!.items.map((product) => {
                  const isSelected = selected.has(product.id);
                  return (
                    <li key={product.id}>
                      <button
                        type="button"
                        onClick={() => onToggleProduct(product)}
                        className="flex w-full items-center justify-between gap-3 px-3 py-2 text-left text-sm transition hover:bg-sky-50 dark:hover:bg-sky-900/20"
                      >
                        <div className="min-w-0">
                          <p className="truncate font-medium text-slate-900 dark:text-slate-100">
                            {product.name}
                          </p>
                          <p className="truncate text-xs text-slate-500">{product.sku}</p>
                        </div>
                        {isSelected ? (
                          <span className="flex items-center gap-1 text-xs font-semibold text-sky-600 dark:text-sky-400">
                            <Check size={14} /> {t('dealers.visibility.selected')}
                          </span>
                        ) : (
                          <span className="text-xs text-slate-400">
                            {t('dealers.visibility.add')}
                          </span>
                        )}
                      </button>
                    </li>
                  );
                })}
              </ul>
            )}
          </div>

          <div className="rounded-xl border border-slate-200 dark:border-slate-700">
            <p className="border-b border-slate-100 px-3 py-2 text-xs font-medium uppercase tracking-wide text-slate-500 dark:border-slate-800 dark:text-slate-400">
              {t('dealers.visibility.selectedTitle', { count: selectedList.length })}
            </p>
            {selectedList.length === 0 ? (
              <p className="px-3 py-4 text-xs text-slate-500">
                {t('dealers.visibility.noneSelected')}
              </p>
            ) : (
              <ul className="max-h-40 overflow-y-auto">
                {selectedList.map((product) => (
                  <li
                    key={product.id}
                    className="flex items-center justify-between gap-3 border-b border-slate-50 px-3 py-2 text-sm last:border-none dark:border-slate-800"
                  >
                    <div className="min-w-0">
                      <p className="truncate font-medium text-slate-900 dark:text-slate-100">
                        {product.name}
                      </p>
                      {product.sku ? (
                        <p className="truncate text-xs text-slate-500">{product.sku}</p>
                      ) : null}
                    </div>
                    <button
                      type="button"
                      onClick={() => onRemoveSelected(product.id)}
                      className="rounded-md p-1 text-slate-400 transition hover:bg-slate-100 hover:text-rose-500 dark:hover:bg-slate-800"
                      aria-label={t('dealers.visibility.remove')}
                    >
                      <X size={14} />
                    </button>
                  </li>
                ))}
              </ul>
            )}
          </div>
        </div>
      ) : null}

      <div className="flex flex-wrap items-center justify-end gap-2 pt-2">
        <Button type="button" variant="ghost" onClick={onClose} disabled={setVisibility.isPending}>
          {t('common.cancel')}
        </Button>
        <Button type="button" onClick={onSave} disabled={setVisibility.isPending}>
          {setVisibility.isPending ? <Spinner size={16} className="text-white" /> : null}
          {setVisibility.isPending ? t('dealers.visibility.saving') : t('dealers.visibility.save')}
        </Button>
      </div>
    </div>
  );
};
