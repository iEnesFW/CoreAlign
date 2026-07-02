import { useRef } from 'react';
import type { MouseEvent as ReactMouseEvent } from 'react';

export const useBackdropClick = (onBackdropClick: () => void) => {
  const armed = useRef(false);

  return {
    onMouseDown: (e: ReactMouseEvent) => {
      armed.current = e.target === e.currentTarget;
    },
    onClick: (e: ReactMouseEvent) => {
      if (armed.current && e.target === e.currentTarget) {
        onBackdropClick();
      }
      armed.current = false;
    },
  };
};
