export type { Tag } from '@/shared/model/tag.types';

export interface CreateTagInput {
  name: string;
  colorHex?: string | null;
}

export interface UpdateTagInput {
  id: string;
  name: string;
  colorHex?: string | null;
  isActive: boolean;
}
