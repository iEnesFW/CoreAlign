import { useEffect, useState } from 'react';
import { useLocation } from 'react-router-dom';
import { useAuthStore } from '@/shared/lib/store/authStore';
import { useAiHelperStore } from '../model/aiHelperStore';
import { getAiHelperStatus } from '../api/getAiHelperStatus';
import { AiHelperLauncher } from './AiHelperLauncher';
import { AiHelperPanel } from './AiHelperPanel';

const HIDDEN_PATH_FRAGMENTS = ['/print'];

export const AiHelperWidget = () => {
  const isOpen = useAiHelperStore((state) => state.isOpen);
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  const { pathname } = useLocation();
  const [enabled, setEnabled] = useState(true);

  useEffect(() => {
    let active = true;
    void getAiHelperStatus().then((value) => {
      if (active) {
        setEnabled(value);
      }
    });
    return () => {
      active = false;
    };
  }, []);

  if (!isAuthenticated || !enabled) {
    return null;
  }

  if (HIDDEN_PATH_FRAGMENTS.some((fragment) => pathname.includes(fragment))) {
    return null;
  }

  return (
    <>
      {isOpen && <AiHelperPanel />}
      <AiHelperLauncher />
    </>
  );
};
