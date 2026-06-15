import { useState } from 'react';
import { Outlet } from 'react-router-dom';
import { Sidebar } from './Sidebar';
import { Topbar } from './Topbar';
import { Footer } from '@/widgets/Footer/Footer';
import { CookieBanner } from '@/features/consent/CookieBanner';

export const PortalLayout = () => {
  const [sidebarOpen, setSidebarOpen] = useState(false);

  return (
    <div className="min-h-screen bg-slate-50 text-slate-900 dark:bg-slate-950 dark:text-slate-100">
      <Sidebar open={sidebarOpen} onClose={() => setSidebarOpen(false)} />
      <div className="lg:pl-72">
        <Topbar onOpenSidebar={() => setSidebarOpen(true)} />
        <main className="px-4 py-6 sm:px-6 lg:px-10 lg:py-10">
          <div className="mx-auto max-w-7xl space-y-6">
            <Outlet />
          </div>
        </main>
        <Footer />
      </div>
      <CookieBanner />
    </div>
  );
};
