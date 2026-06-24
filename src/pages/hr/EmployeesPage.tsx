import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { Plus, Users } from 'lucide-react';
import { formatCurrency, formatDate } from '@/shared/lib/format';
import { useFormatLocale } from '@/shared/lib/useFormatLocale';
import { useDebouncedValue } from '@/shared/hooks/useDebouncedValue';
import { DataToolbar } from '@/shared/ui/DataToolbar/DataToolbar';
import { SegmentedControl } from '@/shared/ui/SegmentedControl/SegmentedControl';
import { PageHeader } from '@/shared/ui/PageHeader/PageHeader';
import { ListPageTemplate } from '@/shared/ui/PageTemplate/PageTemplate';
import { Button } from '@/shared/ui/Button/Button';
import { Badge, type BadgeVariant } from '@/shared/ui/Badge/Badge';
import { EmployeeFormModal } from '@/features/hr/ui/EmployeeFormModal';
import { useEmployeesQuery } from '@/features/hr/hooks/useEmployees';
import { EMPLOYMENT_STATUSES, type EmploymentStatus } from '@/features/hr/model/enums';

type StatusFilter = 'all' | EmploymentStatus;

const STATUS_VARIANT: Record<EmploymentStatus, BadgeVariant> = {
  Active: 'success',
  OnLeave: 'warning',
  Terminated: 'neutral',
};

const STATUS_FALLBACK: Record<EmploymentStatus, string> = {
  Active: 'Aktif',
  OnLeave: 'İzinde',
  Terminated: 'İşten Ayrıldı',
};

export const EmployeesPage = () => {
  const { t } = useTranslation();
  const locale = useFormatLocale();
  const navigate = useNavigate();

  const [status, setStatus] = useState<StatusFilter>('all');
  const [searchInput, setSearchInput] = useState('');
  const [page, setPage] = useState(1);
  const [showCreate, setShowCreate] = useState(false);

  const search = useDebouncedValue(searchInput, 300);

  const statusLabel = (s: EmploymentStatus) =>
    t(`Payroll.employmentStatus.${s}`, { defaultValue: STATUS_FALLBACK[s] });

  const params = useMemo(
    () => ({
      status: status === 'all' ? undefined : status,
      search: search || undefined,
      page,
      pageSize: 25,
    }),
    [status, search, page],
  );

  const query = useEmployeesQuery(params);
  const employees = query.data?.data?.items ?? [];
  const total = query.data?.data?.total ?? 0;
  const totalPages = query.data?.data?.totalPages ?? 0;

  const hasActiveFilters = status !== 'all' || searchInput !== '';
  const clearFilters = () => {
    setStatus('all');
    setSearchInput('');
    setPage(1);
  };

  return (
    <ListPageTemplate
      header={
        <PageHeader
          icon={<Users size={20} />}
          title={t('Payroll.employees.title', { defaultValue: 'Personel' })}
          subtitle={t('Payroll.employees.subtitle', {
            defaultValue: 'Personel kayıtlarını yönetin, maaş ve kesintileri tanımlayın.',
          })}
          actions={
            <Button size="sm" onClick={() => setShowCreate(true)}>
              <Plus size={14} />
              {t('Payroll.employees.new', { defaultValue: 'Yeni Personel' })}
            </Button>
          }
        />
      }
      toolbar={
        <DataToolbar
          search={{
            value: searchInput,
            onChange: (v) => {
              setSearchInput(v);
              setPage(1);
            },
            placeholder: t('Payroll.employees.searchPlaceholder', {
              defaultValue: 'Ad, sicil no veya departman ara…',
            }),
          }}
          viewMode={
            <SegmentedControl
              value={status}
              onChange={(v) => {
                setStatus(v);
                setPage(1);
              }}
              ariaLabel={t('Payroll.employees.statusAria', {
                defaultValue: 'Duruma göre filtrele',
              })}
              options={[
                { value: 'all', label: t('Payroll.employees.all', { defaultValue: 'Tümü' }) },
                ...EMPLOYMENT_STATUSES.map((s) => ({ value: s, label: statusLabel(s) })),
              ]}
            />
          }
          resultCount={{
            count: total,
            label: t('Payroll.employees.resultCountLabel', { defaultValue: 'personel' }),
          }}
          hasActiveFilters={hasActiveFilters}
          onClearFilters={clearFilters}
        />
      }
      pagination={
        totalPages > 1 ? (
          <div className="flex items-center justify-end gap-1 text-xs">
            <Button
              variant="outline"
              size="sm"
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              disabled={page === 1}
            >
              {t('common.prev', { defaultValue: 'Önceki' })}
            </Button>
            <span className="px-2 text-slate-500">
              {page} / {totalPages}
            </span>
            <Button
              variant="outline"
              size="sm"
              onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
              disabled={page === totalPages}
            >
              {t('common.next', { defaultValue: 'Sonraki' })}
            </Button>
          </div>
        ) : undefined
      }
    >
      <div className="overflow-x-auto rounded-lg border border-slate-200 dark:border-slate-800">
        {query.isPending ? (
          <div className="px-3 py-8 text-center text-sm text-slate-500">
            {t('common.loading', { defaultValue: 'Yükleniyor…' })}
          </div>
        ) : employees.length === 0 ? (
          <div className="px-3 py-10 text-center text-sm text-slate-500 dark:text-slate-400">
            {t('Payroll.employees.empty', { defaultValue: 'Personel bulunamadı.' })}
          </div>
        ) : (
          <table className="w-full text-sm">
            <thead className="bg-slate-50/60 text-[10px] uppercase tracking-wider text-slate-500 dark:bg-slate-900/30 dark:text-slate-400">
              <tr>
                <th className="px-3 py-2 text-left">
                  {t('Payroll.employees.cols.number', { defaultValue: 'Sicil' })}
                </th>
                <th className="px-3 py-2 text-left">
                  {t('Payroll.employees.cols.name', { defaultValue: 'Ad Soyad' })}
                </th>
                <th className="px-3 py-2 text-left">
                  {t('Payroll.employees.cols.tc', { defaultValue: 'TC Kimlik' })}
                </th>
                <th className="px-3 py-2 text-left">
                  {t('Payroll.employees.cols.department', { defaultValue: 'Departman' })}
                </th>
                <th className="px-3 py-2 text-center">
                  {t('Payroll.employees.cols.status', { defaultValue: 'Durum' })}
                </th>
                <th className="px-3 py-2 text-right">
                  {t('Payroll.employees.cols.baseSalary', { defaultValue: 'Brüt Maaş' })}
                </th>
                <th className="px-3 py-2 text-left">
                  {t('Payroll.employees.cols.hireDate', { defaultValue: 'İşe Giriş' })}
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
              {employees.map((e) => (
                <tr
                  key={e.id}
                  onClick={() => navigate(`/dashboard/hr/employees/${e.id}`)}
                  className="cursor-pointer hover:bg-slate-50/40 dark:hover:bg-slate-800/30"
                >
                  <td className="px-3 py-2 font-mono text-xs text-slate-700 dark:text-slate-300">
                    {e.employeeNumber}
                  </td>
                  <td className="px-3 py-2 font-medium text-slate-800 dark:text-slate-100">
                    {e.fullName}
                    {e.title && (
                      <div className="text-[11px] font-normal text-slate-500">{e.title}</div>
                    )}
                  </td>
                  <td className="px-3 py-2 font-mono text-xs text-slate-500 dark:text-slate-400">
                    {e.nationalIdMasked ?? '—'}
                  </td>
                  <td className="px-3 py-2 text-slate-600 dark:text-slate-300">
                    {e.department ?? '—'}
                  </td>
                  <td className="px-3 py-2 text-center">
                    <Badge variant={STATUS_VARIANT[e.status]}>{statusLabel(e.status)}</Badge>
                  </td>
                  <td className="px-3 py-2 text-right font-mono text-slate-800 dark:text-slate-200">
                    {formatCurrency(e.baseSalaryGross, locale, e.salaryCurrency)}
                  </td>
                  <td className="px-3 py-2 text-xs text-slate-500 dark:text-slate-400">
                    {formatDate(e.hireDate, locale)}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {showCreate && <EmployeeFormModal employee={null} onClose={() => setShowCreate(false)} />}
    </ListPageTemplate>
  );
};

export default EmployeesPage;
