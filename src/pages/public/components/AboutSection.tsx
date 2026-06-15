import { useTranslation } from 'react-i18next';
import { History, Target, Compass, Award, Shield, Lock, Server, ShieldCheck } from 'lucide-react';

export const AboutSection = () => {
  const { t } = useTranslation();

  const mainItems = [
    {
      icon: <History className="h-6 w-6 text-indigo-500" />,
      title: t('LandingPage.about.historyTitle'),
      text: t('LandingPage.about.historyText'),
    },
    {
      icon: <Target className="h-6 w-6 text-purple-500" />,
      title: t('LandingPage.about.missionTitle'),
      text: t('LandingPage.about.missionText'),
    },
    {
      icon: <Compass className="h-6 w-6 text-fuchsia-500" />,
      title: t('LandingPage.about.visionTitle'),
      text: t('LandingPage.about.visionText'),
    },
    {
      icon: <Award className="h-6 w-6 text-pink-500" />,
      title: t('LandingPage.about.valuesTitle'),
      text: t('LandingPage.about.valuesText'),
    },
  ];

  const milestones = [
    {
      year: t('LandingPage.about.t1Year'),
      title: t('LandingPage.about.t1Title'),
      text: t('LandingPage.about.t1Text'),
    },
    {
      year: t('LandingPage.about.t2Year'),
      title: t('LandingPage.about.t2Title'),
      text: t('LandingPage.about.t2Text'),
    },
    {
      year: t('LandingPage.about.t3Year'),
      title: t('LandingPage.about.t3Title'),
      text: t('LandingPage.about.t3Text'),
    },
    {
      year: t('LandingPage.about.t4Year'),
      title: t('LandingPage.about.t4Title'),
      text: t('LandingPage.about.t4Text'),
    },
  ];

  const complianceItems = [
    {
      icon: <Shield className="h-6 w-6 text-indigo-600 dark:text-indigo-400" />,
      bg: 'bg-indigo-500/10',
      title: t('LandingPage.about.c1Title'),
      desc: t('LandingPage.about.c1Desc'),
    },
    {
      icon: <Lock className="h-6 w-6 text-emerald-600 dark:text-emerald-400" />,
      bg: 'bg-emerald-500/10',
      title: t('LandingPage.about.c2Title'),
      desc: t('LandingPage.about.c2Desc'),
    },
    {
      icon: <Server className="h-6 w-6 text-blue-600 dark:text-blue-400" />,
      bg: 'bg-blue-500/10',
      title: t('LandingPage.about.c3Title'),
      desc: t('LandingPage.about.c3Desc'),
    },
    {
      icon: <ShieldCheck className="h-6 w-6 text-purple-600 dark:text-purple-400" />,
      bg: 'bg-purple-500/10',
      title: t('LandingPage.about.c4Title'),
      desc: t('LandingPage.about.c4Desc'),
    },
  ];

  return (
    <section className="px-8 py-16 sm:px-16 lg:px-24">
      <div className="mx-auto max-w-4xl space-y-20">
        <div>
          <div className="mb-12 text-center">
            <h2 className="mb-4 text-3xl font-extrabold tracking-tight text-slate-900 dark:text-white md:text-4xl">
              {t('LandingPage.about.title')}
            </h2>
            <p className="text-lg text-slate-600 dark:text-slate-400">
              {t('LandingPage.about.subtitle')}
            </p>
          </div>
          <div className="grid grid-cols-1 gap-8 sm:grid-cols-2">
            {mainItems.map((item, index) => (
              <div
                key={index}
                className="rounded-2xl border border-slate-200/60 bg-white/40 p-6 shadow-sm backdrop-blur-md transition-all duration-300 hover:translate-y-[-4px] hover:border-indigo-500/30 hover:shadow-md dark:border-slate-800/60 dark:bg-slate-900/40"
              >
                <div className="mb-4 inline-flex rounded-xl bg-slate-100 p-3 dark:bg-slate-800/80">
                  {item.icon}
                </div>
                <h3 className="mb-2 text-xl font-bold text-slate-900 dark:text-slate-100">
                  {item.title}
                </h3>
                <p className="text-sm leading-relaxed text-slate-600 dark:text-slate-400">
                  {item.text}
                </p>
              </div>
            ))}
          </div>
        </div>

        <div>
          <div className="mb-12 text-center">
            <h2 className="mb-4 text-2xl font-bold tracking-tight text-slate-900 dark:text-white md:text-3xl">
              {t('LandingPage.about.timelineTitle')}
            </h2>
            <p className="text-base text-slate-600 dark:text-slate-400">
              {t('LandingPage.about.timelineSubtitle')}
            </p>
          </div>

          <div className="relative border-l border-slate-200/80 dark:border-slate-800/80 ml-4 md:ml-6 space-y-8 py-2">
            {milestones.map((milestone, index) => (
              <div key={index} className="relative pl-8 md:pl-10">
                <div className="absolute -left-2 top-1.5 bg-indigo-600 rounded-full h-4 w-4 border-4 border-slate-50 dark:border-[#0b0f19] ring-4 ring-indigo-500/10" />
                <div className="rounded-2xl border border-slate-200/50 bg-white/40 p-6 backdrop-blur-sm transition-all duration-300 hover:border-indigo-500/30 dark:border-slate-800/50 dark:bg-slate-900/40">
                  <span className="inline-block rounded-full bg-indigo-500/10 px-3 py-1 text-xs font-bold text-indigo-600 dark:bg-indigo-500/20 dark:text-indigo-400 mb-2">
                    {milestone.year}
                  </span>
                  <h4 className="text-lg font-bold text-slate-900 dark:text-white">
                    {milestone.title}
                  </h4>
                  <p className="text-sm text-slate-600 dark:text-slate-400 mt-2 leading-relaxed">
                    {milestone.text}
                  </p>
                </div>
              </div>
            ))}
          </div>
        </div>

        <div>
          <div className="mb-12 text-center">
            <h2 className="mb-4 text-2xl font-bold tracking-tight text-slate-900 dark:text-white md:text-3xl">
              {t('LandingPage.about.complianceTitle')}
            </h2>
            <p className="text-base text-slate-600 dark:text-slate-400">
              {t('LandingPage.about.complianceSubtitle')}
            </p>
          </div>

          <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-4">
            {complianceItems.map((c, index) => (
              <div
                key={index}
                className="rounded-2xl border border-slate-200/50 bg-white/40 p-6 shadow-sm backdrop-blur-md transition-all duration-300 hover:translate-y-[-4px] hover:border-indigo-500/30 hover:shadow-md dark:border-slate-800/50 dark:bg-slate-900/40"
              >
                <div className={`mb-4 inline-flex rounded-xl ${c.bg} p-3`}>{c.icon}</div>
                <h4 className="mb-2 text-base font-bold text-slate-900 dark:text-slate-100 leading-tight">
                  {c.title}
                </h4>
                <p className="text-xs leading-relaxed text-slate-600 dark:text-slate-400">
                  {c.desc}
                </p>
              </div>
            ))}
          </div>
        </div>
      </div>
    </section>
  );
};
