import { useEffect, useRef, useState, type KeyboardEvent } from 'react';
import { useTranslation } from 'react-i18next';
import { useLocation, useNavigate } from 'react-router-dom';
import { Bot, Send, ThumbsDown, ThumbsUp, X } from 'lucide-react';
import { cn } from '@/shared/lib/cn';
import { Button } from '@/shared/ui/Button/Button';
import { useAiHelperStore } from '@/shared/lib/store/aiHelperStore';
import { askAiHelperStream } from '../api/askAiHelperStream';
import { derivePageEntity } from '../lib/derivePageEntity';
import { postAiHelperFeedback } from '../api/postAiHelperFeedback';
import { FeedbackFormModal } from '@/features/feedback/ui/FeedbackFormModal';
import type { AiHelperMessage } from '../model/types';

const newId = (): string =>
  typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function'
    ? crypto.randomUUID()
    : `${Date.now()}-${Math.random().toString(36).slice(2)}`;

const AiHelperMessageBubble = ({
  message,
  streaming,
  onFeedback,
}: {
  message: AiHelperMessage;
  streaming: boolean;
  onFeedback: (messageId: string, isHelpful: boolean) => void;
}) => {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const close = useAiHelperStore((state) => state.close);
  const isUser = message.role === 'user';

  const openSource = (ref: string) => {
    if (ref.startsWith('/')) {
      navigate(ref);
      close();
    } else if (ref.startsWith('http')) {
      window.open(ref, '_blank', 'noopener,noreferrer');
    }
  };

  return (
    <div className={cn('flex', isUser ? 'justify-end' : 'justify-start')}>
      <div
        className={cn(
          'max-w-[85%] whitespace-pre-wrap break-words rounded-2xl px-3 py-2 text-sm',
          isUser
            ? 'bg-primary-600 text-white'
            : 'bg-slate-100 text-slate-800 dark:bg-slate-800 dark:text-slate-100',
        )}
      >
        {message.error ? (
          <span className="text-danger-600 dark:text-danger-400">{t('AiHelper.Error')}</span>
        ) : (
          <>
            {message.content
              ? message.content
              : streaming && (
                  <span className="text-slate-400 dark:text-slate-500">
                    {t('AiHelper.Thinking')}
                  </span>
                )}
            {message.sources && message.sources.length > 0 && (
              <div className="mt-2 border-t border-slate-200/60 pt-2 dark:border-slate-600/60">
                <p className="mb-1 text-xs font-medium opacity-70">{t('AiHelper.Sources')}</p>
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
            {!streaming && message.sources && message.sources.length === 0 && message.content && (
              <div className="mt-2 border-t border-slate-200/60 pt-2 dark:border-slate-600/60">
                <p className="text-xs italic text-slate-500 dark:text-slate-400">
                  {t('AiHelper.NoSource')}
                </p>
              </div>
            )}
            {!isUser && !streaming && message.answerId && message.content && (
              <div className="mt-2 flex items-center gap-2 border-t border-slate-200/60 pt-2 dark:border-slate-600/60">
                {message.feedback ? (
                  <span className="text-xs text-slate-500 dark:text-slate-400">
                    {t('AiHelper.FeedbackThanks')}
                  </span>
                ) : (
                  <>
                    <button
                      type="button"
                      onClick={() => onFeedback(message.id, true)}
                      aria-label={t('AiHelper.FeedbackHelpful')}
                      className="rounded p-1 text-slate-400 hover:bg-slate-200 hover:text-success-600 dark:hover:bg-slate-700 dark:hover:text-success-400"
                    >
                      <ThumbsUp className="h-3.5 w-3.5" />
                    </button>
                    <button
                      type="button"
                      onClick={() => onFeedback(message.id, false)}
                      aria-label={t('AiHelper.FeedbackNotHelpful')}
                      className="rounded p-1 text-slate-400 hover:bg-slate-200 hover:text-danger-600 dark:hover:bg-slate-700 dark:hover:text-danger-400"
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
  );
};

export const AiHelperPanel = () => {
  const { t, i18n } = useTranslation();
  const close = useAiHelperStore((state) => state.close);
  const location = useLocation();
  const [messages, setMessages] = useState<AiHelperMessage[]>([]);
  const [input, setInput] = useState('');
  const [isStreaming, setIsStreaming] = useState(false);
  const [ticketOpen, setTicketOpen] = useState(false);
  const abortRef = useRef<AbortController | null>(null);
  const listEndRef = useRef<HTMLDivElement | null>(null);
  const conversationIdRef = useRef(newId());

  useEffect(() => () => abortRef.current?.abort(), []);
  useEffect(() => {
    listEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

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

    void askAiHelperStream(
      {
        question,
        locale: i18n.language,
        routePath: location.pathname,
        conversationId: conversationIdRef.current,
        ...(derivePageEntity(location.pathname) ?? {}),
      },
      {
        onSources: (sources) =>
          setMessages((prev) => prev.map((m) => (m.id === assistantId ? { ...m, sources } : m))),
        onToken: (text) =>
          setMessages((prev) =>
            prev.map((m) => (m.id === assistantId ? { ...m, content: m.content + text } : m)),
          ),
        onDone: (answerId) =>
          setMessages((prev) => prev.map((m) => (m.id === assistantId ? { ...m, answerId } : m))),
        onError: () =>
          setMessages((prev) =>
            prev.map((m) => (m.id === assistantId ? { ...m, error: true } : m)),
          ),
      },
      controller.signal,
    ).finally(() => setIsStreaming(false));
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

  const handleKeyDown = (event: KeyboardEvent<HTMLTextAreaElement>) => {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      submit();
    }
  };

  const lastQuestion = [...messages].reverse().find((m) => m.role === 'user')?.content ?? '';

  return (
    <>
      <div
        role="dialog"
        aria-label={t('AiHelper.Title')}
        className={cn(
          'fixed bottom-20 right-4 z-50 flex h-[32rem] max-h-[calc(100vh-7rem)] w-[calc(100vw-2rem)] max-w-sm flex-col',
          'overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-2xl',
          'dark:border-slate-700 dark:bg-slate-900 sm:bottom-24 sm:right-6',
        )}
      >
        <header className="flex items-center gap-2 border-b border-slate-200 bg-slate-50 px-4 py-3 dark:border-slate-700 dark:bg-slate-800">
          <Bot className="h-5 w-5 shrink-0 text-primary-600 dark:text-primary-400" />
          <div className="min-w-0 flex-1">
            <p className="truncate text-sm font-semibold text-slate-900 dark:text-slate-100">
              {t('AiHelper.Title')}
            </p>
            <p className="truncate text-xs text-slate-500 dark:text-slate-400">
              {t('AiHelper.Subtitle')}
            </p>
          </div>
          <button
            type="button"
            onClick={close}
            aria-label={t('AiHelper.Close')}
            className="rounded-md p-1 text-slate-500 hover:bg-slate-200 hover:text-slate-700 dark:text-slate-400 dark:hover:bg-slate-700 dark:hover:text-slate-200"
          >
            <X className="h-5 w-5" />
          </button>
        </header>

        <div className="flex-1 space-y-3 overflow-y-auto px-4 py-3">
          {messages.length === 0 && (
            <p className="text-sm text-slate-500 dark:text-slate-400">{t('AiHelper.Welcome')}</p>
          )}
          {messages.map((message) => (
            <AiHelperMessageBubble
              key={message.id}
              message={message}
              streaming={isStreaming}
              onFeedback={handleFeedback}
            />
          ))}
          <div ref={listEndRef} />
        </div>

        <div className="border-t border-slate-200 p-3 dark:border-slate-700">
          <button
            type="button"
            onClick={() => setTicketOpen(true)}
            className="mb-2 text-xs text-slate-500 underline decoration-dotted hover:text-primary-600 dark:text-slate-400 dark:hover:text-primary-400"
          >
            {t('AiHelper.UnresolvedCta')}
          </button>
          <div className="flex items-end gap-2">
            <textarea
              value={input}
              onChange={(event) => setInput(event.target.value)}
              onKeyDown={handleKeyDown}
              rows={1}
              placeholder={t('AiHelper.Placeholder')}
              aria-label={t('AiHelper.Placeholder')}
              className={cn(
                'max-h-32 flex-1 resize-none rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-900',
                'placeholder:text-slate-400 focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500',
                'dark:border-slate-600 dark:bg-slate-800 dark:text-slate-100 dark:placeholder:text-slate-500',
              )}
            />
            <Button
              type="button"
              size="sm"
              onClick={submit}
              isLoading={isStreaming}
              aria-label={t('AiHelper.Send')}
              className="h-10 w-10 p-0"
            >
              <Send className="h-4 w-4" />
            </Button>
          </div>
        </div>
      </div>
      {ticketOpen && (
        <FeedbackFormModal
          onClose={() => setTicketOpen(false)}
          initialType="Question"
          initialTitle={lastQuestion.slice(0, 180)}
          initialDescription={
            lastQuestion
              ? `${lastQuestion}\n\n${t('AiHelper.TicketContext')}`
              : t('AiHelper.TicketContext')
          }
          initialPageUrl={location.pathname}
        />
      )}
    </>
  );
};
