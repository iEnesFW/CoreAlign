import React, { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Calendar, Send } from 'lucide-react';
import { safeRequestWithNotify } from '@/shared/lib/safeRequest';

export const DemoScheduler = () => {
  const { t } = useTranslation();
  const [formData, setFormData] = useState({
    name: '',
    email: '',
    company: '',
    module: 'cad',
    date: '',
  });
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!formData.name || !formData.email || !formData.company || !formData.date) return;

    setLoading(true);
    const mockRequest = new Promise((resolve) => {
      setTimeout(() => resolve(true), 1500);
    });

    const [data] = await safeRequestWithNotify(mockRequest, {
      successMessage: t('LandingPage.scheduler.success'),
      showSuccessNotification: true,
    });

    if (data) {
      setFormData({
        name: '',
        email: '',
        company: '',
        module: 'cad',
        date: '',
      });
    }
    setLoading(false);
  };

  return (
    <section className="px-8 py-20 sm:px-16 lg:px-24">
      <div className="mx-auto max-w-xl">
        <div className="rounded-3xl border border-slate-200/60 bg-white/60 p-8 shadow-xl backdrop-blur-md dark:border-slate-800/80 dark:bg-[#0f1524]/65">
          <div className="mb-8 text-center">
            <div className="mb-4 inline-flex items-center gap-2 rounded-full border border-indigo-500/30 bg-indigo-500/10 px-3 py-1 text-xs font-semibold text-indigo-600 backdrop-blur-md dark:text-indigo-300">
              <Calendar size={12} />
              BİREBİR DEMO
            </div>
            <h3 className="text-2xl font-extrabold text-slate-900 dark:text-white">
              {t('LandingPage.scheduler.title')}
            </h3>
            <p className="mt-2 text-xs text-slate-500 dark:text-slate-400 leading-relaxed">
              {t('LandingPage.scheduler.subtitle')}
            </p>
          </div>

          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <div>
                <label className="block text-[10px] font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400">
                  {t('LandingPage.scheduler.name')}
                </label>
                <input
                  type="text"
                  required
                  value={formData.name}
                  onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                  className="mt-1.5 w-full rounded-xl border border-slate-200 bg-white/80 px-4 py-2.5 text-xs outline-none transition focus:border-indigo-500 dark:border-slate-800 dark:bg-slate-900/80 dark:focus:border-indigo-400"
                />
              </div>

              <div>
                <label className="block text-[10px] font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400">
                  {t('LandingPage.scheduler.email')}
                </label>
                <input
                  type="email"
                  required
                  value={formData.email}
                  onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                  className="mt-1.5 w-full rounded-xl border border-slate-200 bg-white/80 px-4 py-2.5 text-xs outline-none transition focus:border-indigo-500 dark:border-slate-800 dark:bg-slate-900/80 dark:focus:border-indigo-400"
                />
              </div>
            </div>

            <div>
              <label className="block text-[10px] font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400">
                {t('LandingPage.scheduler.company')}
              </label>
              <input
                type="text"
                required
                value={formData.company}
                onChange={(e) => setFormData({ ...formData, company: e.target.value })}
                className="mt-1.5 w-full rounded-xl border border-slate-200 bg-white/80 px-4 py-2.5 text-xs outline-none transition focus:border-indigo-500 dark:border-slate-800 dark:bg-slate-900/80 dark:focus:border-indigo-400"
              />
            </div>

            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <div>
                <label className="block text-[10px] font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400">
                  {t('LandingPage.scheduler.module')}
                </label>
                <select
                  value={formData.module}
                  onChange={(e) => setFormData({ ...formData, module: e.target.value })}
                  className="mt-1.5 w-full rounded-xl border border-slate-200 bg-white/80 px-3 py-2.5 text-xs outline-none transition focus:border-indigo-500 dark:border-slate-800 dark:bg-slate-900/80 dark:focus:border-indigo-400"
                >
                  <option value="cad">{t('LandingPage.scheduler.optCAD')}</option>
                  <option value="mrp">{t('LandingPage.scheduler.optMRP')}</option>
                  <option value="b2b">{t('LandingPage.scheduler.optB2B')}</option>
                  <option value="finance">{t('LandingPage.scheduler.optFinance')}</option>
                </select>
              </div>

              <div>
                <label className="block text-[10px] font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400">
                  {t('LandingPage.scheduler.date')}
                </label>
                <input
                  type="date"
                  required
                  value={formData.date}
                  onChange={(e) => setFormData({ ...formData, date: e.target.value })}
                  className="mt-1.5 w-full rounded-xl border border-slate-200 bg-white/80 px-3 py-2.5 text-xs outline-none transition focus:border-indigo-500 dark:border-slate-800 dark:bg-slate-900/80 dark:focus:border-indigo-400"
                />
              </div>
            </div>

            <button
              type="submit"
              disabled={loading}
              className="mt-4 inline-flex w-full items-center justify-center gap-2 rounded-xl bg-indigo-600 px-6 py-3 font-bold text-white shadow-lg transition hover:bg-indigo-700 disabled:opacity-50"
            >
              {loading ? t('LandingPage.scheduler.sending') : t('LandingPage.scheduler.submit')}
              <Send size={14} />
            </button>
          </form>
        </div>
      </div>
    </section>
  );
};
