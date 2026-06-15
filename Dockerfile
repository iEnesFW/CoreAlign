# syntax=docker/dockerfile:1.7

ARG BUILD_VERSION=0.0.0-dev

FROM node:22-alpine AS build
WORKDIR /repo

COPY package.json package-lock.json ./
COPY apps/customer-portal/package.json apps/customer-portal/
COPY apps/b2b/package.json apps/b2b/

RUN npm ci --no-audit --no-fund

COPY index.html ./
COPY tsconfig.json tsconfig.app.json tsconfig.node.json vite.config.ts eslint.config.js ./
COPY public/ public/
COPY src/ src/

RUN npm run build

FROM nginx:alpine AS runtime

ARG BUILD_VERSION
LABEL org.opencontainers.image.source="https://github.com/corealign/corealign" \
      org.opencontainers.image.version="${BUILD_VERSION}" \
      org.opencontainers.image.title="CoreAlign Tenant Admin" \
      org.opencontainers.image.description="CoreAlign tenant-admin SPA"

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /repo/dist /usr/share/nginx/html

EXPOSE 80

USER nginx

CMD ["nginx", "-g", "daemon off;"]
