# CoreAlign Mobile (F4.7)

Expo SDK 52 + expo-router v4 mobile companion for CoreAlign. Targets field glass installers (Simple persona). Offline-first.

## Stack

- Expo SDK 52 (managed workflow, new architecture)
- expo-router v4 (file-based routing)
- TypeScript strict
- TanStack Query v5
- Zustand (auth state)
- NativeWind v4 + Tailwind CSS
- i18next (5 locales: en, tr, de, ru, ar)
- expo-secure-store (JWT)
- expo-sqlite (offline outbox)
- expo-camera + expo-image-picker
- react-native-signature-canvas
- expo-notifications (FCM/APNS)

## Run

```bash
cd mobile
npm install
npm run start
```

## Layout

- `app/` expo-router routes
  - `(auth)/login.tsx`
  - `(tabs)/{home,installations,tickets,profile}`
  - `installation/[id]`, `ticket/[id]`, `project/[id]`
- `src/api` axios client + endpoints
- `src/features` auth, installation, ticket, notifications, offline
- `src/shared` ui, db, i18n
- `src/theme` providers
