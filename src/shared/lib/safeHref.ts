export const safeHref = (raw?: string | null): string | null => {
  if (!raw) return null;
  try {
    const url = new URL(raw, window.location.origin);
    return url.protocol === 'http:' || url.protocol === 'https:' ? url.href : null;
  } catch {
    return null;
  }
};
