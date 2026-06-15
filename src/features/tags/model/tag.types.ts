export interface Tag {
  id: string;
  name: string;
  colorHex: string | null;
  isActive: boolean;
}

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
