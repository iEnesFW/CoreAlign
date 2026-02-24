import { create } from 'zustand';
import type { UserProfile } from './auth.types';

interface AuthState {
    accessToken: string | null;
    refreshToken: string | null;
    user: UserProfile | null;
    isAuthenticated: boolean;
    setAuth: (accessToken: string, refreshToken: string, user: UserProfile) => void;
    clearAuth: () => void;
    updateUser: (user: UserProfile) => void;
}

export const useAuthStore = create<AuthState>((set) => ({
    accessToken: localStorage.getItem('accessToken'),
    refreshToken: localStorage.getItem('refreshToken'),
    user: JSON.parse(localStorage.getItem('user') || 'null') as UserProfile | null,
    isAuthenticated: !!localStorage.getItem('accessToken'),

    setAuth: (accessToken, refreshToken, user) => {
        localStorage.setItem('accessToken', accessToken);
        localStorage.setItem('refreshToken', refreshToken);
        localStorage.setItem('user', JSON.stringify(user));
        set({ accessToken, refreshToken, user, isAuthenticated: true });
    },

    clearAuth: () => {
        localStorage.removeItem('accessToken');
        localStorage.removeItem('refreshToken');
        localStorage.removeItem('user');
        set({ accessToken: null, refreshToken: null, user: null, isAuthenticated: false });
    },

    updateUser: (user) => {
        localStorage.setItem('user', JSON.stringify(user));
        set({ user });
    },
}));
