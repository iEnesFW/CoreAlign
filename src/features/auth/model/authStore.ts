import { create } from 'zustand';
import type { UserProfile } from './auth.types';

interface AuthState {
    accessToken: string | null;
    user: UserProfile | null;
    isAuthenticated: boolean;
    setAuth: (accessToken: string, user: UserProfile) => void;
    clearAuth: () => void;
    updateUser: (user: Partial<UserProfile>) => void;
    setAccessToken: (token: string) => void;
}

export const useAuthStore = create<AuthState>((set) => ({
    accessToken: null,
    user: JSON.parse(localStorage.getItem('user') || 'null'),
    isAuthenticated: !!localStorage.getItem('user'),

    setAuth: (accessToken, user) => {
        localStorage.setItem('user', JSON.stringify(user));
        set({ accessToken, user, isAuthenticated: true });
    },

    clearAuth: () => {
        localStorage.removeItem('user');
        set({ accessToken: null, user: null, isAuthenticated: false });
    },

    updateUser: (userData) =>
        set((state) => {
            const updatedUser = state.user ? { ...state.user, ...userData } : null;
            if (updatedUser) {
                localStorage.setItem('user', JSON.stringify(updatedUser));
            }
            return { user: updatedUser };
        }),

    setAccessToken: (token) => set({ accessToken: token }),
}));
