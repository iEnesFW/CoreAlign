import { useEffect, useRef, useState, type KeyboardEvent } from 'react';
import { useTranslation } from 'react-i18next';
import { Bot, MessageCircle, Send, ThumbsDown, ThumbsUp, X } from 'lucide-react';
import { cn } from '@/shared/lib/cn';
import { authBridge } from '@/shared/api/authBridge';

interface Source {
  title: string;
  sourceRef: string;
  sourceType: string;
}

interface Message {
  id: string;
  role: 'user' | 'assistant';
  content: string;
  sources?: Source[];
  error?: boolean;
  answerId?: string;
  feedback?: 'up' | 'down';
}

const newId = (): string =>
  typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function'
    ? crypto.randomUUID()
    : `${Date.now()}-${Math.random().toString(36).slice(2)}`;

const parseFrame = (
  raw: string,
  onSources: (sources: Source[]) => void,
  onToken: (text: string) => void,
  onDone: (answerId?: string) => void,
  onError: () => void,
): void => {
  let event = 'message';
  let data = '';
  for (const line of raw.split('\n')) {
    if (line.startsWith('event:')) {
      event = line.slice(6).trim();
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
  if (event === 'token') {
    onToken((payload as { text?: string }).text ?? '');
  } else if (event === 'sources') {
    onSources((payload as { sources?: Source[] }).sources ?? []);
  } else if (event === 'done') {
    onDone((payload as { answerId?: string }).answerId);
  } else if (event === 'error') {
    onError();
  }
};

const postAiHelperFeedback = async (answerId: string, isHelpful: boolean): Promise<void> => {
  const token = authBridge.getAccessToken();
  try {
    await fetch('/api/v1/ai-helper/feedback', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
      credentials: 'include',
      body: JSON.stringify({ answerId, isHelpful }),
    });
  } catch {
    return;
  }
};

const GUID_RE = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
const PORTAL_ENTITY_MAP: Record<string, string> = { orders: 'order', invoices: 'invoice' };

const derivePageEntity = (
  pathname: string,
): { pageEntityType: string; pageEntityId: string } | null => {
  const parts = pathname.split('/').filter(Boolean);
  for (let i = parts.length - 1; i >= 1; i -= 1) {
    if (GUID_RE.test(parts[i])) {
      const type = PORTAL_ENTITY_MAP[parts[i - 1].toLowerCase()];
      if (type) {
        return { pageEntityType: type, pageEntityId: parts[i] };
      }
    }
  }
  return null;
};

export const AiHelperWidget = () => {
  const { t, i18n } = useTranslation();
  const [isOpen, setIsOpen] = useState(false);
  const [messages, setMessages] = useState<Message[]>([]);
  const [input, setInput] = useState('');
  const [isStreaming, setIsStreaming] = useState(false);
  const abortRef = useRef<AbortController | null>(null);
  const listEndRef = useRef<HTMLDivElement | null>(null);
  const conversationIdRef = useRef(newId());
  const [enabled, setEnabled] = useState(true);

  useEffect(() => () => abortRef.current?.abort(), []);
  useEffect(() => {
    let active = true;
    void (async () => {
      try {
        const token = authBridge.getAccessToken();
        const response = await fetch('/api/v1/ai-helper/status', {
          headers: { ...(token ? { Authorization: `Bearer ${token}` } : {}) },
          credentials: 'include',
        });
        if (response.ok) {
          const data = (await response.json()) as { enabled?: boolean };
          if (active) {
            setEnabled(data.enabled !== false);
          }
        }
      } catch {
        return;
      }
    })();
    return () => {
      active = false;
    };
  }, []);
  useEffect(() => {
    listEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages, isOpen]);

  const markError = (id: string) =>
    setMessages((prev) => prev.map((m) => (m.id === id ? { ...m, error: true } : m)));

  const submit = () => {
    const question = input.trim();
    if (!question || isStreaming) {
      return;
    }
    setInput('');
    const assistantId = newId();
    setMessages((prev) => [
      ...prev,
      { id: newId(), role: 'user', content: question },
      { id: assistantId, role: 'assistant', content: '' },
    ]);
    setIsStreaming(true);

    const controller = new AbortController();
    abortRef.current = controller;
    const token = authBridge.getAccessToken();

    void (async () => {
      try {
        const response = await fetch('/api/v1/ai-helper/ask', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            ...(token ? { Authorization: `Bearer ${token}` } : {}),
          },
          credentials: 'include',
          body: JSON.stringify({
            question,
            locale: i18n.language,
            routePath: window.location.pathname,
            conversationId: conversationIdRef.current,
            ...(derivePageEntity(window.location.pathname) ?? {}),
          }),
          signal: controller.signal,
        });
        if (!response.ok || !response.body) {
          markError(assistantId);
          return;
        }
        const reader = response.body.getReader();
        const decoder = new TextDecoder();
        let buffer = '';
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
            parseFrame(
              frame,
              (sources) =>
                setMessages((prev) =>
                  prev.map((m) => (m.id === assistantId ? { ...m, sources } : m)),
                ),
              (text) =>
                setMessages((prev) =>
                  prev.map((m) => (m.id === assistantId ? { ...m, content: m.content + text } : m)),
                ),
              (answerId) =>
                setMessages((prev) =>
                  prev.map((m) => (m.id === assistantId ? { ...m, answerId } : m)),
                ),
              () => markError(assistantId),
            );
            separator = buffer.indexOf('\n\n');
          }
        }
      } catch {
        if (!controller.signal.aborted) {
          markError(assistantId);
        }
      } finally {
        setIsStreaming(false);
      }
    })();
  };

  const openSource = (ref: string) => {
    if (ref.startsWith('/')) {
      window.location.assign(ref);
    } else if (ref.startsWith('http')) {
      window.open(ref, '_blank', 'noopener,noreferrer');
    }
  };

  const handleFeedback = (messageId: string, isHelpful: boolean) => {
    const target = messages.find((m) => m.id === messageId);
    if (!target?.answerId || target.feedback) {
      return;
    }
    setMessages((prev) =>
      prev.map((m) => (m.id === messageId ? { ...m, feedback: isHelpful ? 'up' : 'down' } : m)),
    );
    void postAiHelperFeedback(target.answerId, isHelpful);
  };

  const onKeyDown = (event: KeyboardEvent<HTMLTextAreaElement>) => {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      submit();
    }
  };

  if (!enabled) {
    return null;
  }

  return (
    <>
      {isOpen && (
        <div
          role="dialog"
          aria-label={t('aiHelper.title')}
          className={cn(
            'fixed bottom-20 right-4 z-50 flex h-[32rem] max-h-[calc(100vh-7rem)] w-[calc(100vw-2rem)] max-w-sm flex-col',
            'overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-2xl',
            'dark:border-slate-700 dark:bg-slate-900 sm:bottom-24 sm:right-6',
          )}
        >
          <header className="flex items-center gap-2 border-b border-slate-200 bg-slate-50 px-4 py-3 dark:border-slate-700 dark:bg-slate-800">
            <Bot className="h-5 w-5 shrink-0 text-amber-600 dark:text-amber-400" />
            <div className="min-w-0 flex-1">
              <p className="truncate text-sm font-semibold text-slate-900 dark:text-slate-100">
                {t('aiHelper.title')}
              </p>
              <p className="truncate text-xs text-slate-500 dark:text-slate-400">
                {t('aiHelper.subtitle')}
              </p>
            </div>
            <button
              type="button"
              onClick={() => setIsOpen(false)}
              aria-label={t('aiHelper.close')}
              className="rounded-md p-1 text-slate-500 hover:bg-slate-200 hover:text-slate-700 dark:text-slate-400 dark:hover:bg-slate-700"
            >
              <X className="h-5 w-5" />
            </button>
          </header>

          <div className="flex-1 space-y-3 overflow-y-auto px-4 py-3">
            {messages.length === 0 && (
              <p className="text-sm text-slate-500 dark:text-slate-400">{t('aiHelper.welcome')}</p>
            )}
            {messages.map((message) => (
              <div
                key={message.id}
                className={cn('flex', message.role === 'user' ? 'justify-end' : 'justify-start')}
              >
                <div
                  className={cn(
                    'max-w-[85%] whitespace-pre-wrap break-words rounded-2xl px-3 py-2 text-sm',
                    message.role === 'user'
                      ? 'bg-amber-600 text-white'
                      : 'bg-slate-100 text-slate-800 dark:bg-slate-800 dark:text-slate-100',
                  )}
                >
                  {message.error ? (
                    <span className="text-rose-600 dark:text-rose-400">{t('aiHelper.error')}</span>
                  ) : (
                    <>
                      {message.content
                        ? message.content
                        : isStreaming && (
                            <span className="text-slate-400 dark:text-slate-500">
                              {t('aiHelper.thinking')}
                            </span>
                          )}
                      {message.sources && message.sources.length > 0 && (
                        <div className="mt-2 border-t border-slate-200/60 pt-2 dark:border-slate-600/60">
                          <p className="mb-1 text-xs font-medium opacity-70">
                            {t('aiHelper.sources')}
                          </p>
                          <ul className="space-y-1">
                            {message.sources.map((source) => (
                              <li key={source.sourceRef}>
                                <button
                                  type="button"
                                  onClick={() => openSource(source.sourceRef)}
                                  className="text-left text-xs underline opacity-80 hover:opacity-100"
                                >
                                  {source.title}
                                </button>
                              </li>
                            ))}
                          </ul>
                        </div>
                      )}
                      {!isStreaming &&
                        message.sources &&
                        message.sources.length === 0 &&
                        message.content && (
                          <div className="mt-2 border-t border-slate-200/60 pt-2 dark:border-slate-600/60">
                            <p className="text-xs italic text-slate-500 dark:text-slate-400">
                              {t('aiHelper.noSource')}
                            </p>
                          </div>
                        )}
                      {message.role !== 'user' &&
                        !isStreaming &&
                        message.answerId &&
                        message.content && (
                          <div className="mt-2 flex items-center gap-2 border-t border-slate-200/60 pt-2 dark:border-slate-600/60">
                            {message.feedback ? (
                              <span className="text-xs text-slate-500 dark:text-slate-400">
                                {t('aiHelper.feedbackThanks')}
                              </span>
                            ) : (
                              <>
                                <button
                                  type="button"
                                  onClick={() => handleFeedback(message.id, true)}
                                  aria-label={t('aiHelper.feedbackHelpful')}
                                  className="rounded p-1 text-slate-400 hover:bg-slate-200 hover:text-emerald-600 dark:hover:bg-slate-700 dark:hover:text-emerald-400"
                                >
                                  <ThumbsUp className="h-3.5 w-3.5" />
                                </button>
                                <button
                                  type="button"
                                  onClick={() => handleFeedback(message.id, false)}
                                  aria-label={t('aiHelper.feedbackNotHelpful')}
                                  className="rounded p-1 text-slate-400 hover:bg-slate-200 hover:text-rose-600 dark:hover:bg-slate-700 dark:hover:text-rose-400"
                                >
                                  <ThumbsDown className="h-3.5 w-3.5" />
                                </button>
                              </>
                            )}
                          </div>
                        )}
                    </>
                  )}
                </div>
              </div>
            ))}
            <div ref={listEndRef} />
          </div>

          <div className="border-t border-slate-200 p-3 dark:border-slate-700">
            <div className="flex items-end gap-2">
              <textarea
                value={input}
                onChange={(event) => setInput(event.target.value)}
                onKeyDown={onKeyDown}
                rows={1}
                placeholder={t('aiHelper.placeholder')}
                aria-label={t('aiHelper.placeholder')}
                className="max-h-32 flex-1 resize-none rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-900 placeholder:text-slate-400 focus:border-amber-500 focus:outline-none focus:ring-1 focus:ring-amber-500 dark:border-slate-600 dark:bg-slate-800 dark:text-slate-100 dark:placeholder:text-slate-500"
              />
              <button
                type="button"
                onClick={submit}
                disabled={isStreaming}
                aria-label={t('aiHelper.send')}
                className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-amber-600 text-white transition hover:bg-amber-500 disabled:opacity-60"
              >
                <Send className="h-4 w-4" />
              </button>
            </div>
          </div>
        </div>
      )}
      <button
        type="button"
        onClick={() => setIsOpen((value) => !value)}
        aria-label={t('aiHelper.launcher')}
        aria-expanded={isOpen}
        className="fixed bottom-4 right-4 z-40 flex h-14 w-14 items-center justify-center rounded-full bg-amber-600 text-white shadow-lg transition hover:bg-amber-500 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-amber-400 focus-visible:ring-offset-2 dark:focus-visible:ring-offset-slate-900 sm:bottom-6 sm:right-6"
      >
        {isOpen ? <X className="h-6 w-6" /> : <MessageCircle className="h-6 w-6" />}
      </button>
    </>
  );
};
