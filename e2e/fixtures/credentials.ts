export interface UserCredentials {
  email: string;
  password: string;
}

const fromEnv = (
  envKey: string,
  fallbackEmail: string,
  fallbackPassword: string,
): UserCredentials => {
  const raw = process.env[envKey];
  if (!raw) {
    return { email: fallbackEmail, password: fallbackPassword };
  }
  const [email, password] = raw.split(':');
  return { email: email ?? fallbackEmail, password: password ?? fallbackPassword };
};

export const adminUser = fromEnv('E2E_ADMIN_USER', 'admin@e2e.local', 'Test1234!');
export const customerUser = fromEnv('E2E_CUSTOMER_USER', 'customer@e2e.local', 'Test1234!');
export const dealerUser = fromEnv('E2E_DEALER_USER', 'dealer@e2e.local', 'Test1234!');

export const skipIfNoStack = () => {
  return process.env.E2E_LIVE_STACK !== '1';
};
