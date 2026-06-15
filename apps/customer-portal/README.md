# CoreAlign — Customer Portal

Customer-facing SPA for the CoreAlign multi-tenant ERP. Lets end customers view their dealer-issued orders, approve or reject quotes, track shipments, raise comments, and read invoices.

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

`customer` — JWT claim issued by the API. Routes are protected by `PersonaAuthorizationPolicies` server-side and by a route guard client-side.

## Local dev

```bash
# From the repo root
cp apps/customer-portal/.env.example apps/customer-portal/.env
npm ci
npm run dev:customer
```

The dev server proxies API calls to `VITE_API_URL` (default `http://localhost:5178`).

## Build

```bash
npm --workspace apps/customer-portal run build
```

Output: `apps/customer-portal/dist/`. Served by nginx in the container image (`apps/customer-portal/Dockerfile`).

## Project structure

```
apps/customer-portal/
├── public/             # static assets (favicon, manifest, icons)
├── src/
│   ├── app/            # router, providers, layout
│   ├── features/       # feature folders (orders, invoices, comments, ...)
│   ├── pages/          # page-level components
│   ├── shared/         # cross-feature components, hooks, api client
│   └── widgets/        # composite UI blocks
├── Dockerfile          # multi-stage node 22 build → nginx:alpine
├── nginx.conf          # SPA fallback + cache headers + gzip
└── vite.config.ts
```

See the root [`README.md`](../../README.md) for the full project overview, secret inventory, and deploy notes.
