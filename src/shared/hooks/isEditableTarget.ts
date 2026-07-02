export const isEditableTarget = (el: EventTarget | null): boolean => {
  if (!el || typeof el !== 'object' || !('tagName' in el)) return false;
  const node = el as HTMLElement;
  const tag = node.tagName;
  return (
    tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT' || node.isContentEditable === true
  );
};
