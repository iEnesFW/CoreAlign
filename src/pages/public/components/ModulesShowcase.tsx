import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { DraftingCompass, Cpu, Globe, Coins, ShoppingCart, Award } from 'lucide-react';

type ModuleType = 'cad' | 'mrp' | 'b2b' | 'finance' | 'procure' | 'service';

export const ModulesShowcase = () => {
  const { t } = useTranslation();
  const [activeMod, setActiveMod] = useState<ModuleType>('cad');

  const mods = [
    {
      id: 'cad' as ModuleType,
      icon: <DraftingCompass size={18} />,
      title: t('LandingPage.showcase.m1Title'),
      desc: t('LandingPage.showcase.m1Desc'),
      color: 'text-blue-500 bg-blue-500/10 border-blue-500/20',
      ui: (
        <div className="space-y-4 font-mono text-[11px] text-slate-600 dark:text-slate-400">
          <div className="flex justify-between border-b border-slate-100 pb-2 dark:border-slate-800">
            <span>[CAD_ENGINE_ACTIVE]</span>
            <span className="text-emerald-500">READY</span>
          </div>
          <div className="space-y-1">
            <div>&gt; Constraining profile: ALUM_PREMIUM_THERMAL</div>
            <div>&gt; Snapping angles: 45°, 90°, 135°</div>
            <div>&gt; Boundary test: Max width 6000mm, actual 3200mm ... OK</div>
            <div>&gt; Wind load limit: 1.4 kN/m², actual load 0.85 kN/m² ... OK</div>
          </div>
          <div className="rounded-xl border border-slate-200/60 bg-slate-50/50 p-3 dark:border-slate-800 dark:bg-slate-900/60">
            <div className="mb-2 font-bold text-slate-800 dark:text-white">
              Profile Geometry Snap Grid
            </div>
            <div className="grid grid-cols-5 gap-1 text-center font-bold">
              <span className="rounded bg-indigo-500/10 p-1 text-indigo-650 dark:text-indigo-400">
                0.0
              </span>
              <span className="rounded bg-slate-100 p-1 dark:bg-slate-800">12.5</span>
              <span className="rounded bg-slate-100 p-1 dark:bg-slate-800">25.0</span>
              <span className="rounded bg-slate-100 p-1 dark:bg-slate-800">37.5</span>
              <span className="rounded bg-indigo-500/10 p-1 text-indigo-650 dark:text-indigo-400">
                50.0
              </span>
            </div>
          </div>
        </div>
      ),
    },
    {
      id: 'mrp' as ModuleType,
      icon: <Cpu size={18} />,
      title: t('LandingPage.showcase.m2Title'),
      desc: t('LandingPage.showcase.m2Desc'),
      color: 'text-purple-500 bg-purple-500/10 border-purple-500/20',
      ui: (
        <div className="space-y-4 font-mono text-[11px] text-slate-600 dark:text-slate-400">
          <div className="flex justify-between border-b border-slate-100 pb-2 dark:border-slate-800">
            <span>[MRP_OPTIMIZER_ACTIVE]</span>
            <span className="text-emerald-500">OPTIMAL</span>
          </div>
          <div className="space-y-2">
            <div className="flex justify-between">
              <span>Nesting Efficiency:</span>
              <span className="font-bold text-slate-900 dark:text-white">%98.8</span>
            </div>
            <div className="h-2 w-full rounded-full bg-slate-100 dark:bg-slate-800">
              <div className="h-full rounded-full bg-purple-500" style={{ width: '98.8%' }} />
            </div>
            <div className="flex justify-between">
              <span>Baking Queue:</span>
              <span className="text-indigo-650 dark:text-indigo-400">Batch #1209 (6mm Low-E)</span>
            </div>
            <div className="flex justify-between">
              <span>Furnace Utilization:</span>
              <span className="font-bold text-slate-900 dark:text-white">%84.2</span>
            </div>
          </div>
        </div>
      ),
    },
    {
      id: 'b2b' as ModuleType,
      icon: <Globe size={18} />,
      title: t('LandingPage.showcase.m3Title'),
      desc: t('LandingPage.showcase.m3Desc'),
      color: 'text-emerald-500 bg-emerald-500/10 border-emerald-500/20',
      ui: (
        <div className="space-y-4 font-mono text-[11px] text-slate-600 dark:text-slate-400">
          <div className="flex justify-between border-b border-slate-100 pb-2 dark:border-slate-800">
            <span>[DEALER_PORTAL_LOGS]</span>
            <span className="text-emerald-500">SYNCED</span>
          </div>
          <div className="space-y-1.5">
            <div className="flex justify-between">
              <span>Dealer ID:</span>
              <span className="font-bold">dlr_izmir_09</span>
            </div>
            <div className="flex justify-between">
              <span>Credit Limit:</span>
              <span className="font-bold text-slate-900 dark:text-white">€150,000</span>
            </div>
            <div className="flex justify-between">
              <span>Available Risk:</span>
              <span className="text-emerald-600 dark:text-emerald-400">€42,500 (Safe)</span>
            </div>
            <div className="flex justify-between">
              <span>Special Price List:</span>
              <span className="font-bold">TIER_GOLD_DISC_10</span>
            </div>
          </div>
        </div>
      ),
    },
    {
      id: 'finance' as ModuleType,
      icon: <Coins size={18} />,
      title: t('LandingPage.showcase.m4Title'),
      desc: t('LandingPage.showcase.m4Desc'),
      color: 'text-amber-500 bg-amber-500/10 border-amber-500/20',
      ui: (
        <div className="space-y-4 font-mono text-[11px] text-slate-600 dark:text-slate-400">
          <div className="flex justify-between border-b border-slate-100 pb-2 dark:border-slate-800">
            <span>[GL_LEDGER_ENTRY]</span>
            <span className="text-indigo-650 dark:text-indigo-400">BALANCED</span>
          </div>
          <div className="rounded-xl border border-slate-100 p-2.5 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-900/40">
            <table className="w-full text-left">
              <thead>
                <tr className="border-b border-slate-200 pb-1.5 dark:border-slate-700 text-slate-400">
                  <th>Acc</th>
                  <th>Dr</th>
                  <th>Cr</th>
                </tr>
              </thead>
              <tbody>
                <tr>
                  <td>120.01 (Dealer)</td>
                  <td className="text-emerald-600 dark:text-emerald-400">18,000</td>
                  <td>0.00</td>
                </tr>
                <tr>
                  <td>600.01 (Sales)</td>
                  <td>0.00</td>
                  <td className="text-amber-600 dark:text-amber-400">15,000</td>
                </tr>
                <tr>
                  <td>391.01 (VAT)</td>
                  <td>0.00</td>
                  <td className="text-amber-600 dark:text-amber-400">3,000</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      ),
    },
    {
      id: 'procure' as ModuleType,
      icon: <ShoppingCart size={18} />,
      title: t('LandingPage.showcase.m5Title'),
      desc: t('LandingPage.showcase.m5Desc'),
      color: 'text-pink-500 bg-pink-500/10 border-pink-500/20',
      ui: (
        <div className="space-y-4 font-mono text-[11px] text-slate-600 dark:text-slate-400">
          <div className="flex justify-between border-b border-slate-100 pb-2 dark:border-slate-800">
            <span>[RFQ_EVALUATION]</span>
            <span className="text-indigo-650 dark:text-indigo-400">EVALUATING</span>
          </div>
          <div className="space-y-1.5">
            <div className="flex justify-between border-b border-slate-100/50 pb-1 dark:border-slate-850">
              <span className="font-bold">Vendor A (AlumCorp)</span>
              <span className="text-emerald-600 dark:text-emerald-400">€3.20/kg (Best Offer)</span>
            </div>
            <div className="flex justify-between border-b border-slate-100/50 pb-1 dark:border-slate-850">
              <span>Vendor B (MetalInc)</span>
              <span>€3.45/kg</span>
            </div>
            <div className="flex justify-between">
              <span>Min Stock Level:</span>
              <span>1200kg / Actual 450kg (Reorder Triggered)</span>
            </div>
          </div>
        </div>
      ),
    },
    {
      id: 'service' as ModuleType,
      icon: <Award size={18} />,
      title: t('LandingPage.showcase.m6Title'),
      desc: t('LandingPage.showcase.m6Desc'),
      color: 'text-blue-600 bg-blue-500/10 border-blue-500/20',
      ui: (
        <div className="space-y-4 font-mono text-[11px] text-slate-600 dark:text-slate-400">
          <div className="flex justify-between border-b border-slate-100 pb-2 dark:border-slate-800">
            <span>[SERVICE_CONTRACT_STATUS]</span>
            <span className="text-emerald-500">COMPLIANT</span>
          </div>
          <div className="space-y-1.5">
            <div className="flex justify-between">
              <span>Ticket ID:</span>
              <span>tkt_2209_act</span>
            </div>
            <div className="flex justify-between">
              <span>SLA Response:</span>
              <span className="text-emerald-600 dark:text-emerald-400">
                4 Hours (Resolved in 1.5h)
              </span>
            </div>
            <div className="flex justify-between">
              <span>Customer Rating:</span>
              <span className="font-bold text-slate-900 dark:text-white">
                5.0 / 5.0 (Signed PDF Attached)
              </span>
            </div>
          </div>
        </div>
      ),
    },
  ];

  const currentMod = mods.find((m) => m.id === activeMod) || mods[0];

  return (
    <section className="px-8 py-20 sm:px-16 lg:px-24">
      <div className="mx-auto max-w-5xl">
        <div className="mb-16 text-center">
          <div className="mb-4 inline-flex items-center gap-2 rounded-full border border-indigo-500/30 bg-indigo-500/10 px-3 py-1 text-xs font-semibold text-indigo-600 backdrop-blur-md dark:text-indigo-300">
            <Cpu size={12} />
            MİMARİ
          </div>
          <h2 className="mb-4 text-3xl font-extrabold tracking-tight text-slate-900 dark:text-white md:text-4xl">
            {t('LandingPage.showcase.title')}
          </h2>
          <p className="mx-auto max-w-2xl text-lg text-slate-600 dark:text-slate-400">
            {t('LandingPage.showcase.subtitle')}
          </p>
        </div>

        <div className="grid grid-cols-1 gap-8 lg:grid-cols-12 items-stretch">
          <div className="lg:col-span-5 flex flex-col gap-3">
            {mods.map((m) => {
              const isActive = m.id === activeMod;
              return (
                <button
                  key={m.id}
                  onClick={() => setActiveMod(m.id)}
                  className={`flex items-center gap-4 rounded-2xl border p-4 text-left transition-all duration-300 ${
                    isActive
                      ? 'border-indigo-500 bg-indigo-500/5 shadow-md shadow-indigo-500/5 dark:border-indigo-400 dark:bg-indigo-400/10'
                      : 'border-slate-200/60 bg-white/40 hover:border-slate-350 dark:border-slate-800 dark:bg-slate-900/40 dark:hover:border-slate-700'
                  }`}
                >
                  <div
                    className={`rounded-xl border p-2.5 ${isActive ? m.color : 'text-slate-500 border-slate-200 dark:border-slate-800 bg-slate-50 dark:bg-slate-900'}`}
                  >
                    {m.icon}
                  </div>
                  <div className="flex-1 min-w-0">
                    <h3
                      className={`text-sm font-bold truncate ${isActive ? 'text-slate-900 dark:text-white' : 'text-slate-700 dark:text-slate-300'}`}
                    >
                      {m.title}
                    </h3>
                    <p className="text-[11px] text-slate-500 dark:text-slate-400 truncate mt-0.5">
                      {m.desc}
                    </p>
                  </div>
                </button>
              );
            })}
          </div>

          <div className="lg:col-span-7 flex flex-col justify-between rounded-3xl border border-slate-200 bg-white p-8 shadow-xl dark:border-slate-800/80 dark:bg-[#0f1524]/65">
            <div>
              <div className="flex items-center gap-3 border-b border-slate-100 pb-4 dark:border-slate-800">
                <div className={`rounded-xl border p-2.5 ${currentMod.color}`}>
                  {currentMod.icon}
                </div>
                <div>
                  <h3 className="text-lg font-bold text-slate-900 dark:text-white">
                    {currentMod.title}
                  </h3>
                  <span className="rounded-full bg-slate-100 px-2 py-0.5 text-[9px] font-bold uppercase tracking-wider text-slate-500 dark:bg-slate-800 dark:text-slate-400">
                    CoreAlign Engine v2.3
                  </span>
                </div>
              </div>

              <p className="text-sm leading-relaxed text-slate-650 dark:text-slate-400 mt-6 mb-8">
                {currentMod.desc}
              </p>
            </div>

            <div className="rounded-2xl border border-slate-200/80 bg-slate-50/50 p-6 dark:border-slate-800/70 dark:bg-[#090d16]/70 shadow-inner">
              <span className="mb-4 inline-block text-[10px] font-extrabold uppercase tracking-widest text-indigo-500">
                Live Sandbox Simulation Output
              </span>
              {currentMod.ui}
            </div>
          </div>
        </div>
      </div>
    </section>
  );
};
