import { useTranslation } from 'react-i18next';
import { Network, Database, Cpu } from 'lucide-react';

export const IntegrationsGrid = () => {
  const { t } = useTranslation();

  const sections = [
    {
      icon: <Network className="h-6 w-6 text-indigo-500" />,
      title: t('LandingPage.integrations.erpLabel'),
      desc: t('LandingPage.integrations.erpDesc'),
      tags: ['Logo Tiger/Go', 'Netsis 3', 'Mikro Fly', 'SAP B1', 'MS Dynamics'],
    },
    {
      icon: <Database className="h-6 w-6 text-emerald-500" />,
      title: t('LandingPage.integrations.dbLabel'),
      desc: t('LandingPage.integrations.dbDesc'),
      tags: ['MS SQL', 'PostgreSQL', 'REST API', 'JSON Webhooks', 'OAuth 2.0'],
    },
    {
      icon: <Cpu className="h-6 w-6 text-amber-500" />,
      title: t('LandingPage.integrations.hwLabel'),
      desc: t('LandingPage.integrations.hwDesc'),
      tags: ['Modbus TCP/IP', 'OPC UA', 'OPC DA', 'Serial RS-485', 'MQTT Broker'],
    },
  ];

  return (
    <section className="border-t border-slate-200/50 bg-white/20 px-8 py-20 backdrop-blur-sm sm:px-16 lg:px-24 dark:border-slate-800/50 dark:bg-slate-900/20">
      <div className="mx-auto max-w-5xl">
        <div className="mb-16 text-center">
          <div className="mb-4 inline-flex items-center gap-2 rounded-full border border-indigo-500/30 bg-indigo-500/10 px-3 py-1 text-xs font-semibold text-indigo-600 backdrop-blur-md dark:text-indigo-300">
            <Network size={12} />
            ENTEGRASYON
          </div>
          <h2 className="mb-4 text-3xl font-extrabold tracking-tight text-slate-900 dark:text-white md:text-4xl">
            {t('LandingPage.integrations.title')}
          </h2>
          <p className="mx-auto max-w-2xl text-lg text-slate-600 dark:text-slate-400">
            {t('LandingPage.integrations.subtitle')}
          </p>
        </div>

        <div className="grid grid-cols-1 gap-8 md:grid-cols-3">
          {sections.map((sec, idx) => (
            <div
              key={idx}
              className="flex flex-col justify-between rounded-3xl border border-slate-200 bg-white/50 p-6 shadow-sm backdrop-blur-sm transition-all duration-300 hover:translate-y-[-4px] hover:border-indigo-500/30 hover:shadow-md dark:border-slate-800/60 dark:bg-[#0f1524]/50 dark:shadow-none"
            >
              <div>
                <div className="mb-4 inline-flex rounded-2xl bg-slate-100 p-3 dark:bg-slate-800/80">
                  {sec.icon}
                </div>
                <h3 className="mb-2 text-lg font-bold text-slate-900 dark:text-white">
                  {sec.title}
                </h3>
                <p className="text-xs leading-relaxed text-slate-600 dark:text-slate-400 mb-6">
                  {sec.desc}
                </p>
              </div>

              <div className="flex flex-wrap gap-1.5 border-t border-slate-100/50 pt-4 dark:border-slate-800/60">
                {sec.tags.map((tag, tIdx) => (
                  <span
                    key={tIdx}
                    className="rounded-lg bg-indigo-500/5 border border-indigo-550/5 px-2 py-1 text-[10px] font-bold text-indigo-650 dark:bg-indigo-500/10 dark:text-indigo-400 dark:border-indigo-500/10"
                  >
                    {tag}
                  </span>
                ))}
              </div>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
};
