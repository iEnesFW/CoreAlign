import { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Database,
  Upload,
  Play,
  CheckCircle2,
  ArrowRight,
  RefreshCw,
  FileSpreadsheet,
} from 'lucide-react';

type ERPType = 'excel' | 'logo' | 'netsis' | 'mikro' | 'sap';
type EntityType = 'customers' | 'products' | 'gl-accounts';

export const MigrationSection = () => {
  const { t } = useTranslation();
  const [erp, setErp] = useState<ERPType>('excel');
  const [entity, setEntity] = useState<EntityType>('customers');
  const [step, setStep] = useState<'setup' | 'simulating' | 'success'>('setup');
  const [progress, setProgress] = useState(0);
  const [sessionId, setSessionId] = useState('');
  const [importedCount, setImportedCount] = useState(0);

  const erps: { id: ERPType; label: string }[] = [
    { id: 'excel', label: 'Excel / CSV' },
    { id: 'logo', label: 'Logo Go / Tiger' },
    { id: 'netsis', label: 'Netsis ERP' },
    { id: 'mikro', label: 'Mikro Run / Fly' },
    { id: 'sap', label: 'SAP Business One' },
  ];

  const entities: { id: EntityType; label: string }[] = [
    { id: 'customers', label: t('LandingPage.migration.entityCustomers') },
    { id: 'products', label: t('LandingPage.migration.entityProducts') },
    { id: 'gl-accounts', label: t('LandingPage.migration.entityGlAccounts') },
  ];

  const getMapping = () => {
    if (entity === 'customers') {
      switch (erp) {
        case 'logo':
          return [
            { source: 'TITLE', target: 'CustomerName' },
            { source: 'EMAIL', target: 'Email' },
            { source: 'TEL', target: 'Phone' },
            { source: 'TAX_NO', target: 'TaxNumber' },
          ];
        case 'netsis':
          return [
            { source: 'CARI_ISIM', target: 'CustomerName' },
            { source: 'E_POSTA', target: 'Email' },
            { source: 'TEL_NO', target: 'Phone' },
            { source: 'VERGI_NUMARASI', target: 'TaxNumber' },
          ];
        case 'mikro':
          return [
            { source: 'cari_unvan', target: 'CustomerName' },
            { source: 'cari_eposta', target: 'Email' },
            { source: 'cari_tel', target: 'Phone' },
            { source: 'cari_vergi_no', target: 'TaxNumber' },
          ];
        case 'sap':
          return [
            { source: 'KNA1-NAME1', target: 'CustomerName' },
            { source: 'KNA1-SMTP_ADDR', target: 'Email' },
            { source: 'KNA1-TELF1', target: 'Phone' },
            { source: 'DFK00-STCD1', target: 'TaxNumber' },
          ];
        default:
          return [
            { source: 'Müşteri Adı', target: 'CustomerName' },
            { source: 'E-posta Adresi', target: 'Email' },
            { source: 'Telefon Numarası', target: 'Phone' },
            { source: 'Vergi Numarası', target: 'TaxNumber' },
          ];
      }
    } else if (entity === 'products') {
      switch (erp) {
        case 'logo':
          return [
            { source: 'NAME', target: 'ProductName' },
            { source: 'CODE', target: 'SKU' },
            { source: 'PRICE', target: 'UnitPrice' },
            { source: 'VAT_RATE', target: 'TaxRate' },
          ];
        case 'netsis':
          return [
            { source: 'STOK_ADI', target: 'ProductName' },
            { source: 'STOK_KODU', target: 'SKU' },
            { source: 'FIYAT', target: 'UnitPrice' },
            { source: 'KDV_ORANI', target: 'TaxRate' },
          ];
        case 'mikro':
          return [
            { source: 'sto_isim', target: 'ProductName' },
            { source: 'sto_kod', target: 'SKU' },
            { source: 'sto_fiyat', target: 'UnitPrice' },
            { source: 'sto_kdv', target: 'TaxRate' },
          ];
        case 'sap':
          return [
            { source: 'MAKT-MAKTX', target: 'ProductName' },
            { source: 'MARA-MATNR', target: 'SKU' },
            { source: 'MBEW-VERPR', target: 'UnitPrice' },
            { source: 'OVTG-Rate', target: 'TaxRate' },
          ];
        default:
          return [
            { source: 'Ürün Adı', target: 'ProductName' },
            { source: 'Stok Kodu', target: 'SKU' },
            { source: 'Birim Fiyat', target: 'UnitPrice' },
            { source: 'KDV Oranı', target: 'TaxRate' },
          ];
      }
    } else {
      switch (erp) {
        case 'logo':
          return [
            { source: 'ACCOUNT_NAME', target: 'AccountName' },
            { source: 'ACCOUNT_CODE', target: 'AccountCode' },
          ];
        case 'netsis':
          return [
            { source: 'HESAP_ISMI', target: 'AccountName' },
            { source: 'HESAP_KODU', target: 'AccountCode' },
          ];
        case 'mikro':
          return [
            { source: 'hesap_adi', target: 'AccountName' },
            { source: 'hesap_kodu', target: 'AccountCode' },
          ];
        case 'sap':
          return [
            { source: 'SKAT-TXT20', target: 'AccountName' },
            { source: 'SKA1-SAKNR', target: 'AccountCode' },
          ];
        default:
          return [
            { source: 'Hesap Adı', target: 'AccountName' },
            { source: 'Hesap Kodu', target: 'AccountCode' },
          ];
      }
    }
  };

  useEffect(() => {
    if (step !== 'simulating') return;

    const interval = setInterval(() => {
      setProgress((prev) => {
        const next = prev + 5;
        if (next >= 100) {
          clearInterval(interval);
          setStep('success');
          setSessionId('ca-mig-' + Math.random().toString(36).substring(2, 8).toUpperCase());
          setImportedCount(Math.floor(Math.random() * 2000) + 1200);
          return 100;
        }
        return next;
      });
    }, 150);

    return () => clearInterval(interval);
  }, [step]);

  const simStepText =
    step === 'simulating'
      ? progress < 20
        ? t('LandingPage.migration.stepRead')
        : progress < 40
          ? t('LandingPage.migration.stepVal')
          : progress < 60
            ? t('LandingPage.migration.stepNormalize')
            : progress < 80
              ? t('LandingPage.migration.stepEncrypt')
              : t('LandingPage.migration.stepPost')
      : '';

  const logs: string[] = [];
  if (step === 'simulating') {
    if (progress >= 10) logs.push(t('LandingPage.migration.log1'));
    if (progress >= 35) logs.push(t('LandingPage.migration.log2'));
    if (progress >= 55) logs.push(t('LandingPage.migration.log3'));
    if (progress >= 75) logs.push(t('LandingPage.migration.log4'));
    if (progress >= 95) logs.push(t('LandingPage.migration.log5'));
  }

  const handleStartSimulation = () => {
    setProgress(0);
    setStep('simulating');
  };

  const handleReset = () => {
    setStep('setup');
    setProgress(0);
  };

  return (
    <section className="border-t border-slate-200/50 bg-white/20 px-8 py-20 backdrop-blur-sm sm:px-16 lg:px-24 dark:border-slate-800/50 dark:bg-slate-900/20">
      <div className="mx-auto max-w-5xl">
        <div className="mb-16 text-center">
          <div className="mb-4 inline-flex items-center gap-2 rounded-full border border-emerald-500/30 bg-emerald-500/10 px-3 py-1 text-xs font-semibold text-emerald-600 backdrop-blur-md dark:text-emerald-400">
            <Upload size={12} />
            {t('LandingPage.migration.title')}
          </div>
          <h2 className="mb-4 text-3xl font-extrabold tracking-tight text-slate-900 dark:text-white md:text-4xl">
            {t('LandingPage.migration.subtitle')}
          </h2>
          <p className="mx-auto max-w-2xl text-lg text-slate-600 dark:text-slate-400">
            {t('LandingPage.migration.desc')}
          </p>
        </div>

        <div className="grid grid-cols-1 gap-12 lg:grid-cols-12 items-start">
          <div className="lg:col-span-5 space-y-6">
            <div>
              <label className="mb-3 block text-sm font-bold text-slate-900 dark:text-slate-100">
                {t('LandingPage.migration.selectErp')}
              </label>
              <div className="flex flex-wrap gap-2">
                {erps.map((item) => (
                  <button
                    key={item.id}
                    disabled={step === 'simulating'}
                    onClick={() => setErp(item.id)}
                    className={`inline-flex items-center gap-2 rounded-xl border px-4 py-2.5 text-xs font-semibold transition-all duration-300 ${
                      erp === item.id
                        ? 'border-indigo-500 bg-indigo-500/10 text-indigo-600 dark:border-indigo-400 dark:bg-indigo-400/20 dark:text-indigo-400'
                        : 'border-slate-200 bg-white/50 text-slate-600 hover:border-slate-300 dark:border-slate-800 dark:bg-slate-900/50 dark:text-slate-400'
                    }`}
                  >
                    {item.id === 'excel' ? <FileSpreadsheet size={14} /> : <Database size={14} />}
                    {item.label}
                  </button>
                ))}
              </div>
            </div>

            <div>
              <label className="mb-3 block text-sm font-bold text-slate-900 dark:text-slate-100">
                {t('LandingPage.migration.selectEntity')}
              </label>
              <div className="flex flex-col gap-2">
                {entities.map((item) => (
                  <button
                    key={item.id}
                    disabled={step === 'simulating'}
                    onClick={() => setEntity(item.id)}
                    className={`flex items-center justify-between rounded-2xl border p-4 text-left transition-all duration-300 ${
                      entity === item.id
                        ? 'border-indigo-500 bg-indigo-500/5 text-slate-900 dark:border-indigo-400 dark:bg-indigo-400/10 dark:text-white'
                        : 'border-slate-200 bg-white/30 text-slate-600 hover:border-slate-300 dark:border-slate-800 dark:bg-slate-900/30 dark:text-slate-400'
                    }`}
                  >
                    <span className="text-sm font-semibold">{item.label}</span>
                    <ArrowRight
                      size={16}
                      className={entity === item.id ? 'text-indigo-500' : 'text-slate-400'}
                    />
                  </button>
                ))}
              </div>
            </div>
          </div>

          <div className="lg:col-span-7">
            <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-xl dark:border-slate-800/80 dark:bg-[#0f1524]/65">
              {step === 'setup' && (
                <div className="space-y-6">
                  <div className="flex items-center justify-between border-b border-slate-100 pb-4 dark:border-slate-800">
                    <span className="text-sm font-bold text-slate-900 dark:text-white">
                      {t('LandingPage.migration.mappingPreview')}
                    </span>
                    <span className="rounded-full bg-slate-100 px-2.5 py-0.5 text-[10px] font-bold uppercase tracking-wider text-slate-500 dark:bg-slate-800 dark:text-slate-400">
                      {erp.toUpperCase()}
                    </span>
                  </div>

                  <div className="overflow-x-auto">
                    <table className="w-full text-left text-xs">
                      <thead>
                        <tr className="border-b border-slate-100 text-slate-400 dark:border-slate-800">
                          <th className="pb-3 font-semibold">
                            {t('LandingPage.migration.legacyField')}
                          </th>
                          <th className="pb-3 font-semibold">
                            {t('LandingPage.migration.corealignField')}
                          </th>
                          <th className="pb-3 text-right font-semibold">
                            {t('LandingPage.migration.status')}
                          </th>
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-slate-50 dark:divide-slate-800/40">
                        {getMapping().map((map, idx) => (
                          <tr key={idx} className="text-slate-700 dark:text-slate-300">
                            <td className="py-3 font-mono">{map.source}</td>
                            <td className="py-3 font-semibold">{map.target}</td>
                            <td className="py-3 text-right">
                              <span className="rounded-full bg-emerald-500/10 px-2 py-0.5 text-[9px] font-semibold text-emerald-600 dark:bg-emerald-500/20 dark:text-emerald-400">
                                {t('LandingPage.migration.autoMapped')}
                              </span>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>

                  <button
                    onClick={handleStartSimulation}
                    className="inline-flex w-full items-center justify-center gap-2 rounded-2xl bg-indigo-600 py-4 font-bold text-white shadow-lg shadow-indigo-500/25 transition-all hover:bg-indigo-700 hover:shadow-indigo-500/35"
                  >
                    <Play size={16} />
                    {t('LandingPage.migration.btnStart')}
                  </button>
                </div>
              )}

              {step === 'simulating' && (
                <div className="flex flex-col items-center justify-center py-8 text-center">
                  <div className="relative mb-6 flex h-16 w-16 items-center justify-center">
                    <RefreshCw className="h-10 w-10 animate-spin text-indigo-500" />
                  </div>
                  <h4 className="mb-2 text-lg font-bold text-slate-900 dark:text-white">
                    {t('LandingPage.migration.btnSimulating')}
                  </h4>
                  <p className="mb-6 text-sm text-slate-500 dark:text-slate-400">{simStepText}</p>
                  <div className="h-2 w-full max-w-md overflow-hidden rounded-full bg-slate-100 dark:bg-slate-800">
                    <div
                      className="h-full bg-indigo-600 transition-all duration-150 ease-out"
                      style={{ width: `${progress}%` }}
                    />
                  </div>
                  <span className="mt-2 text-xs font-bold text-indigo-600 dark:text-indigo-400">
                    %{progress}
                  </span>
                  <div className="mt-6 w-full max-w-md rounded-2xl border border-slate-200 bg-slate-950 p-4 font-mono text-[9px] text-indigo-450 dark:border-slate-800 text-left space-y-1 overflow-y-auto max-h-[120px] shadow-inner">
                    {logs.map((log, idx) => (
                      <div
                        key={idx}
                        className={
                          log.includes('[SUCCESS]') || log.includes('[BAŞARI]')
                            ? 'text-emerald-400'
                            : log.includes('[WARN]') || log.includes('[UYARI]')
                              ? 'text-amber-400'
                              : ''
                        }
                      >
                        {log}
                      </div>
                    ))}
                  </div>
                </div>
              )}

              {step === 'success' && (
                <div className="space-y-6 py-4">
                  <div className="flex flex-col items-center text-center">
                    <div className="mb-4 rounded-full bg-emerald-500/10 p-3 text-emerald-600 dark:bg-emerald-500/20 dark:text-emerald-400">
                      <CheckCircle2 size={40} />
                    </div>
                    <h3 className="mb-1 text-xl font-bold text-slate-900 dark:text-white">
                      {t('LandingPage.migration.successTitle')}
                    </h3>
                    <p className="text-sm text-slate-500 dark:text-slate-400">
                      {t('LandingPage.migration.successText')}
                    </p>
                  </div>

                  <div className="rounded-2xl bg-slate-50 p-4 text-xs space-y-2.5 dark:bg-slate-900/60 font-medium">
                    <div className="flex justify-between">
                      <span className="text-slate-500">{t('LandingPage.migration.session')}</span>
                      <span className="font-mono font-bold text-slate-900 dark:text-white">
                        {sessionId}
                      </span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-slate-500">{t('LandingPage.migration.imported')}</span>
                      <span className="font-bold text-emerald-600 dark:text-emerald-400">
                        +{importedCount}
                      </span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-slate-500">{t('LandingPage.migration.skipped')}</span>
                      <span className="font-bold text-slate-600 dark:text-slate-400">0</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-slate-500">{t('LandingPage.migration.errors')}</span>
                      <span className="font-bold text-slate-950 dark:text-slate-100">0</span>
                    </div>
                  </div>

                  <button
                    onClick={handleReset}
                    className="inline-flex w-full items-center justify-center gap-2 rounded-2xl border border-slate-200 py-3 text-xs font-bold text-slate-600 hover:bg-slate-50 dark:border-slate-800 dark:text-slate-400 dark:hover:bg-slate-900/60"
                  >
                    <RefreshCw size={14} />
                    Yeni Simülasyon
                  </button>
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
    </section>
  );
};
