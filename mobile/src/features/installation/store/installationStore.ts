import { create } from 'zustand';
import type { ChecklistItemStatus, InstallationPhoto } from '../api/installationApi';

export interface LocalChecklistOverride {
  status: ChecklistItemStatus;
  notes: string | null;
}

export interface DraftPhoto {
  localId: string;
  uri: string;
  capturedAt: number;
  caption: string | null;
  checklistItemId: string | null;
  uploaded: boolean;
  remote?: InstallationPhoto;
}

export interface DraftSignature {
  signerName: string;
  signerRole: string | null;
  base64: string;
  capturedAt: number;
}

interface InstallationDraft {
  installationId: string;
  notes: string;
  overrides: Record<string, LocalChecklistOverride>;
  photos: DraftPhoto[];
  signature: DraftSignature | null;
  startedAt: number | null;
}

interface InstallationStoreState {
  drafts: Record<string, InstallationDraft>;
  getDraft: (id: string) => InstallationDraft;
  setNotes: (id: string, notes: string) => void;
  setOverride: (id: string, itemId: string, override: LocalChecklistOverride) => void;
  addPhoto: (id: string, photo: DraftPhoto) => void;
  markPhotoUploaded: (id: string, localId: string, remote: InstallationPhoto) => void;
  removePhoto: (id: string, localId: string) => void;
  setSignature: (id: string, signature: DraftSignature | null) => void;
  markStarted: (id: string) => void;
  reset: (id: string) => void;
}

const emptyDraft = (installationId: string): InstallationDraft => ({
  installationId,
  notes: '',
  overrides: {},
  photos: [],
  signature: null,
  startedAt: null,
});

export const useInstallationStore = create<InstallationStoreState>((set, get) => ({
  drafts: {},
  getDraft: (id) => get().drafts[id] ?? emptyDraft(id),
  setNotes: (id, notes) =>
    set((state) => ({
      drafts: {
        ...state.drafts,
        [id]: { ...(state.drafts[id] ?? emptyDraft(id)), notes },
      },
    })),
  setOverride: (id, itemId, override) =>
    set((state) => {
      const existing = state.drafts[id] ?? emptyDraft(id);
      return {
        drafts: {
          ...state.drafts,
          [id]: {
            ...existing,
            overrides: { ...existing.overrides, [itemId]: override },
          },
        },
      };
    }),
  addPhoto: (id, photo) =>
    set((state) => {
      const existing = state.drafts[id] ?? emptyDraft(id);
      return {
        drafts: {
          ...state.drafts,
          [id]: { ...existing, photos: [...existing.photos, photo] },
        },
      };
    }),
  markPhotoUploaded: (id, localId, remote) =>
    set((state) => {
      const existing = state.drafts[id];
      if (!existing) return state;
      return {
        drafts: {
          ...state.drafts,
          [id]: {
            ...existing,
            photos: existing.photos.map((p) =>
              p.localId === localId ? { ...p, uploaded: true, remote } : p,
            ),
          },
        },
      };
    }),
  removePhoto: (id, localId) =>
    set((state) => {
      const existing = state.drafts[id];
      if (!existing) return state;
      return {
        drafts: {
          ...state.drafts,
          [id]: {
            ...existing,
            photos: existing.photos.filter((p) => p.localId !== localId),
          },
        },
      };
    }),
  setSignature: (id, signature) =>
    set((state) => ({
      drafts: {
        ...state.drafts,
        [id]: { ...(state.drafts[id] ?? emptyDraft(id)), signature },
      },
    })),
  markStarted: (id) =>
    set((state) => {
      const existing = state.drafts[id] ?? emptyDraft(id);
      if (existing.startedAt) return state;
      return {
        drafts: {
          ...state.drafts,
          [id]: { ...existing, startedAt: Date.now() },
        },
      };
    }),
  reset: (id) =>
    set((state) => {
      const next = { ...state.drafts };
      delete next[id];
      return { drafts: next };
    }),
}));
