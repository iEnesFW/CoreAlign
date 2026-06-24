import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';
import { VitePWA } from 'vite-plugin-pwa';
import type { ServerResponse } from 'node:http';
import path from 'path';

export default defineConfig({
  plugins: [
    react(),
    tailwindcss(),
    VitePWA({
      registerType: 'autoUpdate',
      includeAssets: ['favicon.svg', 'icons/*.png'],
      manifest: {
        name: 'CoreAlign ERP',
        short_name: 'CoreAlign',
        description: 'Multi-tenant Turkish SaaS ERP — offline-capable field operations',
        theme_color: '#6366f1',
        background_color: '#FFFFFF',
        display: 'standalone',
        start_url: '/',
        scope: '/',
        icons: [
          {
            src: '/icons/icon-192.png',
            sizes: '192x192',
            type: 'image/png',
            purpose: 'any maskable',
          },
          {
            src: '/icons/icon-512.png',
            sizes: '512x512',
            type: 'image/png',
            purpose: 'any maskable',
          },
        ],
      },
      workbox: {
        maximumFileSizeToCacheInBytes: 15 * 1024 * 1024,
        globPatterns: ['**/*.{js,css,html,svg,png,woff2}'],
        navigateFallbackDenylist: [/^\/api\//],
        runtimeCaching: [
          {
            urlPattern: /\/api\/v1\/glass-enclosure\//,
            handler: 'NetworkFirst',
            options: {
              cacheName: 'api-glass-enclosure',
              networkTimeoutSeconds: 5,
              expiration: { maxEntries: 200, maxAgeSeconds: 60 * 60 * 24 },
              cacheableResponse: { statuses: [0, 200] },
              plugins: [
                {
                  cacheKeyWillBeUsed: async ({ request }: { request: Request }) => {
                    const tenantId = request.headers.get('X-CoreAlign-Tenant-Id') ?? 'no-tenant';
                    const url = new URL(request.url);
                    url.searchParams.set('__tenant', tenantId);
                    return url.toString();
                  },
                },
              ],
            },
          },
          {
            urlPattern: /\/api\/v1\/installation-acceptances\//,
            handler: 'NetworkFirst',
            options: {
              cacheName: 'api-installation',
              networkTimeoutSeconds: 3,
              expiration: { maxEntries: 200, maxAgeSeconds: 60 * 60 * 24 * 7 },
              cacheableResponse: { statuses: [0, 200] },
              plugins: [
                {
                  cacheKeyWillBeUsed: async ({ request }: { request: Request }) => {
                    const tenantId = request.headers.get('X-CoreAlign-Tenant-Id') ?? 'no-tenant';
                    const url = new URL(request.url);
                    url.searchParams.set('__tenant', tenantId);
                    return url.toString();
                  },
                },
              ],
            },
          },
        ],
      },
      devOptions: {
        enabled: false,
      },
    }),
  ],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    port: 5273,
    strictPort: true,
    proxy: {
      '/api': {
        target: 'http://localhost:5178',
        changeOrigin: true,
        secure: false,
        configure: (proxy) => {
          // Backend henuz baslamamissa ECONNREFUSED log spam'i yapma —
          // 503 don ve kullaniciya geri don, F5 ardindan dev ortaminda dakikalar sururebilir.
          // Vite'in kendi built-in 'error' handler'i AggregateError/ECONNREFUSED stack'ini
          // basiyor; onu kaldirip event'i tamamen biz sahiplenelim (asagida hemen yeni
          // listener ekledigimiz icin unhandled-'error' crash riski yok).
          proxy.removeAllListeners('error');
          proxy.on('error', (err, _req, res) => {
            const code = (err as NodeJS.ErrnoException).code;
            if (code === 'ECONNREFUSED' || code === 'ECONNRESET') {
              if (res && !(res as ServerResponse).headersSent && 'writeHead' in res) {
                try {
                  (res as ServerResponse).writeHead(503, { 'Content-Type': 'application/json' });
                  (res as ServerResponse).end(
                    JSON.stringify({ error: 'Backend not ready, please retry in a few seconds.' }),
                  );
                } catch {
                  /* socket already closed */
                }
              }
              return;
            }
            console.error('[vite proxy] /api error:', err);
          });
        },
      },
      '/health': {
        target: 'http://localhost:5178',
        changeOrigin: true,
        secure: false,
        configure: (proxy) => {
          proxy.removeAllListeners('error');
          proxy.on('error', (err) => {
            const code = (err as NodeJS.ErrnoException).code;
            if (code === 'ECONNREFUSED' || code === 'ECONNRESET') return;
            console.error('[vite proxy] /health error:', err);
          });
        },
      },
    },
  },
  build: {
    chunkSizeWarningLimit: 600,
    target: 'es2022',
    sourcemap: false,
    cssCodeSplit: true,
    rollupOptions: {
      output: {
        manualChunks: {
          'vendor-react': ['react', 'react-dom', 'react-router-dom'],
          'vendor-query': ['@tanstack/react-query', 'axios'],
          'vendor-charts': ['recharts'],
          'vendor-3d': ['three', '@react-three/fiber', '@react-three/drei'],
          'vendor-i18n': ['i18next', 'react-i18next'],
          'vendor-forms': ['react-hook-form', '@hookform/resolvers', 'zod'],
          'vendor-geo': ['country-state-city'],
        },
      },
    },
  },
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: ['./src/test/setup.ts'],
    include: ['src/**/*.{test,spec}.{ts,tsx}'],
    coverage: {
      reporter: ['text', 'html'],
      exclude: ['node_modules', 'dist', 'src/test', '**/*.d.ts'],
    },
  },
});
