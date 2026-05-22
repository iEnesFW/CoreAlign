import React, { Suspense, useCallback, useEffect, useState } from 'react';
import { Outlet } from 'react-router-dom';
import { Sidebar } from '@/widgets/Sidebar/Sidebar';
import { Navbar } from '@/widgets/Navbar/Navbar';
import { Footer } from '@/widgets/Footer/Footer';
import { RouteFallback } from '@/shared/ui/RouteFallback/RouteFallback';
import { prefetchCommonDashboardPages } from '@/app/router/routePrefetch';

type NavigatorConnection = { saveData?: boolean; effectiveType?: string };

const shouldPrefetchPages = (): boolean => {
  if (typeof navigator === 'undefined') return false;
  const conn = (navigator as Navigator & { connection?: NavigatorConnection }).connection;
  if (!conn) return true;
  if (conn.saveData) return false;
  // 2g/slow-2g/3g networks: skip optimistic prefetching to save data and CPU.
  return conn.effectiveType ? !/(2g|3g)/.test(conn.effectiveType) : true;
};

export const DashboardLayout: React.FC = () => {
  const [isSidebarOpen, setIsSidebarOpen] = useState(false);
  const [isSidebarCollapsed, setIsSidebarCollapsed] = useState(false);

  useEffect(() => {
    if (shouldPrefetchPages()) {
      prefetchCommonDashboardPages();
    }
  }, []);

  // Stable callback identity so the memoized Navbar doesn't re-render on every
  // layout state change.
  const toggleSidebar = useCallback(() => setIsSidebarOpen((open) => !open), []);

  return (
    <div className="flex h-screen overflow-hidden bg-slate-50/50 dark:bg-[#060913] transition-colors duration-300">
      <Sidebar
        isOpen={isSidebarOpen}
        setIsOpen={setIsSidebarOpen}
        isCollapsed={isSidebarCollapsed}
        setIsCollapsed={setIsSidebarCollapsed}
      />

      <div className="flex flex-col flex-1 w-full min-w-0 overflow-hidden relative">
        <Navbar toggleSidebar={toggleSidebar} />

        <main className="flex-1 overflow-y-auto overflow-x-hidden">
          <div className="mx-auto flex h-full w-full max-w-[1920px] flex-col px-2 sm:px-4 lg:px-6 2xl:px-8">
            <Suspense fallback={<RouteFallback />}>
              <Outlet />
            </Suspense>
          </div>
        </main>

        <Footer />
      </div>
    </div>
  );
};
