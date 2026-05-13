import React, { useState, useRef, useEffect } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { Menu, Search, Sun, Moon, Bell, User, Sliders, Globe, LogOut, Command } from 'lucide-react';
import { useTheme } from '@/app/providers/ThemeProvider';
import { useAuthStore } from '@/features/auth/model/authStore';
import { useLogout } from '@/features/auth/hooks/useAuth';
import { useTranslation } from 'react-i18next';

interface NavbarProps {
  toggleSidebar: () => void;
}

export const Navbar: React.FC<NavbarProps> = ({ toggleSidebar }) => {
  const { theme, toggleTheme } = useTheme();
  const location = useLocation();
  const [isProfileOpen, setIsProfileOpen] = useState(false);
  const profileRef = useRef<HTMLDivElement>(null);

  const user = useAuthStore((state) => state.user);
  const logout = useLogout();
  const { t, i18n } = useTranslation();

  const handleLanguageToggle = () => {
    const nextLang = i18n.language === 'en' ? 'tr' : 'en';
    i18n.changeLanguage(nextLang);
  };

  // Close dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (profileRef.current && !profileRef.current.contains(event.target as Node)) {
        setIsProfileOpen(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  // Format page title from pathname
  const getPageTitle = () => {
    const path = location.pathname.split('/')[1];
    if (!path || path === 'dashboard') return 'Dashboard';
    return path.charAt(0).toUpperCase() + path.slice(1);
  };

  const handleLogout = () => {
    logout.mutate();
  };

  const initials =
    user && user.firstName && user.lastName
      ? `${user.firstName[0]}${user.lastName[0]}`.toUpperCase()
      : 'U';
  const name = user ? `${user.firstName || ''} ${user.lastName || ''}`.trim() : 'User';

  return (
    <header className="h-12 bg-white/80 dark:bg-[#0B0F19]/80 backdrop-blur-md border-b border-slate-200/60 dark:border-slate-800/60 flex items-center justify-between px-3 sticky top-0 z-30 transition-colors duration-200">
      {/* Left section: Mobile menu & Page Title */}
      <div className="flex items-center gap-2">
        <button
          onClick={toggleSidebar}
          className="p-1.5 -ml-1.5 text-slate-500 hover:text-slate-700 dark:text-slate-400 dark:hover:text-slate-200 lg:hidden rounded-[5px] focus:outline-none focus:ring-1 focus:ring-indigo-500 transition-colors"
        >
          <Menu size={18} />
        </button>
        <div className="hidden sm:block">
          <h1 className="text-sm font-bold text-slate-900 dark:text-white tracking-tight">
            {getPageTitle()}
          </h1>
        </div>
      </div>

      {/* Middle section: Search Bar */}
      <div className="flex-1 max-w-md px-4 hidden md:block">
        <div className="relative group">
          <div className="absolute inset-y-0 left-0 pl-2.5 flex items-center pointer-events-none">
            <Search className="h-3.5 w-3.5 text-slate-400 group-focus-within:text-indigo-500 transition-colors" />
          </div>
          <input
            type="text"
            className="block w-full pl-8 pr-10 py-1.5 border border-slate-200/60 dark:border-slate-700/60 rounded-[5px] leading-4 bg-slate-50/50 dark:bg-slate-800/50 text-slate-900 dark:text-white placeholder-slate-400 focus:outline-none focus:bg-white dark:focus:bg-[#0B0F19] focus:ring-1 focus:ring-indigo-500/50 focus:border-indigo-500 text-xs transition-all duration-300 shadow-sm"
            placeholder="Search anything..."
          />
          <div className="absolute inset-y-0 right-0 pr-2 flex items-center pointer-events-none">
            <div className="flex items-center gap-0.5 text-slate-400 text-[10px] font-medium bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-[3px] px-1 py-0.5 shadow-sm">
              <Command size={10} />
              <span>K</span>
            </div>
          </div>
        </div>
      </div>

      {/* Right section: Actions & Profile */}
      <div className="flex items-center gap-1.5">
        {/* Mobile Search Icon */}
        <button className="p-1.5 text-slate-500 hover:text-slate-700 dark:text-slate-400 dark:hover:text-slate-200 md:hidden rounded-[5px] hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors">
          <Search size={16} />
        </button>

        {/* Theme Toggle */}
        <button
          onClick={toggleTheme}
          className="p-1.5 text-slate-500 hover:text-slate-700 dark:text-slate-400 dark:hover:text-slate-200 rounded-[5px] hover:bg-slate-100 dark:hover:bg-slate-800 transition-all focus:outline-none focus:ring-1 focus:ring-indigo-500"
          aria-label="Toggle theme"
        >
          {theme === 'dark' ? <Sun size={16} /> : <Moon size={16} />}
        </button>

        {/* Notifications */}
        <button className="p-1.5 text-slate-500 hover:text-slate-700 dark:text-slate-400 dark:hover:text-slate-200 rounded-[5px] hover:bg-slate-100 dark:hover:bg-slate-800 transition-all relative focus:outline-none focus:ring-1 focus:ring-indigo-500">
          <Bell size={16} />
          <span className="absolute top-1.5 right-1.5 block h-1.5 w-1.5 rounded-full bg-red-500 ring-1 ring-white dark:ring-[#0B0F19]" />
        </button>

        {/* Profile Dropdown */}
        <div className="relative ml-1" ref={profileRef}>
          <button
            onClick={() => setIsProfileOpen(!isProfileOpen)}
            className="flex items-center gap-2 focus:outline-none rounded-[5px] p-1 border border-transparent hover:border-slate-200 dark:hover:border-slate-700 transition-all"
          >
            <div className="h-6 w-6 rounded-[5px] bg-gradient-to-br from-indigo-500 to-purple-600 flex items-center justify-center text-white font-semibold text-[10px] shadow-sm ring-1 ring-white dark:ring-[#0B0F19]">
              {initials}
            </div>
            <div className="hidden sm:block text-left">
              <p className="text-xs font-semibold text-slate-700 dark:text-slate-200 leading-none">
                {name}
              </p>
              <p className="text-[10px] text-slate-500 dark:text-slate-400 mt-0.5 font-medium">
                {user?.tenantName ?? user?.roles?.[0] ?? 'User'}
              </p>
            </div>
          </button>

          {/* Dropdown Menu */}
          {isProfileOpen && (
            <div className="absolute right-0 mt-2 w-48 rounded-[5px] shadow-lg bg-white dark:bg-slate-800 ring-1 ring-slate-200 dark:ring-slate-700 divide-y divide-slate-100 dark:divide-slate-700/50 focus:outline-none py-1 transform opacity-100 scale-100 transition-all duration-200 origin-top-right">
              <div className="px-3 py-2 sm:hidden">
                <p className="text-xs font-semibold text-slate-900 dark:text-white">{name}</p>
                <p className="text-[10px] text-slate-500 dark:text-slate-400 truncate mt-0.5">
                  {user?.email}
                </p>
              </div>

              <div className="px-1 py-1">
                <Link
                  to="/dashboard/profile"
                  onClick={() => setIsProfileOpen(false)}
                  className="group flex items-center px-2 py-1.5 text-[11px] font-medium text-slate-700 dark:text-slate-300 rounded-[5px] hover:bg-slate-50 dark:hover:bg-slate-700/50 hover:text-indigo-600 dark:hover:text-indigo-400 transition-colors"
                >
                  <User className="mr-2 h-3.5 w-3.5 text-slate-400 group-hover:text-indigo-500 transition-colors" />
                  {t('auth.profile')}
                </Link>
                <Link
                  to="/dashboard/activity"
                  onClick={() => setIsProfileOpen(false)}
                  className="group flex items-center px-2 py-1.5 text-[11px] font-medium text-slate-700 dark:text-slate-300 rounded-[5px] hover:bg-slate-50 dark:hover:bg-slate-700/50 hover:text-indigo-600 dark:hover:text-indigo-400 transition-colors"
                >
                  <Sliders className="mr-2 h-3.5 w-3.5 text-slate-400 group-hover:text-indigo-500 transition-colors" />
                  {t('activity.title')}
                </Link>
                <button
                  onClick={handleLanguageToggle}
                  className="group flex w-full items-center justify-between px-2 py-1.5 text-[11px] font-medium text-slate-700 dark:text-slate-300 rounded-[5px] hover:bg-slate-50 dark:hover:bg-slate-700/50 hover:text-indigo-600 dark:hover:text-indigo-400 transition-colors"
                >
                  <div className="flex items-center">
                    <Globe className="mr-2 h-3.5 w-3.5 text-slate-400 group-hover:text-indigo-500 transition-colors" />
                    Language
                  </div>
                  <span className="text-[9px] font-bold bg-slate-100 dark:bg-slate-700 text-slate-500 dark:text-slate-400 px-1.5 py-0.5 rounded-[3px] uppercase">
                    {i18n.language}
                  </span>
                </button>
              </div>

              <div className="px-1 py-1">
                <button
                  onClick={handleLogout}
                  className="group flex w-full items-center px-2 py-1.5 text-[11px] font-medium text-red-600 dark:text-red-400 rounded-[5px] hover:bg-red-50 dark:hover:bg-red-500/10 transition-colors"
                >
                  <LogOut className="mr-2 h-3.5 w-3.5 text-red-500 group-hover:text-red-600 dark:group-hover:text-red-400 transition-colors" />
                  {t('auth.sign_out')}
                </button>
              </div>
            </div>
          )}
        </div>
      </div>
    </header>
  );
};
