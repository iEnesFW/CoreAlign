export interface AiHelperSource {
  title: string;
  sourceRef: string;
  sourceType: string;
}

export interface AiHelperMessage {
  id: string;
  role: 'user' | 'assistant';
  content: string;
  sources?: AiHelperSource[];
  error?: boolean;
  answerId?: string;
  feedback?: 'up' | 'down';
}
