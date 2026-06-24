import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Tags } from 'lucide-react';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { ListPageTemplate } from '@/shared/ui/PageTemplate/PageTemplate';
import { usePriceListsQuery } from '@/shared/master-data/hooks/useMasterData';
import type { PriceList } from '@/shared/master-data/model/masterData.types';
import { PriceListItemsGrid } from './PriceListItemsGrid';

export const PriceListsPage = () => {
  const { t } = useTranslation();
  const listsQ = usePriceListsQuery();
  const lists = listsQ.data?.data ?? [];
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const selected = lists.find((l) => l.id === selectedId) ?? lists[0];

  return (
    <ListPageTemplate
      header={
        <PageHeader
          icon={<Tags size={20} />}
          title={t('Settings.PriceLists.Title')}
          subtitle={t('Settings.PriceLists.Subtitle')}
        />
      }
    >
      {listsQ.isPending && <p className="text-xs text-slate-400">{t('Common.Loading')}</p>}

      {lists.length === 0 && !listsQ.isPending && (
        <p className="text-xs text-slate-500 dark:text-slate-400">
          {t('Settings.PriceLists.Empty')}
        </p>
      )}

      {lists.length > 0 && (
        <div className="grid grid-cols-1 gap-4 md:grid-cols-[260px_minmax(0,1fr)]">
          <aside className="space-y-1">
            {lists.map((list) => (
              <PriceListCard
                key={list.id}
                list={list}
                active={selected?.id === list.id}
                onSelect={() => setSelectedId(list.id)}
              />
            ))}
          </aside>
          {selected && (
            <section className="rounded-lg border border-slate-200 p-3 dark:border-slate-800">
              <PriceListItemsGrid priceList={selected} />
            </section>
          )}
        </div>
      )}
    </ListPageTemplate>
  );
};

interface CardProps {
  list: PriceList;
  active: boolean;
  onSelect: () => void;
}

const PriceListCard = ({ list, active, onSelect }: CardProps) => (
  <button
    type="button"
    onClick={onSelect}
    className={`block w-full rounded-md border px-3 py-2 text-left text-xs transition ${
      active
        ? 'border-primary-500 bg-primary-50 text-primary-700 dark:border-primary-400 dark:bg-primary-500/15 dark:text-primary-200'
        : 'border-slate-200 bg-white text-slate-700 hover:bg-slate-50 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-300 dark:hover:bg-slate-800'
    }`}
  >
    <div className="font-semibold">{list.name}</div>
    <div className="text-[11px] text-slate-500 dark:text-slate-400">
      {list.code} · {list.currency}
      {list.isDefault ? ' · *' : ''}
    </div>
  </button>
);

export default PriceListsPage;
