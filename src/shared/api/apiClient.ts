import axios from 'axios';
import { useAuthStore } from '@/features/auth/model/authStore';
import { authApi } from '@/features/auth/api/authApi';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5178';

export const apiClient = axios.create({
    baseURL: `${API_BASE_URL}/api`,
    headers: {
        'Content-Type': 'application/json',
    },
    withCredentials: true,
});

apiClient.interceptors.request.use((config) => {
    const { accessToken } = useAuthStore.getState();
    if (accessToken) {
        config.headers.Authorization = `Bearer ${accessToken}`;
    }
    return config;
});

let isRefreshing = false;
let failedQueue: Array<{
    resolve: (value: unknown) => void;
    reject: (reason: unknown) => void;
}> = [];

const processQueue = (error: unknown) => {
    failedQueue.forEach((prom) => {
        if (error) {
            prom.reject(error);
        } else {
            prom.resolve(undefined);
        }
    });
    failedQueue = [];
};

apiClient.interceptors.response.use(
    (response) => {
        console.log("apiClient Response Interceptor:", response.config.url, response.status);
        return response;
    },
    async (error) => {
        console.error("apiClient ERROR Interceptor:", error?.config?.url, error?.response?.status, error?.message);
        const originalRequest = error.config;

        const isAuthRequest = originalRequest.url?.includes('/auth/login') || originalRequest.url?.includes('/auth/refresh-token');

        if (error.response?.status !== 401 || originalRequest._retry || isAuthRequest) {
            return Promise.reject(error);
        }

        if (isRefreshing) {
            return new Promise((resolve, reject) => {
                failedQueue.push({ resolve, reject });
            }).then(() => apiClient(originalRequest));
        }

        originalRequest._retry = true;
        isRefreshing = true;

        try {
            const response = await authApi.refreshToken();

            if (response.isSuccess && response.data) {
                useAuthStore.getState().setAuth(response.data.accessToken, response.data.user);
                processQueue(null);
                return apiClient(originalRequest);
            }

            throw new Error('Token refresh failed');
        } catch (refreshError) {
            processQueue(refreshError);
            useAuthStore.getState().clearAuth();
            window.location.href = '/login';
            return Promise.reject(refreshError);
        } finally {
            isRefreshing = false;
        }
    }
);
