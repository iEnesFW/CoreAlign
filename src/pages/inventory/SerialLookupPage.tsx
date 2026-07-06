import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ScanBarcode, Search } from 'lucide-react';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { ListPageTemplate } from '@/shared/ui/PageTemplate/PageTemplate';
import { Input } from '@/shared/ui/Input/Input';
import { Button } from '@/shared/ui/Button/Button';
import { Badge } from '@/shared/ui/Badge/Badge';
import { useSerialWhereUsedQuery } from '@/features/serials/hooks/useSerialQueries';
import type { SerialStatus } from '@/features/serials/model/serial.types';

const STATUS_VARIANT: Record<SerialStatus, 'success' | 'info' | 'warning' | 'danger'> = {
  InStock: 'success',
  Shipped: 'info',
  Returned: 'warning',
  Scrapped: 'danger',
};

export const SerialLookupPage = () => {
  const { t } = useTranslation();
  const [input, setInput] = useState('');
  const [term, setTerm] = useState('');

  const query = useSerialWhereUsedQuery(term, term.length > 0);
  const results = query.data?.data ?? [];

  const submit = (e: React.FormEvent) => {
    e.preventDefault();
    setTerm(input.trim());
  };

  return (
    <ListPageTemplate
      header={
        <PageHeader
          icon={<ScanBarcode size={20} />}
          title={t('Serials.title')}
          subtitle={t('Serials.subtitle')}
        />
      }
    >
      <form onSubmit={submit} className="mb-4 flex gap-2">
        <div className="max-w-md flex-1">
          <Input
            value={input}
            onChange={(e) => setInput(e.target.value)}
            placeholder={t('Serials.searchPlaceholder')}
          />
        </div>
        <Button type="submit" disabled={input.trim().length === 0}>
          <Search size={16} /> {t('Serials.search')}
        </Button>
      </form>

      {term.length > 0 && query.isSuccess && results.length === 0 && (
        <p className="text-sm text-slate-500 dark:text-slate-400">
          {t('Serials.notFound', { serial: term })}
        </p>
      )}

      <div className="space-y-3">
        {results.map((unit) => (
          <div
            key={unit.id}
            className="rounded-lg border border-slate-200 bg-white p-4 dark:border-slate-800 dark:bg-slate-900"
          >
            <div className="flex items-center justify-between gap-2">
              <span className="font-mono text-sm font-semibold text-slate-900 dark:text-slate-100">
                {unit.serialNumber}
              </span>
              <Badge variant={STATUS_VARIANT[unit.status]}>
                {t(`Serials.status.${unit.status}`)}
              </Badge>
            </div>
            <dl className="mt-3 grid grid-cols-2 gap-x-4 gap-y-1 text-xs sm:grid-cols-3">
              <Field label={t('Serials.fields.orderId')} value={unit.orderId} />
              <Field label={t('Serials.fields.shipmentId')} value={unit.shipmentId} />
              <Field label={t('Serials.fields.owner')} value={unit.currentOwnerCustomerId} />
              <Field label={t('Serials.fields.warehouseId')} value={unit.warehouseId} />
              <Field label={t('Serials.fields.lotId')} value={unit.lotId} />
              <Field label={t('Serials.fields.parent')} value={unit.parentSerialUnitId} />
            </dl>
            {unit.components.length > 0 && (
              <div className="mt-3 border-t border-slate-100 pt-2 dark:border-slate-800">
                <p className="mb-1 text-xs font-semibold text-slate-600 dark:text-slate-300">
                  {t('Serials.components')}
                </p>
                <ul className="space-y-1">
                  {unit.components.map((c) => (
                    <li key={c.id} className="flex items-center gap-2 text-xs">
                      <span className="font-mono text-slate-700 dark:text-slate-200">
                        {c.serialNumber}
                      </span>
                      <Badge variant={STATUS_VARIANT[c.status]}>
                        {t(`Serials.status.${c.status}`)}
                      </Badge>
                    </li>
                  ))}
                </ul>
              </div>
            )}
          </div>
        ))}
      </div>
    </ListPageTemplate>
  );
};

const Field = ({ label, value }: { label: string; value: string | null }) => (
  <div>
    <dt className="text-slate-400">{label}</dt>
    <dd className="truncate font-mono text-slate-700 dark:text-slate-200">{value ?? '—'}</dd>
  </div>
);
