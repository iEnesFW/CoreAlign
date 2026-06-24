import { env } from '@/shared/lib/env';
import { authBridge } from '@/shared/api/authBridge';
import { logger } from '@/shared/lib/logger';

export const postAiHelperFeedback = async (answerId: string, isHelpful: boolean): Promise<void> => {
  const token = authBridge.getAccessToken();
  try {
    await fetch(`${env.VITE_API_URL}/api/v1/ai-helper/feedback`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
      credentials: 'include',
      body: JSON.stringify({ answerId, isHelpful }),
    });
  } catch (error) {
    logger.error('AI Helper feedback failed', error);
  }
};
