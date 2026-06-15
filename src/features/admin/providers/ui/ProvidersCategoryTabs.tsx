import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  SegmentedControl,
  type SegmentOption,
} from '@/shared/ui/SegmentedControl/SegmentedControl';
import { EmptyState } from '@/shared/ui/EmptyState/EmptyState';
import { ProviderCard } from './ProviderCard';
import type { ProviderInfo } from '../api/providersAdminApi';
import type { ProviderCategory } from '../providers.types';

type TabKey = 'EFatura' | 'Payment' | 'LaserMeter' | 'Other';

const TAB_CATEGORIES: Record<TabKey, ProviderCategory[]> = {
  EFatura: ['EFatura'],
  Payment: ['Payment'],
  LaserMeter: ['LaserMeter'],
  Other: [
    'LabelPrinter',
    'CncExport',
    'CadImport',
    'Freight',
    'BankReconciliation',
    'Calendar',
    'Export',
    'Sms',
    'WhatsApp',
  ],
};

interface Props {
  providers: ProviderInfo[];
}

export const ProvidersCategoryTabs = ({ providers }: Props) => {
  const { t } = useTranslation();
  const [tab, setTab] = useState<TabKey>('EFatura');

  const grouped = useMemo(() => {
    const out: Record<TabKey, ProviderInfo[]> = {
      EFatura: [],
      Payment: [],
      LaserMeter: [],
      Other: [],
    };
    for (const p of providers) {
      (Object.keys(TAB_CATEGORIES) as TabKey[]).forEach((key) => {
        if (TAB_CATEGORIES[key].includes(p.category)) {
          out[key].push(p);
        }
      });
    }
    return out;
  }, [providers]);

  const options: SegmentOption<TabKey>[] = [
    {
      value: 'EFatura',
      label: t('Admin.Providers.Category.EFatura'),
      count: grouped.EFatura.length,
    },
    {
      value: 'Payment',
      label: t('Admin.Providers.Category.Payment'),
      count: grouped.Payment.length,
    },
    {
      value: 'LaserMeter',
      label: t('Admin.Providers.Category.LaserMeter'),
      count: grouped.LaserMeter.length,
    },
    {
      value: 'Other',
      label: t('Admin.Providers.Category.Other'),
      count: grouped.Other.length,
    },
  ];

  const active = grouped[tab];

  return (
    <div className="space-y-4">
      <SegmentedControl
        value={tab}
        onChange={setTab}
        options={options}
        ariaLabel={t('Admin.Providers.CategoryTabs')}
      />
      {active.length === 0 ? (
        <EmptyState title={t('Admin.Providers.Empty')} />
      ) : (
        <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
          {active.map((p) => (
            <ProviderCard key={`${p.category}:${p.name}`} provider={p} />
          ))}
        </div>
      )}
    </div>
  );
};
