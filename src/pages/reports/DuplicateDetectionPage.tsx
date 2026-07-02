import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { CopyCheck, ExternalLink, Users } from 'lucide-react';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { ListPageTemplate } from '@/shared/ui/PageTemplate/PageTemplate';
import { useDuplicatesQuery } from '@/features/reports/hooks/useReportQueries';
import type { DuplicateEntity, DuplicateKeyKind } from '@/features/reports/model/reports.types';

const ENTITIES: DuplicateEntity[] = ['customer', 'vendor'];
const KEYS: DuplicateKeyKind[] = ['Email', 'TaxNumber', 'NationalId'];

function Toggle<T extends string>({
  value,
  options,
  onChange,
  labelFor,
}: {
  value: T;
  options: T[];
  onChange: (v: T) => void;
  labelFor: (v: T) => string;
}) {
  return (
    <div className="inline-flex rounded-lg border border-slate-200 p-0.5 dark:border-slate-700">
      {options.map((o) => (
        <button
          key={o}
          type="button"
          onClick={() => onChange(o)}
          className={`rounded-md px-2.5 py-1 text-xs font-medium transition ${
            value === o
              ? 'bg-primary-600 text-white'
              : 'text-slate-600 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-800'
          }`}
        >
          {labelFor(o)}
        </button>
      ))}
    </div>
  );
}

export const DuplicateDetectionPage = () => {
  const { t } = useTranslation();
  const [entity, setEntity] = useState<DuplicateEntity>('customer');
  const [key, setKey] = useState<DuplicateKeyKind>('Email');

  const query = useDuplicatesQuery({ entity, key });
  const data = query.data?.data;
  const groups = data?.groups ?? [];
  const detailBase = entity === 'vendor' ? '/dashboard/vendors' : '/dashboard/customers';

  return (
    <ListPageTemplate
      header={
        <PageHeader
          icon={<CopyCheck size={20} />}
          title={t('DuplicateDetection.title', { defaultValue: 'Yinelenen Kayıt Tespiti' })}
          subtitle={t('DuplicateDetection.subtitle', {
            defaultValue: 'Aynı e-posta / VKN / TCKN ile birden çok müşteri veya tedarikçi.',
          })}
        />
      }
      toolbar={
        <div className="flex flex-wrap items-center gap-3">
          <Toggle
            value={entity}
            options={ENTITIES}
            onChange={setEntity}
            labelFor={(e) => t(`DuplicateDetection.entity.${e}` as const, { defaultValue: e })}
          />
          <Toggle
            value={key}
            options={KEYS}
            onChange={setKey}
            labelFor={(k) => t(`DuplicateDetection.key.${k}` as const, { defaultValue: k })}
          />
          <span className="ml-auto text-[11px] text-slate-500 dark:text-slate-400">
            {t('DuplicateDetection.count', {
              defaultValue: '{{count}} grup',
              count: data?.groupCount ?? 0,
            })}
          </span>
        </div>
      }
    >
      {query.isPending ? (
        <div className="px-3 py-8 text-center text-sm text-slate-500">
          {t('common.loading', { defaultValue: 'Yükleniyor…' })}
        </div>
      ) : groups.length === 0 ? (
        <div className="rounded-lg border border-success-200 bg-success-50/50 px-3 py-10 text-center text-sm text-success-700 dark:border-success-500/30 dark:bg-success-500/10 dark:text-success-300">
          {t('DuplicateDetection.empty', { defaultValue: 'Yinelenen kayıt bulunamadı.' })}
        </div>
      ) : (
        <div className="space-y-3">
          {groups.map((g) => (
            <div
              key={g.keyValue}
              className="overflow-hidden rounded-lg border border-warning-200 dark:border-warning-500/30"
            >
              <div className="flex items-center justify-between gap-2 bg-warning-50/60 px-3 py-2 dark:bg-warning-500/10">
                <span className="font-mono text-sm font-semibold text-warning-800 dark:text-warning-300">
                  {g.keyValue}
                </span>
                <span className="inline-flex items-center gap-1 rounded bg-warning-100 px-1.5 text-[10px] font-medium text-warning-800 dark:bg-warning-500/20 dark:text-warning-300">
                  <Users size={11} />
                  {t('DuplicateDetection.records', {
                    defaultValue: '{{count}} kayıt',
                    count: g.count,
                  })}
                </span>
              </div>
              <ul className="divide-y divide-slate-200 dark:divide-slate-800">
                {g.members.map((m) => (
                  <li key={m.id} className="px-3 py-2 text-sm">
                    <Link
                      to={`${detailBase}/${m.id}`}
                      className="inline-flex items-center gap-1 text-primary-600 hover:underline dark:text-primary-400"
                    >
                      {m.name}
                      <ExternalLink size={11} />
                    </Link>
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>
      )}
    </ListPageTemplate>
  );
};

export default DuplicateDetectionPage;
