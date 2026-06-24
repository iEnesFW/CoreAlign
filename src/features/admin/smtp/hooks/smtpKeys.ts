export const smtpKeys = {
  all: ['admin', 'smtp'] as const,
  settings: () => [...smtpKeys.all, 'settings'] as const,
  health: () => [...smtpKeys.all, 'health'] as const,
};
