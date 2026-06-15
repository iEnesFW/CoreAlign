import React, { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { MapPin, Phone, Mail, Clock, Send } from 'lucide-react';
import { safeRequestWithNotify } from '@/shared/lib/safeRequest';

export const ContactSection = () => {
  const { t } = useTranslation();
  const [formData, setFormData] = useState({
    name: '',
    email: '',
    subject: '',
    message: '',
  });
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!formData.name || !formData.email || !formData.message) return;

    setLoading(true);
    const mockRequest = new Promise((resolve) => {
      setTimeout(() => resolve(true), 1500);
    });

    const [data] = await safeRequestWithNotify(mockRequest, {
      successMessage: t('LandingPage.contact.success'),
      showSuccessNotification: true,
    });

    if (data) {
      setFormData({
        name: '',
        email: '',
        subject: '',
        message: '',
      });
    }
    setLoading(false);
  };

  return (
    <section
      id="contact"
      className="border-t border-slate-200/50 bg-white/30 px-8 py-20 backdrop-blur-sm sm:px-16 lg:px-24 dark:border-slate-800/50 dark:bg-slate-900/30"
    >
      <div className="mx-auto max-w-4xl">
        <div className="mb-12 text-center">
          <h2 className="mb-4 text-3xl font-extrabold tracking-tight text-slate-900 dark:text-white md:text-4xl">
            {t('LandingPage.contact.title')}
          </h2>
          <p className="text-lg text-slate-600 dark:text-slate-400">
            {t('LandingPage.contact.subtitle')}
          </p>
        </div>
        <div className="grid grid-cols-1 gap-12 md:grid-cols-2">
          <div className="rounded-3xl border border-slate-200/60 bg-white/50 p-8 shadow-sm dark:border-slate-800/60 dark:bg-slate-800/50">
            <form onSubmit={handleSubmit} className="space-y-4">
              <div>
                <label className="block text-xs font-semibold text-slate-700 dark:text-slate-300">
                  {t('LandingPage.contact.name')}
                </label>
                <input
                  type="text"
                  required
                  value={formData.name}
                  onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                  className="mt-1 w-full rounded-xl border border-slate-200 bg-white/80 px-4 py-2.5 text-sm outline-none transition focus:border-indigo-500 dark:border-slate-700 dark:bg-slate-900/80 dark:focus:border-indigo-400"
                />
              </div>
              <div>
                <label className="block text-xs font-semibold text-slate-700 dark:text-slate-300">
                  {t('LandingPage.contact.email')}
                </label>
                <input
                  type="email"
                  required
                  value={formData.email}
                  onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                  className="mt-1 w-full rounded-xl border border-slate-200 bg-white/80 px-4 py-2.5 text-sm outline-none transition focus:border-indigo-500 dark:border-slate-700 dark:bg-slate-900/80 dark:focus:border-indigo-400"
                />
              </div>
              <div>
                <label className="block text-xs font-semibold text-slate-700 dark:text-slate-300">
                  {t('LandingPage.contact.subject')}
                </label>
                <input
                  type="text"
                  value={formData.subject}
                  onChange={(e) => setFormData({ ...formData, subject: e.target.value })}
                  className="mt-1 w-full rounded-xl border border-slate-200 bg-white/80 px-4 py-2.5 text-sm outline-none transition focus:border-indigo-500 dark:border-slate-700 dark:bg-slate-900/80 dark:focus:border-indigo-400"
                />
              </div>
              <div>
                <label className="block text-xs font-semibold text-slate-700 dark:text-slate-300">
                  {t('LandingPage.contact.message')}
                </label>
                <textarea
                  required
                  rows={4}
                  value={formData.message}
                  onChange={(e) => setFormData({ ...formData, message: e.target.value })}
                  className="mt-1 w-full rounded-xl border border-slate-200 bg-white/80 px-4 py-2.5 text-sm outline-none transition focus:border-indigo-500 dark:border-slate-700 dark:bg-slate-900/80 dark:focus:border-indigo-400"
                />
              </div>
              <button
                type="submit"
                disabled={loading}
                className="inline-flex w-full items-center justify-center gap-2 rounded-xl bg-indigo-600 px-6 py-3 font-semibold text-white shadow-lg transition hover:bg-indigo-700 disabled:opacity-50"
              >
                {loading ? t('LandingPage.contact.sending') : t('LandingPage.contact.submit')}
                <Send size={16} />
              </button>
            </form>
          </div>
          <div className="flex flex-col justify-between space-y-8 py-2">
            <div>
              <h3 className="mb-6 text-xl font-bold text-slate-900 dark:text-slate-100">
                {t('LandingPage.contact.infoTitle')}
              </h3>
              <div className="space-y-4">
                <div className="flex items-start gap-4">
                  <div className="rounded-xl bg-indigo-500/10 p-3 text-indigo-600 dark:bg-indigo-500/20 dark:text-indigo-400">
                    <MapPin size={18} />
                  </div>
                  <div>
                    <p className="text-sm leading-relaxed text-slate-600 dark:text-slate-400">
                      {t('LandingPage.contact.address')}
                    </p>
                  </div>
                </div>
                <div className="flex items-center gap-4">
                  <div className="rounded-xl bg-indigo-500/10 p-3 text-indigo-600 dark:bg-indigo-500/20 dark:text-indigo-400">
                    <Phone size={18} />
                  </div>
                  <div>
                    <p className="text-sm text-slate-600 dark:text-slate-400">
                      {t('LandingPage.contact.phone')}
                    </p>
                  </div>
                </div>
                <div className="flex items-center gap-4">
                  <div className="rounded-xl bg-indigo-500/10 p-3 text-indigo-600 dark:bg-indigo-500/20 dark:text-indigo-400">
                    <Mail size={18} />
                  </div>
                  <div>
                    <p className="text-sm text-slate-600 dark:text-slate-400">
                      {t('LandingPage.contact.emailLabel')}
                    </p>
                  </div>
                </div>
                <div className="flex items-center gap-4">
                  <div className="rounded-xl bg-indigo-500/10 p-3 text-indigo-600 dark:bg-indigo-500/20 dark:text-indigo-400">
                    <Clock size={18} />
                  </div>
                  <div>
                    <p className="text-sm text-slate-600 dark:text-slate-400">
                      {t('LandingPage.contact.hours')}
                    </p>
                  </div>
                </div>
              </div>
            </div>
            <div className="h-48 overflow-hidden rounded-3xl border border-slate-200/60 bg-slate-100 dark:border-slate-800/60 dark:bg-slate-900/60">
              <div className="flex h-full w-full flex-col items-center justify-center p-4 text-center">
                <span className="text-xs font-semibold uppercase tracking-widest text-indigo-500">
                  Interactive Map
                </span>
                <span className="mt-1 text-xs text-slate-500">Teknopark İstanbul</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
};
