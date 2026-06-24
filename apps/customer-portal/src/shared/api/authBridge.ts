export interface AuthBridge {
  getAccessToken: () => string | null;
  clearAuth: () => void;
}

let impl: AuthBridge | null = null;

export const registerAuthBridge = (bridge: AuthBridge): void => {
  impl = bridge;
};

export const authBridge: AuthBridge = {
  getAccessToken: () => impl?.getAccessToken() ?? null,
  clearAuth: () => impl?.clearAuth(),
};
