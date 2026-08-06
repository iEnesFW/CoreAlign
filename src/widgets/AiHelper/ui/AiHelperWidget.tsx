import { useEffect } from 'react';
import { useLocation } from 'react-router-dom';
import { useAuthStore } from '@/shared/lib/store/authStore';
import { useAiHelperStore } from '@/shared/lib/store/aiHelperStore';
import { getAiHelperStatus } from '../api/getAiHelperStatus';
import { AiHelperPanel } from './AiHelperPanel';

const HIDDEN_PATH_FRAGMENTS = ['/print'];

export const AiHelperWidget = () => {
  const isOpen = useAiHelperStore((state) => state.isOpen);
  const isAvailable = useAiHelperStore((state) => state.isAvailable);
  const setAvailable = useAiHelperStore((state) => state.setAvailable);
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  const { pathname } = useLocation();

  useEffect(() => {
    if (!isAuthenticated) {
      setAvailable(false);
      return;
    }
    let active = true;
    void getAiHelperStatus().then((value) => {
      if (active) {
        setAvailable(value);
      }
    });
    return () => {
      active = false;
    };
  }, [isAuthenticated, setAvailable]);

  if (!isAuthenticated || !isAvailable || !isOpen) {
    return null;
  }

  if (HIDDEN_PATH_FRAGMENTS.some((fragment) => pathname.includes(fragment))) {
    return null;
  }

  return <AiHelperPanel />;
};
