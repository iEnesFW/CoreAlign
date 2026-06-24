import React, { useMemo, useState } from 'react';
import { useColorScheme } from 'react-native';
import { ThemeContext, type ThemeContextValue, type ThemeMode } from './themeContext';

export const ThemeProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const system = useColorScheme();
  const [mode, setMode] = useState<ThemeMode>('system');

  const value = useMemo<ThemeContextValue>(() => {
    const resolved: 'light' | 'dark' =
      mode === 'system' ? (system === 'dark' ? 'dark' : 'light') : mode;
    return { mode, resolved, setMode };
  }, [mode, system]);

  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>;
};
