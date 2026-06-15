import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Layers,
  Network,
  Coins,
  DraftingCompass,
  ShieldCheck,
  Package,
  Sliders,
  Play,
  CheckCircle,
  Activity,
} from 'lucide-react';

export const SolutionsSection = () => {
  const { t } = useTranslation();
  const [activeTab, setActiveTab] = useState<'cad' | 'b2b' | 'mrp' | 'finance'>('cad');
  const [isSimulating, setIsSimulating] = useState(false);

  const [width, setWidth] = useState(3200);
  const [height, setHeight] = useState(2200);
  const [glassType, setGlassType] = useState('double');
  const [profile, setProfile] = useState('thermal');

  const [b2bOrderVal, setB2bOrderVal] = useState(25000);
  const [b2bDealerTier, setB2bDealerTier] = useState('gold');
  const [b2bTerms, setB2bTerms] = useState('net30');
  const [b2bRisk, setB2bRisk] = useState('medium');

  const [mrpWeight, setMrpWeight] = useState(3500);
  const [mrpThickness, setMrpThickness] = useState(6);

  const [finInvoiceVal, setFinInvoiceVal] = useState(15000);
  const [finVatRate, setFinVatRate] = useState(20);

  const solutions = [
    {
      icon: <Layers className="h-6 w-6 text-indigo-500" />,
      title: t('LandingPage.solutions.mrpTitle'),
      desc: t('LandingPage.solutions.mrpDesc'),
    },
    {
      icon: <Network className="h-6 w-6 text-emerald-500" />,
      title: t('LandingPage.solutions.b2bTitle'),
      desc: t('LandingPage.solutions.b2bDesc'),
    },
    {
      icon: <Coins className="h-6 w-6 text-amber-500" />,
      title: t('LandingPage.solutions.financeTitle'),
      desc: t('LandingPage.solutions.financeDesc'),
    },
    {
      icon: <DraftingCompass className="h-6 w-6 text-blue-500" />,
      title: t('LandingPage.solutions.glassTitle'),
      desc: t('LandingPage.solutions.glassDesc'),
    },
    {
      icon: <ShieldCheck className="h-6 w-6 text-rose-500" />,
      title: t('LandingPage.solutions.warrantyTitle'),
      desc: t('LandingPage.solutions.warrantyDesc'),
    },
    {
      icon: <Package className="h-6 w-6 text-violet-500" />,
      title: t('LandingPage.solutions.inventoryTitle'),
      desc: t('LandingPage.solutions.inventoryDesc'),
    },
  ];

  const handleSimulate = () => {
    setIsSimulating(true);
    setTimeout(() => {
      setIsSimulating(false);
    }, 600);
  };

  const area = (width * height) / 1000000;
  const panelCount = Math.ceil(width / 800);
  const panelWidth = Math.round(width / panelCount);
  const perimeter = (2 * (width + height)) / 1000;
  const glassPriceFactor = glassType === 'double' ? 950 : glassType === 'laminated' ? 1200 : 450;
  const profilePriceFactor = profile === 'thermal' ? 600 : profile === 'slim' ? 450 : 300;
  const mfgCost = Math.round(area * glassPriceFactor + perimeter * profilePriceFactor);
  const dealerPrice = Math.round(mfgCost * 1.35);
  const wasteSaved = (area * 0.138).toFixed(2);

  const b2bDiscount = b2bDealerTier === 'gold' ? 0.1 : b2bDealerTier === 'premium' ? 0.2 : 0;
  const b2bNetOrderVal = b2bOrderVal * (1 - b2bDiscount);
  let b2bDownpaymentPct = 0;
  if (b2bTerms === 'advance') {
    b2bDownpaymentPct = 100;
  } else if (b2bTerms === 'net30') {
    b2bDownpaymentPct = b2bRisk === 'low' ? 0 : b2bRisk === 'medium' ? 20 : 50;
  } else {
    b2bDownpaymentPct = b2bRisk === 'low' ? 20 : b2bRisk === 'medium' ? 40 : 80;
  }
  const b2bDownpaymentAmt = Math.round((b2bNetOrderVal * b2bDownpaymentPct) / 100);
  let b2bStatus = 'approved';
  if (b2bRisk === 'high' && b2bNetOrderVal > 40000) {
    b2bStatus = 'blocked';
  } else if (b2bRisk === 'medium' && b2bNetOrderVal > 60000) {
    b2bStatus = 'warning';
  }
  const b2bLotNumber = `LOT-${new Date().getFullYear()}-${Math.floor(1000 + (b2bNetOrderVal % 9000))}`;

  const mrpCycles = Math.ceil(mrpWeight / 1200);
  const mrpLoadRatio = Math.min(100, Math.round((mrpWeight / (mrpCycles * 1200)) * 100));
  const mrpEnergyCost = mrpCycles * (mrpThickness * 140 + 180);

  const finVatAmt = Math.round(finInvoiceVal * (finVatRate / 100));
  const finReceivable = finInvoiceVal + finVatAmt;

  return (
    <section className="px-6 py-12 md:px-12 lg:px-20">
      <div className="mx-auto max-w-6xl">
        <div className="mb-12 text-center">
          <h2 className="mb-4 text-3xl font-extrabold tracking-tight text-slate-900 dark:text-white md:text-4xl">
            {t('LandingPage.solutions.title')}
          </h2>
          <p className="text-lg text-slate-600 dark:text-slate-400">
            {t('LandingPage.solutions.subtitle')}
          </p>
        </div>

        <div className="grid grid-cols-1 gap-6 md:grid-cols-2 lg:grid-cols-3 mb-16">
          {solutions.map((sol, index) => (
            <div
              key={index}
              className="group relative overflow-hidden rounded-2xl border border-slate-200/60 bg-white/50 p-6 shadow-sm transition-all duration-300 hover:border-indigo-500/20 hover:shadow-md dark:border-slate-800/60 dark:bg-[#0f1524]/60"
            >
              <div className="absolute -right-4 -top-4 h-24 w-24 rounded-full bg-gradient-to-br from-indigo-500/5 to-purple-500/5 transition-transform duration-500 group-hover:scale-150"></div>
              <div className="relative z-10 mb-4 inline-flex rounded-xl bg-slate-100 p-3 dark:bg-slate-800">
                {sol.icon}
              </div>
              <h3 className="relative z-10 mb-2 text-lg font-bold text-slate-900 dark:text-slate-100">
                {sol.title}
              </h3>
              <p className="relative z-10 text-sm leading-relaxed text-slate-600 dark:text-slate-400">
                {sol.desc}
              </p>
            </div>
          ))}
        </div>

        <div className="rounded-3xl border border-slate-200/60 bg-white/70 p-6 shadow-xl backdrop-blur-xl dark:border-slate-800/60 dark:bg-[#0f1524]/80 lg:p-10">
          <div className="mb-8 flex flex-col xl:flex-row xl:items-center justify-between gap-6 border-b border-slate-200/50 pb-6 dark:border-slate-800/50">
            <div>
              <div className="inline-flex items-center gap-2 rounded-full bg-indigo-500/10 px-3 py-1 text-xs font-semibold text-indigo-600 dark:bg-indigo-500/20 dark:text-indigo-400">
                <Activity size={14} />
                CoreAlign Live Engine Sandbox
              </div>
              <h3 className="mt-2 text-2xl font-bold text-slate-900 dark:text-white">
                {t('LandingPage.solutions.simulator.title')}
              </h3>
              <p className="text-sm text-slate-500 dark:text-slate-400">
                {t('LandingPage.solutions.simulator.subtitle')}
              </p>
            </div>

            <div className="flex flex-wrap gap-2">
              <button
                onClick={() => {
                  setActiveTab('cad');
                  handleSimulate();
                }}
                className={`flex items-center gap-2 rounded-xl px-4 py-2.5 text-xs font-bold transition-all duration-300 ${
                  activeTab === 'cad'
                    ? 'bg-indigo-600 text-white shadow-md'
                    : 'bg-slate-100 text-slate-600 hover:bg-slate-200 dark:bg-slate-800/50 dark:text-slate-400 dark:hover:bg-slate-800'
                }`}
              >
                <DraftingCompass size={14} />
                {t('LandingPage.solutions.simulator.tabCAD')}
              </button>
              <button
                onClick={() => {
                  setActiveTab('b2b');
                  handleSimulate();
                }}
                className={`flex items-center gap-2 rounded-xl px-4 py-2.5 text-xs font-bold transition-all duration-300 ${
                  activeTab === 'b2b'
                    ? 'bg-indigo-600 text-white shadow-md'
                    : 'bg-slate-100 text-slate-600 hover:bg-slate-200 dark:bg-slate-800/50 dark:text-slate-400 dark:hover:bg-slate-800'
                }`}
              >
                <Network size={14} />
                {t('LandingPage.solutions.simulator.tabB2B')}
              </button>
              <button
                onClick={() => {
                  setActiveTab('mrp');
                  handleSimulate();
                }}
                className={`flex items-center gap-2 rounded-xl px-4 py-2.5 text-xs font-bold transition-all duration-300 ${
                  activeTab === 'mrp'
                    ? 'bg-indigo-600 text-white shadow-md'
                    : 'bg-slate-100 text-slate-600 hover:bg-slate-200 dark:bg-slate-800/50 dark:text-slate-400 dark:hover:bg-slate-800'
                }`}
              >
                <Layers size={14} />
                {t('LandingPage.solutions.simulator.tabMRP')}
              </button>
              <button
                onClick={() => {
                  setActiveTab('finance');
                  handleSimulate();
                }}
                className={`flex items-center gap-2 rounded-xl px-4 py-2.5 text-xs font-bold transition-all duration-300 ${
                  activeTab === 'finance'
                    ? 'bg-indigo-600 text-white shadow-md'
                    : 'bg-slate-100 text-slate-600 hover:bg-slate-200 dark:bg-slate-800/50 dark:text-slate-400 dark:hover:bg-slate-800'
                }`}
              >
                <Coins size={14} />
                {t('LandingPage.solutions.simulator.tabFinance')}
              </button>
            </div>
          </div>

          <div className="grid grid-cols-1 gap-8 lg:grid-cols-12">
            <div className="space-y-6 lg:col-span-5">
              <div className="space-y-4 rounded-2xl border border-slate-200/40 bg-slate-50/50 p-5 dark:border-slate-800/40 dark:bg-slate-900/30">
                <div className="flex items-center gap-2 font-semibold text-slate-900 dark:text-white">
                  <Sliders size={16} />
                  <span>Parametreler</span>
                </div>

                {activeTab === 'cad' && (
                  <>
                    <div>
                      <div className="flex justify-between text-xs text-slate-500 dark:text-slate-400 mb-1">
                        <span>{t('LandingPage.solutions.simulator.width')}</span>
                        <span className="font-bold text-indigo-600 dark:text-indigo-400">
                          {width} mm
                        </span>
                      </div>
                      <input
                        type="range"
                        min="1000"
                        max="6000"
                        step="100"
                        value={width}
                        onChange={(e) => {
                          setWidth(Number(e.target.value));
                          handleSimulate();
                        }}
                        className="w-full accent-indigo-600"
                      />
                    </div>
                    <div>
                      <div className="flex justify-between text-xs text-slate-500 dark:text-slate-400 mb-1">
                        <span>{t('LandingPage.solutions.simulator.height')}</span>
                        <span className="font-bold text-indigo-600 dark:text-indigo-400">
                          {height} mm
                        </span>
                      </div>
                      <input
                        type="range"
                        min="1000"
                        max="3000"
                        step="100"
                        value={height}
                        onChange={(e) => {
                          setHeight(Number(e.target.value));
                          handleSimulate();
                        }}
                        className="w-full accent-indigo-600"
                      />
                    </div>
                    <div>
                      <label className="block text-xs text-slate-500 dark:text-slate-400 mb-1">
                        {t('LandingPage.solutions.simulator.glassType')}
                      </label>
                      <select
                        value={glassType}
                        onChange={(e) => {
                          setGlassType(e.target.value);
                          handleSimulate();
                        }}
                        className="w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm outline-none transition focus:border-indigo-500 dark:border-slate-700 dark:bg-slate-900 dark:focus:border-indigo-400"
                      >
                        <option value="standard">
                          {t('LandingPage.solutions.simulator.glassStandard')}
                        </option>
                        <option value="double">
                          {t('LandingPage.solutions.simulator.glassDouble')}
                        </option>
                        <option value="laminated">
                          {t('LandingPage.solutions.simulator.glassLaminated')}
                        </option>
                      </select>
                    </div>
                    <div>
                      <label className="block text-xs text-slate-500 dark:text-slate-400 mb-1">
                        {t('LandingPage.solutions.simulator.profile')}
                      </label>
                      <select
                        value={profile}
                        onChange={(e) => {
                          setProfile(e.target.value);
                          handleSimulate();
                        }}
                        className="w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm outline-none transition focus:border-indigo-500 dark:border-slate-700 dark:bg-slate-900 dark:focus:border-indigo-400"
                      >
                        <option value="standard">
                          {t('LandingPage.solutions.simulator.profileStandard')}
                        </option>
                        <option value="thermal">
                          {t('LandingPage.solutions.simulator.profileThermal')}
                        </option>
                        <option value="slim">
                          {t('LandingPage.solutions.simulator.profileSlim')}
                        </option>
                      </select>
                    </div>
                  </>
                )}

                {activeTab === 'b2b' && (
                  <>
                    <div>
                      <div className="flex justify-between text-xs text-slate-500 dark:text-slate-400 mb-1">
                        <span>{t('LandingPage.solutions.simulator.b2bOrderVal')}</span>
                        <span className="font-bold text-indigo-600 dark:text-indigo-400">
                          {b2bOrderVal.toLocaleString()} €
                        </span>
                      </div>
                      <input
                        type="range"
                        min="5000"
                        max="100000"
                        step="5000"
                        value={b2bOrderVal}
                        onChange={(e) => {
                          setB2bOrderVal(Number(e.target.value));
                          handleSimulate();
                        }}
                        className="w-full accent-indigo-600"
                      />
                    </div>
                    <div>
                      <label className="block text-xs text-slate-500 dark:text-slate-400 mb-1">
                        {t('LandingPage.solutions.simulator.b2bDealerTier')}
                      </label>
                      <select
                        value={b2bDealerTier}
                        onChange={(e) => {
                          setB2bDealerTier(e.target.value);
                          handleSimulate();
                        }}
                        className="w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm outline-none transition focus:border-indigo-500 dark:border-slate-700 dark:bg-slate-900 dark:focus:border-indigo-400"
                      >
                        <option value="standard">
                          {t('LandingPage.solutions.simulator.b2bTierStandard')}
                        </option>
                        <option value="gold">
                          {t('LandingPage.solutions.simulator.b2bTierGold')}
                        </option>
                        <option value="premium">
                          {t('LandingPage.solutions.simulator.b2bTierPremium')}
                        </option>
                      </select>
                    </div>
                    <div>
                      <label className="block text-xs text-slate-500 dark:text-slate-400 mb-1">
                        {t('LandingPage.solutions.simulator.b2bTerms')}
                      </label>
                      <select
                        value={b2bTerms}
                        onChange={(e) => {
                          setB2bTerms(e.target.value);
                          handleSimulate();
                        }}
                        className="w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm outline-none transition focus:border-indigo-500 dark:border-slate-700 dark:bg-slate-900 dark:focus:border-indigo-400"
                      >
                        <option value="advance">
                          {t('LandingPage.solutions.simulator.b2bTermsAdvance')}
                        </option>
                        <option value="net30">
                          {t('LandingPage.solutions.simulator.b2bTerms30')}
                        </option>
                        <option value="net60">
                          {t('LandingPage.solutions.simulator.b2bTerms60')}
                        </option>
                      </select>
                    </div>
                    <div>
                      <label className="block text-xs text-slate-500 dark:text-slate-400 mb-1">
                        {t('LandingPage.solutions.simulator.b2bRisk')}
                      </label>
                      <select
                        value={b2bRisk}
                        onChange={(e) => {
                          setB2bRisk(e.target.value);
                          handleSimulate();
                        }}
                        className="w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm outline-none transition focus:border-indigo-500 dark:border-slate-700 dark:bg-slate-900 dark:focus:border-indigo-400"
                      >
                        <option value="low">
                          {t('LandingPage.solutions.simulator.b2bRiskLow')}
                        </option>
                        <option value="medium">
                          {t('LandingPage.solutions.simulator.b2bRiskMedium')}
                        </option>
                        <option value="high">
                          {t('LandingPage.solutions.simulator.b2bRiskHigh')}
                        </option>
                      </select>
                    </div>
                  </>
                )}

                {activeTab === 'mrp' && (
                  <>
                    <div>
                      <div className="flex justify-between text-xs text-slate-500 dark:text-slate-400 mb-1">
                        <span>{t('LandingPage.solutions.simulator.mrpWeight')}</span>
                        <span className="font-bold text-indigo-600 dark:text-indigo-400">
                          {mrpWeight.toLocaleString()} kg
                        </span>
                      </div>
                      <input
                        type="range"
                        min="500"
                        max="8000"
                        step="250"
                        value={mrpWeight}
                        onChange={(e) => {
                          setMrpWeight(Number(e.target.value));
                          handleSimulate();
                        }}
                        className="w-full accent-indigo-600"
                      />
                    </div>
                    <div>
                      <label className="block text-xs text-slate-500 dark:text-slate-400 mb-1">
                        {t('LandingPage.solutions.simulator.mrpThickness')}
                      </label>
                      <select
                        value={mrpThickness}
                        onChange={(e) => {
                          setMrpThickness(Number(e.target.value));
                          handleSimulate();
                        }}
                        className="w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm outline-none transition focus:border-indigo-500 dark:border-slate-700 dark:bg-slate-900 dark:focus:border-indigo-400"
                      >
                        <option value="4">4 mm</option>
                        <option value="6">6 mm</option>
                        <option value="8">8 mm</option>
                        <option value="12">12 mm</option>
                      </select>
                    </div>
                  </>
                )}

                {activeTab === 'finance' && (
                  <>
                    <div>
                      <div className="flex justify-between text-xs text-slate-500 dark:text-slate-400 mb-1">
                        <span>{t('LandingPage.solutions.simulator.finInvoiceVal')}</span>
                        <span className="font-bold text-indigo-600 dark:text-indigo-400">
                          {finInvoiceVal.toLocaleString()} €
                        </span>
                      </div>
                      <input
                        type="range"
                        min="1000"
                        max="50000"
                        step="1000"
                        value={finInvoiceVal}
                        onChange={(e) => {
                          setFinInvoiceVal(Number(e.target.value));
                          handleSimulate();
                        }}
                        className="w-full accent-indigo-600"
                      />
                    </div>
                    <div>
                      <label className="block text-xs text-slate-500 dark:text-slate-400 mb-1">
                        {t('LandingPage.solutions.simulator.finVatRate')}
                      </label>
                      <select
                        value={finVatRate}
                        onChange={(e) => {
                          setFinVatRate(Number(e.target.value));
                          handleSimulate();
                        }}
                        className="w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm outline-none transition focus:border-indigo-500 dark:border-slate-700 dark:bg-slate-900 dark:focus:border-indigo-400"
                      >
                        <option value="10">%10</option>
                        <option value="20">%20</option>
                      </select>
                    </div>
                  </>
                )}

                <button
                  onClick={handleSimulate}
                  disabled={isSimulating}
                  className="flex w-full items-center justify-center gap-2 rounded-xl bg-indigo-600 py-3 text-sm font-semibold text-white shadow-lg shadow-indigo-500/20 hover:bg-indigo-700 transition"
                >
                  <Play size={16} className={isSimulating ? 'animate-spin' : ''} />
                  {t('LandingPage.solutions.simulator.calculate')}
                </button>
              </div>
            </div>

            <div className="flex flex-col space-y-6 lg:col-span-7">
              {activeTab === 'cad' && (
                <>
                  <div className="relative flex aspect-[4/3] w-full items-center justify-center rounded-2xl border border-slate-200/50 bg-slate-100/50 dark:border-slate-800/50 dark:bg-slate-900/60 overflow-hidden p-6">
                    <div className="absolute inset-0 grid grid-cols-12 grid-rows-12 gap-0 pointer-events-none opacity-20 dark:opacity-10">
                      {Array.from({ length: 144 }).map((_, i) => (
                        <div
                          key={i}
                          className="border-r border-b border-slate-400 dark:border-slate-600"
                        />
                      ))}
                    </div>

                    <div
                      className={`relative flex items-center justify-center border-4 border-slate-700 dark:border-slate-500 bg-indigo-500/5 dark:bg-indigo-500/10 shadow-inner rounded transition-all duration-500 ${
                        isSimulating ? 'scale-95 opacity-70' : 'scale-100 opacity-100'
                      }`}
                      style={{
                        width: `${Math.min(80, 40 + (width / 6000) * 40)}%`,
                        height: `${Math.min(80, 40 + (height / 3000) * 40)}%`,
                      }}
                    >
                      <div className="absolute -left-10 top-0 bottom-0 flex flex-col justify-between py-2 items-center text-[10px] font-bold text-slate-500">
                        <div className="h-full w-0.5 bg-slate-300 dark:bg-slate-700 relative flex justify-center items-center">
                          <span className="absolute bg-white px-1.5 py-0.5 rounded border border-slate-200 dark:bg-slate-800 dark:border-slate-700 whitespace-nowrap rotate-270">
                            {height} mm
                          </span>
                        </div>
                      </div>

                      <div className="absolute -bottom-10 left-0 right-0 flex justify-center items-center text-[10px] font-bold text-slate-500">
                        <div className="w-full h-0.5 bg-slate-300 dark:bg-slate-700 relative flex justify-center items-center">
                          <span className="absolute bg-white px-1.5 py-0.5 rounded border border-slate-200 dark:bg-slate-800 dark:border-slate-700 whitespace-nowrap">
                            {width} mm
                          </span>
                        </div>
                      </div>

                      <div className="absolute inset-0 flex h-full w-full justify-evenly">
                        {Array.from({ length: panelCount - 1 }).map((_, i) => (
                          <div
                            key={i}
                            className="w-[3px] h-full bg-slate-700/80 dark:bg-slate-500/80 relative"
                          >
                            <span className="absolute -bottom-4 left-1/2 -translate-x-1/2 text-[8px] text-slate-400 font-medium whitespace-nowrap">
                              {panelWidth} mm
                            </span>
                          </div>
                        ))}
                      </div>

                      <div className="absolute top-2 right-2 flex items-center gap-1 text-[9px] font-semibold text-slate-400 dark:text-slate-500 bg-slate-200/40 dark:bg-slate-800/40 px-2 py-0.5 rounded">
                        <span>{panelCount} Panels</span>
                      </div>
                    </div>
                  </div>

                  <div className="rounded-2xl border border-slate-200/50 bg-white/40 p-5 backdrop-blur-md dark:border-slate-800/50 dark:bg-[#0f1524]/60">
                    <h4 className="text-sm font-bold text-slate-900 dark:text-slate-100 mb-4 flex items-center gap-2 border-b border-slate-200/50 pb-2 dark:border-slate-800/50">
                      <CheckCircle size={16} className="text-emerald-500" />
                      {t('LandingPage.solutions.simulator.calcTitle')}
                    </h4>

                    <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
                      <div className="flex flex-col">
                        <span className="text-xs text-slate-400">
                          {t('LandingPage.solutions.simulator.totalArea')}
                        </span>
                        <span className="text-lg font-bold text-slate-800 dark:text-slate-100">
                          {area.toFixed(2)} m²
                        </span>
                      </div>
                      <div className="flex flex-col">
                        <span className="text-xs text-slate-400">
                          {t('LandingPage.solutions.simulator.panelCount')}
                        </span>
                        <span className="text-lg font-bold text-slate-800 dark:text-slate-100">
                          {panelCount}
                        </span>
                      </div>
                      <div className="flex flex-col">
                        <span className="text-xs text-slate-400">
                          {t('LandingPage.solutions.simulator.wasteRate')}
                        </span>
                        <span className="text-lg font-bold text-emerald-600 dark:text-emerald-400">
                          1.2%
                        </span>
                      </div>
                      <div className="flex flex-col">
                        <span className="text-xs text-slate-400">
                          {t('LandingPage.solutions.simulator.wasteSaved')}
                        </span>
                        <span className="text-lg font-bold text-emerald-600 dark:text-emerald-400">
                          +{wasteSaved} m²
                        </span>
                      </div>
                    </div>

                    <div className="mt-4 grid grid-cols-1 gap-4 border-t border-slate-200/30 pt-4 dark:border-slate-800/30 md:grid-cols-2">
                      <div className="flex flex-col bg-slate-50/50 dark:bg-slate-900/30 p-3 rounded-xl">
                        <span className="text-xs text-slate-400">
                          {t('LandingPage.solutions.simulator.estCost')}
                        </span>
                        <span className="text-xl font-extrabold text-slate-900 dark:text-white">
                          {mfgCost.toLocaleString()} €
                        </span>
                      </div>
                      <div className="flex flex-col bg-indigo-500/5 dark:bg-indigo-500/10 p-3 rounded-xl">
                        <span className="text-xs text-slate-400">
                          {t('LandingPage.solutions.simulator.dealerPrice')}
                        </span>
                        <span className="text-xl font-extrabold text-indigo-600 dark:text-indigo-400">
                          {dealerPrice.toLocaleString()} €
                        </span>
                      </div>
                    </div>

                    <p className="mt-4 text-[11px] leading-relaxed text-slate-500 dark:text-slate-400 italic">
                      {t('LandingPage.solutions.simulator.wasteCompare')}
                    </p>
                  </div>
                </>
              )}

              {activeTab === 'b2b' && (
                <>
                  <div className="relative flex aspect-[4/3] w-full flex-col justify-center rounded-2xl border border-slate-200/50 bg-slate-100/50 dark:border-slate-800/50 dark:bg-slate-900/60 p-6">
                    <div className="rounded-2xl border border-slate-200/60 bg-white/80 p-5 shadow-md dark:border-slate-800/60 dark:bg-[#0f1524]/90 space-y-4 max-w-sm mx-auto w-full transition-all duration-300">
                      <div className="flex items-center justify-between">
                        <span className="text-xs font-bold text-slate-400 uppercase tracking-widest">
                          B2B CARI KART
                        </span>
                        <span
                          className={`text-[10px] font-extrabold px-2.5 py-1 rounded-full ${
                            b2bStatus === 'approved'
                              ? 'bg-emerald-500/10 text-emerald-600 dark:bg-emerald-500/20 dark:text-emerald-400'
                              : b2bStatus === 'warning'
                                ? 'bg-amber-500/10 text-amber-600 dark:bg-amber-500/20 dark:text-amber-400'
                                : 'bg-red-500/10 text-red-600 dark:bg-red-500/20 dark:text-red-400'
                          }`}
                        >
                          {b2bStatus === 'approved' &&
                            t('LandingPage.solutions.simulator.b2bApproved')}
                          {b2bStatus === 'warning' &&
                            t('LandingPage.solutions.simulator.b2bWarning')}
                          {b2bStatus === 'blocked' &&
                            t('LandingPage.solutions.simulator.b2bOverlimit')}
                        </span>
                      </div>

                      <div className="space-y-1">
                        <span className="text-[10px] text-slate-400 uppercase tracking-wider block">
                          Net Sipariş Tutarı
                        </span>
                        <span className="text-2xl font-extrabold text-slate-800 dark:text-white">
                          {b2bNetOrderVal.toLocaleString()} €
                        </span>
                        {b2bDiscount > 0 && (
                          <span className="text-xs text-emerald-600 dark:text-emerald-400 font-medium block">
                            %{Math.round(b2bDiscount * 100)} Bayi İndirimi Uygulandı
                          </span>
                        )}
                      </div>

                      <div className="h-1.5 w-full bg-slate-200 dark:bg-slate-800 rounded-full overflow-hidden">
                        <div
                          className={`h-full rounded-full transition-all duration-500 ${
                            b2bStatus === 'approved'
                              ? 'bg-emerald-500'
                              : b2bStatus === 'warning'
                                ? 'bg-amber-500'
                                : 'bg-red-500'
                          }`}
                          style={{ width: `${Math.min(100, (b2bNetOrderVal / 80000) * 100)}%` }}
                        />
                      </div>

                      <div className="grid grid-cols-2 gap-4 text-xs border-t border-slate-100 pt-3 dark:border-slate-800">
                        <div>
                          <span className="text-slate-400 block mb-0.5">Vade Yapısı</span>
                          <span className="font-semibold text-slate-800 dark:text-slate-200">
                            {b2bTerms === 'advance'
                              ? 'Peşin (Nakit)'
                              : b2bTerms === 'net30'
                                ? '30 Gün Cari'
                                : '60 Gün Çekli'}
                          </span>
                        </div>
                        <div>
                          <span className="text-slate-400 block mb-0.5">Risk Grubu</span>
                          <span className="font-semibold text-slate-800 dark:text-slate-200">
                            {b2bRisk === 'low'
                              ? 'A Sınıfı (Düşük)'
                              : b2bRisk === 'medium'
                                ? 'B Sınıfı (Orta)'
                                : 'C Sınıfı (Yüksek)'}
                          </span>
                        </div>
                      </div>
                    </div>
                  </div>

                  <div className="rounded-2xl border border-slate-200/50 bg-white/40 p-5 backdrop-blur-md dark:border-slate-800/50 dark:bg-[#0f1524]/60">
                    <h4 className="text-sm font-bold text-slate-900 dark:text-slate-100 mb-4 flex items-center gap-2 border-b border-slate-200/50 pb-2 dark:border-slate-800/50">
                      <CheckCircle size={16} className="text-emerald-500" />
                      Bayi Cari Analiz Sonuçları
                    </h4>

                    <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
                      <div className="flex flex-col bg-slate-50/50 dark:bg-slate-900/30 p-3 rounded-xl">
                        <span className="text-xs text-slate-400">
                          {t('LandingPage.solutions.simulator.b2bLimitStatus')}
                        </span>
                        <span
                          className={`text-sm font-bold mt-1 ${
                            b2bStatus === 'approved'
                              ? 'text-emerald-600 dark:text-emerald-400'
                              : b2bStatus === 'warning'
                                ? 'text-amber-600 dark:text-amber-400'
                                : 'text-red-600 dark:text-red-400 font-extrabold'
                          }`}
                        >
                          {b2bStatus === 'approved' && 'İŞLEME UYGUN'}
                          {b2bStatus === 'warning' && 'MANUEL ONAY BEKLİYOR'}
                          {b2bStatus === 'blocked' && 'BLOKE / İŞLEM ENGELLENDİ'}
                        </span>
                      </div>

                      <div className="flex flex-col bg-slate-50/50 dark:bg-slate-900/30 p-3 rounded-xl">
                        <span className="text-xs text-slate-400">
                          {t('LandingPage.solutions.simulator.b2bDownpayment')}
                        </span>
                        <span className="text-sm font-bold text-slate-800 dark:text-slate-100 mt-1">
                          %{b2bDownpaymentPct} ({b2bDownpaymentAmt.toLocaleString()} €)
                        </span>
                      </div>

                      <div className="flex flex-col bg-slate-50/50 dark:bg-slate-900/30 p-3 rounded-xl">
                        <span className="text-xs text-slate-400">
                          {t('LandingPage.solutions.simulator.b2bReservation')}
                        </span>
                        <span className="text-sm font-mono font-bold text-slate-800 dark:text-slate-100 mt-1">
                          {b2bStatus === 'blocked' ? 'REZERVE EDİLMEDİ' : b2bLotNumber}
                        </span>
                      </div>
                    </div>
                  </div>
                </>
              )}

              {activeTab === 'mrp' && (
                <>
                  <div className="relative flex aspect-[4/3] w-full items-center justify-center rounded-2xl border border-slate-200/50 bg-slate-100/50 dark:border-slate-800/50 dark:bg-slate-900/60 overflow-hidden p-6">
                    <div className="absolute inset-0 grid grid-cols-12 grid-rows-12 gap-0 pointer-events-none opacity-20 dark:opacity-10">
                      {Array.from({ length: 144 }).map((_, i) => (
                        <div
                          key={i}
                          className="border-r border-b border-slate-400 dark:border-slate-600"
                        />
                      ))}
                    </div>

                    <div className="w-full max-w-sm rounded-2xl border border-slate-200/50 bg-white/70 p-5 dark:border-slate-800/50 dark:bg-[#0f1524]/90 space-y-4 shadow-sm">
                      <div className="flex justify-between items-center">
                        <span className="text-xs font-bold text-slate-400 uppercase tracking-widest">
                          TEMPER FIRINI SLOTU
                        </span>
                        <span className="text-[10px] font-bold text-indigo-600 dark:text-indigo-400 bg-indigo-500/10 px-2 py-0.5 rounded-md">
                          Kapasite: 1,200 kg / Şarj
                        </span>
                      </div>

                      <div className="grid grid-cols-4 gap-2 bg-slate-100/80 dark:bg-slate-950 p-3 rounded-xl aspect-[2/1] border border-slate-200/40 dark:border-slate-800/40">
                        {Array.from({ length: 12 }).map((_, i) => {
                          const isFilled = i < Math.round((mrpLoadRatio / 100) * 12);
                          return (
                            <div
                              key={i}
                              className={`rounded transition-all duration-500 ${
                                isFilled
                                  ? 'bg-gradient-to-br from-indigo-500 to-indigo-600 shadow-md animate-pulse'
                                  : 'bg-slate-200/60 dark:bg-slate-900'
                              }`}
                            />
                          );
                        })}
                      </div>

                      <div className="flex justify-between text-xs text-slate-500 dark:text-slate-400">
                        <span>Pişirme Kalınlığı: {mrpThickness} mm</span>
                        <span>Fırın Yükleme Oranı: %{mrpLoadRatio}</span>
                      </div>
                    </div>
                  </div>

                  <div className="rounded-2xl border border-slate-200/50 bg-white/40 p-5 backdrop-blur-md dark:border-slate-800/50 dark:bg-[#0f1524]/60">
                    <h4 className="text-sm font-bold text-slate-900 dark:text-slate-100 mb-4 flex items-center gap-2 border-b border-slate-200/50 pb-2 dark:border-slate-800/50">
                      <CheckCircle size={16} className="text-emerald-500" />
                      MRP Fırın Kapasite Çıktıları
                    </h4>

                    <div className="grid grid-cols-3 gap-4">
                      <div className="flex flex-col bg-slate-50/50 dark:bg-slate-900/30 p-3 rounded-xl">
                        <span className="text-xs text-slate-400">
                          {t('LandingPage.solutions.simulator.mrpFurnaceLoad')}
                        </span>
                        <span className="text-lg font-bold text-slate-800 dark:text-slate-100 mt-1">
                          %{mrpLoadRatio}
                        </span>
                      </div>

                      <div className="flex flex-col bg-slate-50/50 dark:bg-slate-900/30 p-3 rounded-xl">
                        <span className="text-xs text-slate-400">
                          {t('LandingPage.solutions.simulator.mrpCycles')}
                        </span>
                        <span className="text-lg font-bold text-slate-800 dark:text-slate-100 mt-1">
                          {t('LandingPage.solutions.simulator.mrpCycleVal', { count: mrpCycles })}
                        </span>
                      </div>

                      <div className="flex flex-col bg-slate-50/50 dark:bg-slate-900/30 p-3 rounded-xl">
                        <span className="text-xs text-slate-400">
                          {t('LandingPage.solutions.simulator.mrpEnergy')}
                        </span>
                        <span className="text-lg font-bold text-indigo-600 dark:text-indigo-400 mt-1">
                          {mrpEnergyCost.toLocaleString()} €
                        </span>
                      </div>
                    </div>
                  </div>
                </>
              )}

              {activeTab === 'finance' && (
                <>
                  <div className="relative flex aspect-[4/3] w-full flex-col justify-center rounded-2xl border border-slate-200/50 bg-slate-100/50 dark:border-slate-800/50 dark:bg-slate-900/60 p-6 overflow-y-auto">
                    <div className="space-y-4 w-full max-w-md mx-auto text-xs">
                      <div className="flex justify-between items-center border-b border-slate-200/50 pb-2 dark:border-slate-850">
                        <span className="font-bold text-slate-500 uppercase tracking-widest">
                          {t('LandingPage.solutions.simulator.finPostLedger')}
                        </span>
                        <span className="text-[10px] font-bold bg-emerald-500/10 text-emerald-600 px-2 py-0.5 rounded">
                          {t('LandingPage.solutions.simulator.finPeriodOpen')}
                        </span>
                      </div>

                      <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
                        <div className="border border-slate-200 dark:border-slate-800 rounded-xl bg-white/70 dark:bg-[#0f1524] p-3 text-center">
                          <span className="font-semibold block border-b border-slate-100 dark:border-slate-800 pb-1 mb-2 text-slate-600 dark:text-slate-400">
                            120 - Alıcılar H.
                          </span>
                          <div className="grid grid-cols-2 gap-2 text-[10px]">
                            <div className="border-r border-slate-100 dark:border-slate-800 text-left">
                              <span className="block text-slate-400">Borç (Dr.)</span>
                              <span className="font-bold text-emerald-600 dark:text-emerald-400">
                                {finReceivable.toLocaleString()} €
                              </span>
                            </div>
                            <div className="text-right">
                              <span className="block text-slate-400">Alacak (Cr.)</span>
                              <span className="text-slate-400">—</span>
                            </div>
                          </div>
                        </div>

                        <div className="border border-slate-200 dark:border-slate-800 rounded-xl bg-white/70 dark:bg-[#0f1524] p-3 text-center">
                          <span className="font-semibold block border-b border-slate-100 dark:border-slate-800 pb-1 mb-2 text-slate-600 dark:text-slate-400">
                            600 - Yurtiçi Satış
                          </span>
                          <div className="grid grid-cols-2 gap-2 text-[10px]">
                            <div className="border-r border-slate-100 dark:border-slate-800 text-left">
                              <span className="block text-slate-400">Borç (Dr.)</span>
                              <span className="text-slate-400">—</span>
                            </div>
                            <div className="text-right">
                              <span className="block text-slate-400">Alacak (Cr.)</span>
                              <span className="font-bold text-slate-800 dark:text-slate-200">
                                {finInvoiceVal.toLocaleString()} €
                              </span>
                            </div>
                          </div>
                        </div>

                        <div className="border border-slate-200 dark:border-slate-800 rounded-xl bg-white/70 dark:bg-[#0f1524] p-3 text-center">
                          <span className="font-semibold block border-b border-slate-100 dark:border-slate-800 pb-1 mb-2 text-slate-600 dark:text-slate-400">
                            391 - Hes. KDV H.
                          </span>
                          <div className="grid grid-cols-2 gap-2 text-[10px]">
                            <div className="border-r border-slate-100 dark:border-slate-800 text-left">
                              <span className="block text-slate-400">Borç (Dr.)</span>
                              <span className="text-slate-400">—</span>
                            </div>
                            <div className="text-right">
                              <span className="block text-slate-400">Alacak (Cr.)</span>
                              <span className="font-bold text-slate-800 dark:text-slate-200">
                                {finVatAmt.toLocaleString()} €
                              </span>
                            </div>
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>

                  <div className="rounded-2xl border border-slate-200/50 bg-white/40 p-5 backdrop-blur-md dark:border-slate-800/50 dark:bg-[#0f1524]/60">
                    <h4 className="text-sm font-bold text-slate-900 dark:text-slate-100 mb-4 flex items-center gap-2 border-b border-slate-200/50 pb-2 dark:border-slate-800/50">
                      <CheckCircle size={16} className="text-emerald-500" />
                      Finansal Etki Çıktıları
                    </h4>

                    <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
                      <div className="flex flex-col">
                        <span className="text-xs text-slate-400">
                          {t('LandingPage.solutions.simulator.finReceivableVal')}
                        </span>
                        <span className="text-lg font-bold text-slate-800 dark:text-slate-100">
                          {finReceivable.toLocaleString()} €
                        </span>
                      </div>
                      <div className="flex flex-col">
                        <span className="text-xs text-slate-400">
                          {t('LandingPage.solutions.simulator.finNetRevenue')}
                        </span>
                        <span className="text-lg font-bold text-slate-800 dark:text-slate-100">
                          {finInvoiceVal.toLocaleString()} €
                        </span>
                      </div>
                      <div className="flex flex-col">
                        <span className="text-xs text-slate-400">
                          {t('LandingPage.solutions.simulator.finCalculatedVat')}
                        </span>
                        <span className="text-lg font-bold text-slate-800 dark:text-slate-100">
                          {finVatAmt.toLocaleString()} €
                        </span>
                      </div>
                      <div className="flex flex-col">
                        <span className="text-xs text-slate-400">
                          {t('LandingPage.solutions.simulator.finPeriodStatus')}
                        </span>
                        <span className="text-sm font-bold text-emerald-600 dark:text-emerald-400 mt-1">
                          POST EDİLDİ
                        </span>
                      </div>
                    </div>
                  </div>
                </>
              )}
            </div>
          </div>
        </div>
      </div>
    </section>
  );
};
