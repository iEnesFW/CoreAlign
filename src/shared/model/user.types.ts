export interface UserProfile {
  id: string;
  tenantId: string;
  tenantName: string;
  tenantSlug: string;
  username: string;
  email: string;
  firstName: string | null;
  lastName: string | null;
  avatarUrl: string | null;
  roles: string[];
  preferredLocale?: string | null;
}
