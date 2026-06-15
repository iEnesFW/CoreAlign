# CoreAlign — B2B / Dealer Portal

Dealer-facing SPA for the CoreAlign multi-tenant ERP. Dealers create orders on behalf of their customers, route them through the 3-way approval flow, manage their customer list, track payments, and view the price book they're entitled to.

## Tech

- React 19 + Vite 7 + TypeScript 5.9
- Tailwind CSS v4
- TanStack Query (server state) + Zustand (UI state)
- React Router 7
- i18next (TR primary; EN/AR/DE/RU stubs)
- Sonner toast notifications
- Zod schemas for form validation
- React Hook Form

## Persona

`dealer` — JWT claim issued by the API. Routes are protected by `PersonaAuthorizationPolicies` server-side and by a route guard client-side. Dealers see only the customers attached to their dealer account (multi-tenant filter + dealer scope).

## Local dev

```bash
# From the repo root
cp apps/b2b/.env.example apps/b2b/.env
npm ci
npm run dev:b2b
```

The dev server proxies API calls to `VITE_API_URL` (default `http://localhost:5178`).

## Build

```bash
npm --workspace apps/b2b run build
```

Output: `apps/b2b/dist/`. Served by nginx in the container image (`apps/b2b/Dockerfile`).

## Project structure

```
apps/b2b/
├── public/             # static assets (favicon, manifest, icons)
├── src/
│   ├── app/            # router, providers, layout
│   ├── features/       # feature folders (orders, customers, pricebook, ...)
│   ├── pages/          # page-level components
│   ├── shared/         # cross-feature components, hooks, api client
│   └── widgets/        # composite UI blocks
├── Dockerfile          # multi-stage node 22 build → nginx:alpine
├── nginx.conf          # SPA fallback + cache headers + gzip
└── vite.config.ts
```

See the root [`README.md`](../../README.md) for the full project overview, secret inventory, and deploy notes.
