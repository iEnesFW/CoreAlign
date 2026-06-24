import { env } from '@/shared/lib/env';
import { authBridge } from '@/shared/api/authBridge';
import { logger } from '@/shared/lib/logger';
import type { AiHelperSource } from '../model/types';

export interface AskStreamRequest {
  question: string;
  locale: string;
  routePath?: string;
  conversationId: string;
  pageEntityType?: string;
  pageEntityId?: string;
}

export interface AskStreamHandlers {
  onSources: (sources: AiHelperSource[]) => void;
  onToken: (text: string) => void;
  onDone: (answerId?: string) => void;
  onError: () => void;
}

const parseFrame = (raw: string, handlers: AskStreamHandlers): void => {
  let eventName = 'message';
  let data = '';
  for (const line of raw.split('\n')) {
    if (line.startsWith('event:')) {
      eventName = line.slice(6).trim();
    } else if (line.startsWith('data:')) {
      data += line.slice(5).trim();
    }
  }

  if (!data) {
    return;
  }

  let payload: unknown;
  try {
    payload = JSON.parse(data);
  } catch {
    return;
  }

  if (eventName === 'token') {
    handlers.onToken((payload as { text?: string }).text ?? '');
  } else if (eventName === 'sources') {
    handlers.onSources((payload as { sources?: AiHelperSource[] }).sources ?? []);
  } else if (eventName === 'done') {
    handlers.onDone((payload as { answerId?: string }).answerId);
  } else if (eventName === 'error') {
    handlers.onError();
  }
};

export const askAiHelperStream = async (
  request: AskStreamRequest,
  handlers: AskStreamHandlers,
  signal: AbortSignal,
): Promise<void> => {
  const token = authBridge.getAccessToken();

  let response: Response;
  try {
    response = await fetch(`${env.VITE_API_URL}/api/v1/ai-helper/ask`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
      credentials: 'include',
      body: JSON.stringify(request),
      signal,
    });
  } catch (error) {
    if (!signal.aborted) {
      logger.error('AI Helper request failed', error);
      handlers.onError();
    }
    return;
  }

  if (!response.ok || !response.body) {
    handlers.onError();
    return;
  }

  const reader = response.body.getReader();
  const decoder = new TextDecoder();
  let buffer = '';

  try {
    for (;;) {
      const { done, value } = await reader.read();
      if (done) {
        break;
      }
      buffer += decoder.decode(value, { stream: true });
      let separator = buffer.indexOf('\n\n');
      while (separator !== -1) {
        const frame = buffer.slice(0, separator);
        buffer = buffer.slice(separator + 2);
        parseFrame(frame, handlers);
        separator = buffer.indexOf('\n\n');
      }
    }
  } catch (error) {
    if (!signal.aborted) {
      logger.error('AI Helper stream interrupted', error);
      handlers.onError();
    }
  }
};
