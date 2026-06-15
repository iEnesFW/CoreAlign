export interface Role {
  id: number;
  name: string;
  description: string | null;
}

export interface AppUser {
  id: string;
  username: string;
  email: string;
  firstName: string | null;
  lastName: string | null;
  isActive: boolean;
  isEmailConfirmed: boolean;
  roleIds: number[];
  roles: string[];
  lastLoginAtUtc: string | null;
  createdAtUtc: string;
}

export interface InviteUserInput {
  username: string;
  email: string;
  firstName?: string | null;
  lastName?: string | null;
  password: string;
  roleIds: number[];
}

export interface UpdateUserRolesInput {
  id: string;
  roleIds: number[];
}

export interface SetUserActiveInput {
  id: string;
  isActive: boolean;
}
