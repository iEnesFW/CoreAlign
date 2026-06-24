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
  ShieldCheck,
  Undo2,
  ScanSearch,
  Layers,
  Lock,
  Building2,
} from 'lucide-react';
import { Section, SectionHeader } from './Section';

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

  const pipelineStages = [
    {
      icon: ScanSearch,
      title: t('LandingPage.migration.stageReadTitle', { defaultValue: 'Oku & Çöz' }),
      desc: t('LandingPage.migration.stageReadDesc', {
        defaultValue: 'Excel, CSV veya ERP dışa aktarımındaki başlıklar otomatik tanınır.',
      }),
    },
    {
      icon: Layers,
      title: t('LandingPage.migration.stageMapTitle', { defaultValue: 'Eşleştir & Doğrula' }),
      desc: t('LandingPage.migration.stageMapDesc', {
        defaultValue: 'Alanlar CoreAlign şemasına bağlanır, biçim ve zorunluluk kuralları işler.',
      }),
    },
    {
      icon: Building2,
      title: t('LandingPage.migration.stageLoadTitle', { defaultValue: 'Yükle & Muhasebeleştir' }),
      desc: t('LandingPage.migration.stageLoadDesc', {
        defaultValue: 'Kayıtlar tek bir işlem içinde, geri alınabilir biçimde aktarılır.',
      }),
    },
  ];

  const assurances = [
    {
      icon: ShieldCheck,
      title: t('LandingPage.migration.assureLossTitle', { defaultValue: 'Sıfır veri kaybı' }),
      desc: t('LandingPage.migration.assureLossDesc', {
        defaultValue:
          'Her satır kaynaktan hedefe satır satır eşleştirilir; aktarım öncesi ve sonrası kayıt sayıları otomatik mutabakata tabidir.',
      }),
      color: 'bg-success-500/10 text-success-600 dark:bg-success-500/20 dark:text-success-400',
    },
    {
      icon: Lock,
      title: t('LandingPage.migration.assureEncryptTitle', { defaultValue: 'Uçtan uca şifreli' }),
      desc: t('LandingPage.migration.assureEncryptDesc', {
        defaultValue:
          'Finansal alanlar aktarım sırasında AES-256 ile korunur; verileriniz yalnızca kendi kiracı (tenant) alanınıza yazılır.',
      }),
      color: 'bg-primary-500/10 text-primary-600 dark:bg-primary-500/20 dark:text-primary-400',
    },
    {
      icon: Undo2,
      title: t('LandingPage.migration.assureRollbackTitle', { defaultValue: 'Geri alınabilir' }),
      desc: t('LandingPage.migration.assureRollbackDesc', {
        defaultValue:
          'Aktarım önce yalıtılmış bir oturumda denenir; sonuç beklentinizi karşılamazsa tek tıkla geri alınır.',
      }),
      color: 'bg-accent-500/10 text-accent-600 dark:bg-accent-500/20 dark:text-accent-300',
    },
  ];

  const motionState: 'paused' | 'running' = step === 'setup' ? 'paused' : 'running';

  return (
    <Section>
      <SectionHeader
        eyebrow={
          <>
            <Upload size={12} /> {t('LandingPage.migration.title')}
          </>
        }
        title={t('LandingPage.migration.subtitle')}
        subtitle={t('LandingPage.migration.desc')}
      />

      <div className="mb-12 animate-fade-up">
        <h3 className="mb-2 text-lg font-bold text-slate-900 dark:text-white">
          {t('LandingPage.migration.pipelineTitle', {
            defaultValue: 'Verileriniz CoreAlign’a giderken ne oluyor?',
          })}
        </h3>
        <p className="mb-8 max-w-2xl text-sm text-slate-600 dark:text-slate-400">
          {t('LandingPage.migration.pipelineDesc', {
            defaultValue:
              'Eski tablolardan canlı sisteme yolculuğu izleyin: her kayıt okunur, eşleştirilir, doğrulanır ve güvenle yüklenir — siz tek bir veriyi bile elle taşımadan.',
          })}
        </p>
        {renderPipeline(t, motionState, progress, step)}
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
                      ? 'border-primary-500 bg-primary-500/10 text-primary-600 dark:border-primary-400 dark:bg-primary-400/20 dark:text-primary-400'
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
                      ? 'border-primary-500 bg-primary-500/5 text-slate-900 dark:border-primary-400 dark:bg-primary-400/10 dark:text-white'
                      : 'border-slate-200 bg-white/30 text-slate-600 hover:border-slate-300 dark:border-slate-800 dark:bg-slate-900/30 dark:text-slate-400'
                  }`}
                >
                  <span className="text-sm font-semibold">{item.label}</span>
                  <ArrowRight
                    size={16}
                    className={entity === item.id ? 'text-primary-500' : 'text-slate-400'}
                  />
                </button>
              ))}
            </div>
          </div>

          <div className="rounded-2xl border border-slate-200 bg-white/40 p-5 dark:border-slate-800 dark:bg-slate-900/40">
            <h3 className="mb-3 text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400">
              {t('LandingPage.migration.howTitle', { defaultValue: 'Üç adımda nasıl çalışır?' })}
            </h3>
            <ol className="space-y-3 ca-stagger">
              {pipelineStages.map((stage, idx) => {
                const Icon = stage.icon;
                return (
                  <li key={idx} className="flex gap-3">
                    <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-lg bg-primary-500/10 text-primary-600 dark:bg-primary-500/20 dark:text-primary-400">
                      <Icon size={15} />
                    </span>
                    <div>
                      <p className="text-sm font-semibold text-slate-900 dark:text-white">
                        {stage.title}
                      </p>
                      <p className="text-xs leading-relaxed text-slate-600 dark:text-slate-400">
                        {stage.desc}
                      </p>
                    </div>
                  </li>
                );
              })}
            </ol>
          </div>
        </div>

        <div className="lg:col-span-7">
          <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-xl dark:border-slate-800/80 dark:bg-surface-deep/65">
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
                            <span className="rounded-full bg-success-500/10 px-2 py-0.5 text-[9px] font-semibold text-success-600 dark:bg-success-500/20 dark:text-success-400">
                              {t('LandingPage.migration.autoMapped')}
                            </span>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>

                <p className="text-xs leading-relaxed text-slate-500 dark:text-slate-400">
                  {t('LandingPage.migration.mappingHint', {
                    defaultValue:
                      'Bu eşleştirme yalnızca bir önizlemedir. Gerçek aktarımda hiçbir veri üzerine yazılmaz; aktarım yalıtılmış bir oturumda çalışır ve onaylamadan önce geri alabilirsiniz.',
                  })}
                </p>

                <button
                  onClick={handleStartSimulation}
                  className="inline-flex w-full items-center justify-center gap-2 rounded-2xl bg-primary-600 py-4 font-bold text-white shadow-lg shadow-primary-500/25 transition-all hover:bg-primary-700 hover:shadow-primary-500/35"
                >
                  <Play size={16} />
                  {t('LandingPage.migration.btnStart')}
                </button>
              </div>
            )}

            {step === 'simulating' && (
              <div className="flex flex-col items-center justify-center py-8 text-center">
                <div className="relative mb-6 flex h-16 w-16 items-center justify-center">
                  <RefreshCw className="h-10 w-10 animate-spin text-primary-500" />
                </div>
                <h3 className="mb-2 text-lg font-bold text-slate-900 dark:text-white">
                  {t('LandingPage.migration.btnSimulating')}
                </h3>
                <p className="mb-6 text-sm text-slate-500 dark:text-slate-400">{simStepText}</p>
                <div
                  className="h-2 w-full max-w-md overflow-hidden rounded-full bg-slate-100 dark:bg-slate-800"
                  role="progressbar"
                  aria-valuemin={0}
                  aria-valuemax={100}
                  aria-valuenow={progress}
                  aria-label={t('LandingPage.migration.btnSimulating')}
                >
                  <div
                    className="h-full bg-primary-600 transition-all duration-150 ease-out"
                    style={{ width: `${progress}%` }}
                  />
                </div>
                <span className="mt-2 text-xs font-bold text-primary-600 dark:text-primary-400">
                  %{progress}
                </span>
                <div className="mt-6 w-full max-w-md rounded-2xl border border-slate-200 bg-slate-950 p-4 font-mono text-[9px] text-info-300 dark:border-slate-800 text-left space-y-1 overflow-y-auto max-h-[120px] shadow-inner">
                  {logs.map((log, idx) => (
                    <div
                      key={idx}
                      className={
                        log.includes('[SUCCESS]') || log.includes('[BAŞARI]')
                          ? 'text-success-400'
                          : log.includes('[WARN]') || log.includes('[UYARI]')
                            ? 'text-warning-400'
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
              <div className="space-y-6 py-4 animate-zoom-in">
                <div className="flex flex-col items-center text-center">
                  <div className="mb-4 rounded-full bg-success-500/10 p-3 text-success-600 dark:bg-success-500/20 dark:text-success-400">
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
                    <span className="font-bold text-success-600 dark:text-success-400">
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

                <p className="rounded-xl border border-success-500/20 bg-success-500/5 px-3 py-2.5 text-xs leading-relaxed text-success-700 dark:text-success-300">
                  {t('LandingPage.migration.successReassure', {
                    defaultValue:
                      'Borç/alacak dengesi doğrulandı ve her kayıt denetim günlüğüne işlendi. Sonuçtan memnun kalmazsanız bu oturumu tek tıkla geri alabilirsiniz.',
                  })}
                </p>

                <button
                  onClick={handleReset}
                  className="inline-flex w-full items-center justify-center gap-2 rounded-2xl border border-slate-200 py-3 text-xs font-bold text-slate-600 hover:bg-slate-50 dark:border-slate-800 dark:text-slate-400 dark:hover:bg-slate-900/60"
                >
                  <RefreshCw size={14} />
                  {t('LandingPage.migration.btnReset', { defaultValue: 'Yeni Simülasyon' })}
                </button>
              </div>
            )}
          </div>
        </div>
      </div>

      <div className="mt-12 grid grid-cols-1 gap-6 md:grid-cols-3 ca-stagger">
        {assurances.map((item, idx) => {
          const Icon = item.icon;
          return (
            <div
              key={idx}
              className="flex flex-col rounded-3xl border border-slate-200 bg-white/40 p-6 shadow-sm backdrop-blur-sm transition-all duration-300 hover:border-primary-500/30 dark:border-slate-800 dark:bg-slate-900/40"
            >
              <div
                className={`mb-4 inline-flex h-10 w-10 shrink-0 items-center justify-center rounded-2xl ${item.color}`}
              >
                <Icon size={18} />
              </div>
              <h3 className="mb-2 text-sm font-bold text-slate-900 dark:text-white">
                {item.title}
              </h3>
              <p className="text-xs leading-relaxed text-slate-600 dark:text-slate-400">
                {item.desc}
              </p>
            </div>
          );
        })}
      </div>
    </Section>
  );
};

const renderPipeline = (
  t: (key: string, opts?: { defaultValue: string }) => string,
  motionState: 'paused' | 'running',
  progress: number,
  step: 'setup' | 'simulating' | 'success',
) => {
  const dotDur = motionState === 'running' ? '1.4s' : '3.2s';
  const trackActive = step !== 'setup';

  return (
    <figure className="mx-auto max-w-3xl">
      <svg
        viewBox="0 0 600 150"
        className="w-full"
        role="img"
        aria-label={t('LandingPage.migration.pipelineAria', {
          defaultValue:
            'Eski sistemden CoreAlign veritabanına uzanan, oku-eşleştir-yükle adımlarından geçen veri akışı şeması',
        })}
      >
        <defs>
          <linearGradient id="ca-mig-flow" x1="0" y1="0" x2="1" y2="0">
            <stop offset="0%" stopColor="var(--color-slate-400, #94a3b8)" />
            <stop offset="55%" stopColor="var(--color-primary-500, #6366f1)" />
            <stop offset="100%" stopColor="var(--color-success-500, #10b981)" />
          </linearGradient>
        </defs>

        <path
          id="ca-mig-path"
          d="M 70 75 H 530"
          fill="none"
          stroke="url(#ca-mig-flow)"
          strokeWidth={trackActive ? 3 : 2}
          strokeLinecap="round"
          strokeDasharray="4 6"
          className="transition-all duration-300"
          opacity={trackActive ? 0.9 : 0.45}
          aria-hidden="true"
        />

        <g aria-hidden="true">
          {[0, 1, 2, 3].map((i) => (
            <circle key={i} r={trackActive ? 4 : 3} fill="var(--color-primary-500, #6366f1)">
              <animateMotion
                dur={dotDur}
                begin={`${i * (motionState === 'running' ? 0.35 : 0.8)}s`}
                repeatCount="indefinite"
                keyPoints="0;1"
                keyTimes="0;1"
                path="M 70 75 H 530"
              />
              <animate
                attributeName="opacity"
                values="0;1;1;0"
                keyTimes="0;0.1;0.9;1"
                dur={dotDur}
                begin={`${i * (motionState === 'running' ? 0.35 : 0.8)}s`}
                repeatCount="indefinite"
              />
            </circle>
          ))}
        </g>

        <g>
          <rect
            x={28}
            y={48}
            width={84}
            height={54}
            rx={12}
            className="fill-white stroke-slate-200 dark:fill-slate-900 dark:stroke-slate-700"
            strokeWidth={1.5}
          />
          <text
            x={70}
            y={66}
            textAnchor="middle"
            className="fill-slate-500 dark:fill-slate-400"
            fontSize={9}
            fontWeight={700}
          >
            {t('LandingPage.migration.nodeLegacy', { defaultValue: 'ESKİ SİSTEM' })}
          </text>
          <text
            x={70}
            y={84}
            textAnchor="middle"
            className="fill-slate-900 dark:fill-white"
            fontSize={11}
            fontWeight={700}
          >
            Excel · ERP
          </text>
        </g>

        {[
          { cx: 230, label: t('LandingPage.migration.nodeRead', { defaultValue: 'OKU' }), min: 0 },
          {
            cx: 300,
            label: t('LandingPage.migration.nodeMap', { defaultValue: 'EŞLEŞTİR' }),
            min: 40,
          },
          {
            cx: 370,
            label: t('LandingPage.migration.nodeValidate', { defaultValue: 'DOĞRULA' }),
            min: 70,
          },
        ].map((node, idx) => {
          const reached = step === 'success' || (step === 'simulating' && progress >= node.min);
          return (
            <g key={idx}>
              <circle
                cx={node.cx}
                cy={75}
                r={14}
                className={
                  reached
                    ? 'fill-primary-500/15 stroke-primary-500'
                    : 'fill-white stroke-slate-200 dark:fill-slate-900 dark:stroke-slate-700'
                }
                strokeWidth={1.5}
              >
                {reached && step === 'simulating' && (
                  <animate
                    attributeName="r"
                    values="13;15;13"
                    dur="1.2s"
                    repeatCount="indefinite"
                  />
                )}
              </circle>
              <text
                x={node.cx}
                y={110}
                textAnchor="middle"
                className={
                  reached
                    ? 'fill-primary-600 dark:fill-primary-400'
                    : 'fill-slate-400 dark:fill-slate-500'
                }
                fontSize={8}
                fontWeight={700}
              >
                {node.label}
              </text>
            </g>
          );
        })}

        <g>
          <rect
            x={488}
            y={48}
            width={84}
            height={54}
            rx={12}
            className={
              step === 'success'
                ? 'fill-success-500/10 stroke-success-500'
                : 'fill-white stroke-slate-200 dark:fill-slate-900 dark:stroke-slate-700'
            }
            strokeWidth={1.5}
          />
          <text
            x={530}
            y={66}
            textAnchor="middle"
            className="fill-success-600 dark:fill-success-400"
            fontSize={9}
            fontWeight={700}
          >
            CoreAlign
          </text>
          <text
            x={530}
            y={84}
            textAnchor="middle"
            className="fill-slate-900 dark:fill-white"
            fontSize={11}
            fontWeight={700}
          >
            {t('LandingPage.migration.nodeVault', { defaultValue: 'Canlı Veri' })}
          </text>
        </g>
      </svg>
      <figcaption className="sr-only">
        {t('LandingPage.migration.pipelineCaption', {
          defaultValue: 'Eski sistemden CoreAlign canlı veritabanına veri akış şeması.',
        })}
      </figcaption>
    </figure>
  );
};
