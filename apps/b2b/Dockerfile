# syntax=docker/dockerfile:1.7

ARG BUILD_VERSION=0.0.0-dev

FROM node:22-alpine AS build
WORKDIR /repo

COPY package.json package-lock.json ./
COPY apps/customer-portal/package.json apps/customer-portal/
COPY apps/b2b/package.json apps/b2b/

RUN npm ci --no-audit --no-fund

COPY apps/b2b/ apps/b2b/

WORKDIR /repo/apps/b2b
RUN npm run build

FROM nginx:alpine AS runtime

ARG BUILD_VERSION
LABEL org.opencontainers.image.source="https://github.com/corealign/corealign" \
      org.opencontainers.image.version="${BUILD_VERSION}" \
      org.opencontainers.image.title="CoreAlign B2B Portal" \
      org.opencontainers.image.description="CoreAlign dealer/B2B-facing SPA"

COPY apps/b2b/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /repo/apps/b2b/dist /usr/share/nginx/html

EXPOSE 80

USER nginx

CMD ["nginx", "-g", "daemon off;"]
