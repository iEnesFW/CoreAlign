export const generateDeviceFingerprint = async (): Promise<string> => {
  const components: string[] = [];

  components.push(navigator.userAgent);
  components.push(navigator.language);
  components.push(`${screen.width}x${screen.height}x${screen.colorDepth}`);
  components.push(Intl.DateTimeFormat().resolvedOptions().timeZone);
  components.push(String(navigator.hardwareConcurrency || 0));
  components.push(navigator.platform);

  try {
    const canvas = document.createElement('canvas');
    const ctx = canvas.getContext('2d');
    if (ctx) {
      ctx.textBaseline = 'top';
      ctx.font = '14px Arial';
      ctx.fillStyle = '#f60';
      ctx.fillRect(125, 1, 62, 20);
      ctx.fillStyle = '#069';
      ctx.fillText('CoreAlign', 2, 15);
      components.push(canvas.toDataURL());
    }
  } catch {
    components.push('canvas-unavailable');
  }

  const raw = components.join('|');
  const encoder = new TextEncoder();
  const data = encoder.encode(raw);
  const hashBuffer = await crypto.subtle.digest('SHA-256', data);
  const hashArray = Array.from(new Uint8Array(hashBuffer));

  return hashArray.map((b) => b.toString(16).padStart(2, '0')).join('');
};
