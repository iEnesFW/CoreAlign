export interface AuthBridge {
  getAccessToken: () => string | null;
  applyToken: (accessToken: string) => void;
  signOut: () => void;
  refresh: () => Promise<string | null>;
}

let impl: AuthBridge | null = null;

export const registerAuthBridge = (bridge: AuthBridge): void => {
  impl = bridge;
};

export const authBridge: AuthBridge = {
  getAccessToken: () => impl?.getAccessToken() ?? null,
  applyToken: (accessToken) => impl?.applyToken(accessToken),
  signOut: () => impl?.signOut(),
  refresh: () => impl?.refresh() ?? Promise.resolve(null),
};
