export const glassEnclosureKeys = {
  all: ['glass-enclosure'] as const,
  colors: () => [...glassEnclosureKeys.all, 'colors'] as const,
  color: (id: string | null) => [...glassEnclosureKeys.colors(), id] as const,
  glassTypes: () => [...glassEnclosureKeys.all, 'glass-types'] as const,
  glassType: (id: string | null) => [...glassEnclosureKeys.glassTypes(), id] as const,
  profileSystems: () => [...glassEnclosureKeys.all, 'profile-systems'] as const,
  profileSystem: (id: string | null) => [...glassEnclosureKeys.profileSystems(), id] as const,
  profileItems: (systemId: string | null) =>
    [...glassEnclosureKeys.profileSystems(), systemId, 'items'] as const,
  hardwareItems: () => [...glassEnclosureKeys.all, 'hardware-items'] as const,
  hardwareItem: (id: string | null) => [...glassEnclosureKeys.hardwareItems(), id] as const,
  hardwareKits: () => [...glassEnclosureKeys.all, 'hardware-kits'] as const,
  hardwareKit: (id: string | null) => [...glassEnclosureKeys.hardwareKits(), id] as const,
  brandVendors: () => [...glassEnclosureKeys.all, 'brand-vendors'] as const,
  discountRules: () => [...glassEnclosureKeys.all, 'discount-rules'] as const,
  notificationTemplates: () => [...glassEnclosureKeys.all, 'notification-templates'] as const,
  windZones: () => [...glassEnclosureKeys.all, 'wind-zones'] as const,
  climateZones: () => [...glassEnclosureKeys.all, 'climate-zones'] as const,
  climateRecommendation: (city: string | null, postalCode: string | null) =>
    [...glassEnclosureKeys.all, 'climate-recommendation', city, postalCode] as const,
  settings: () => [...glassEnclosureKeys.all, 'settings'] as const,
  onboarding: () => [...glassEnclosureKeys.all, 'onboarding'] as const,
};
