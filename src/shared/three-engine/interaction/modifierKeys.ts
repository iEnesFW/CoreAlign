const state = { ctrl: false, shift: false, alt: false };

const update = (e: KeyboardEvent | PointerEvent | MouseEvent) => {
  state.ctrl = e.ctrlKey || e.metaKey;
  state.shift = e.shiftKey;
  state.alt = e.altKey;
};

const reset = () => {
  state.ctrl = false;
  state.shift = false;
  state.alt = false;
};

export const trackModifierKeys = () => {
  const onKey = (e: KeyboardEvent) => update(e);
  const onPointer = (e: PointerEvent) => update(e);
  window.addEventListener('keydown', onKey);
  window.addEventListener('keyup', onKey);
  window.addEventListener('pointerdown', onPointer, true);
  window.addEventListener('blur', reset);
  return () => {
    window.removeEventListener('keydown', onKey);
    window.removeEventListener('keyup', onKey);
    window.removeEventListener('pointerdown', onPointer, true);
    window.removeEventListener('blur', reset);
    reset();
  };
};

export const isCtrlPressed = () => state.ctrl;
export const isShiftPressed = () => state.shift;
export const isAltPressed = () => state.alt;
