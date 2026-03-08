import React from 'react';
import { useTranslation } from 'react-i18next';
import { Cpu, Activity, Zap, Shield } from 'lucide-react';

export const FuturisticCard: React.FC = () => {
    const { t } = useTranslation();

    return (
        <div className="relative overflow-hidden rounded-xl border border-indigo-500/30 bg-gradient-to-b from-indigo-500/10 to-transparent p-[1px] backdrop-blur-md group h-full">
            {/* Animated glowing background */}
            <div className="absolute -top-20 -right-20 w-40 h-40 bg-indigo-500/20 blur-[50px] rounded-full group-hover:bg-indigo-400/30 transition-colors duration-500"></div>
            <div className="absolute -bottom-20 -left-20 w-40 h-40 bg-fuchsia-500/20 blur-[50px] rounded-full group-hover:bg-fuchsia-400/30 transition-colors duration-500"></div>

            <div className="relative h-full bg-white/90 dark:bg-[#0B0F19]/90 backdrop-blur-xl rounded-[11px] p-5 border border-slate-200/50 dark:border-white/5 flex flex-col justify-between">
                <div className="flex items-start justify-between mb-5">
                    <div className="flex items-center gap-3">
                        <div className="relative flex items-center justify-center w-12 h-12 rounded-full bg-indigo-50 dark:bg-indigo-500/20 border border-indigo-200 dark:border-indigo-500/50 text-indigo-600 dark:text-indigo-400 shadow-[0_0_15px_rgba(99,102,241,0.15)] dark:shadow-[0_0_15px_rgba(99,102,241,0.3)]">
                            <div className="absolute inset-0 rounded-full border border-indigo-400/30 dark:border-indigo-400/50 animate-[spin_4s_linear_infinite] border-t-transparent"></div>
                            <Cpu size={20} />
                        </div>
                        <div>
                            <h4 className="text-sm font-bold text-slate-900 dark:text-white tracking-wide">
                                QUANTUM CORE
                            </h4>
                            <p className="text-[10px] font-mono text-slate-500 dark:text-indigo-200/60 mt-0.5">ID: SYS-9948-X</p>
                        </div>
                    </div>
                    <div className="flex items-center gap-1.5 px-2 py-1 bg-emerald-50 dark:bg-emerald-500/10 border border-emerald-200 dark:border-emerald-500/20 rounded text-emerald-600 dark:text-emerald-400 text-[9px] font-mono uppercase tracking-wider shadow-[0_0_10px_rgba(16,185,129,0.1)] dark:shadow-[0_0_10px_rgba(16,185,129,0.2)]">
                        <span className="w-1.5 h-1.5 rounded-full bg-emerald-500 dark:bg-emerald-400 animate-pulse"></span>
                        Online
                    </div>
                </div>

                <div className="space-y-4 mb-5">
                    <div>
                        <div className="flex justify-between text-[9px] font-mono text-slate-500 dark:text-slate-400 mb-1.5 uppercase tracking-wider">
                            <span className="flex items-center gap-1"><Activity size={10} /> {t('dashboard.stats.processing_power')}</span>
                            <span className="text-indigo-600 dark:text-indigo-300 font-bold">87%</span>
                        </div>
                        <div className="w-full bg-slate-100 dark:bg-slate-800/50 rounded-full h-1.5 overflow-hidden border border-slate-200 dark:border-white/5">
                            <div className="bg-gradient-to-r from-indigo-500 to-fuchsia-500 h-full rounded-full relative" style={{ width: '87%' }}>
                                <div className="absolute inset-0 bg-white/20 w-full animate-[pulse_2s_ease-in-out_infinite]"></div>
                            </div>
                        </div>
                    </div>

                    <div className="grid grid-cols-2 gap-3">
                        <div className="bg-slate-50 dark:bg-white/5 border border-slate-200 dark:border-white/5 rounded-lg p-2.5 flex flex-col gap-1 transition-colors hover:border-indigo-300 dark:hover:border-indigo-500/50">
                            <span className="text-[9px] font-mono text-slate-500 dark:text-slate-400 uppercase flex items-center gap-1"><Zap size={10} /> {t('dashboard.stats.latency')}</span>
                            <span className="text-xs font-bold text-slate-900 dark:text-white font-mono">12.4ms</span>
                        </div>
                        <div className="bg-slate-50 dark:bg-white/5 border border-slate-200 dark:border-white/5 rounded-lg p-2.5 flex flex-col gap-1 transition-colors hover:border-emerald-300 dark:hover:border-emerald-500/50">
                            <span className="text-[9px] font-mono text-slate-500 dark:text-slate-400 uppercase flex items-center gap-1"><Shield size={10} /> {t('dashboard.stats.uptime')}</span>
                            <span className="text-xs font-bold text-slate-900 dark:text-white font-mono">99.99%</span>
                        </div>
                    </div>
                </div>

                <div className="flex gap-2 pt-4 border-t border-slate-200 dark:border-white/10">
                    <button className="flex-1 px-3 py-2 text-[10px] font-mono uppercase tracking-wider text-slate-600 dark:text-slate-300 bg-slate-50 dark:bg-white/5 border border-slate-200 dark:border-white/10 hover:bg-slate-100 dark:hover:bg-white/10 hover:text-slate-900 dark:hover:text-white rounded-lg transition-all">
                        Diagnostics
                    </button>
                    <button className="flex-1 px-3 py-2 text-[10px] font-mono uppercase tracking-wider text-white bg-indigo-600 hover:bg-indigo-500 border border-indigo-500 dark:border-indigo-400/50 rounded-lg shadow-[0_0_15px_rgba(99,102,241,0.3)] dark:shadow-[0_0_15px_rgba(99,102,241,0.4)] hover:shadow-[0_0_25px_rgba(99,102,241,0.5)] dark:hover:shadow-[0_0_25px_rgba(99,102,241,0.6)] transition-all">
                        Initialize
                    </button>
                </div>
            </div>
        </div>
    );
};
