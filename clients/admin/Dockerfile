# syntax=docker/dockerfile:1.7

# Stage 1: build the SPA bundle
FROM node:22-alpine AS build
WORKDIR /app

# Install deps with a cached layer
COPY package.json package-lock.json ./
RUN npm ci

# Build
COPY . .
RUN npm run build

# Stage 2: serve via nginx
FROM nginx:alpine AS runtime
WORKDIR /usr/share/nginx/html

# nginx:alpine needs `envsubst` from gettext (it ships busybox without it).
RUN apk add --no-cache gettext

# Replace the default nginx site
RUN rm -rf ./* /etc/nginx/conf.d/default.conf
COPY docker/nginx.conf /etc/nginx/conf.d/default.conf

# Copy the built bundle, the runtime config template, and the entrypoint
COPY --from=build /app/dist/ ./
COPY docker/config.json.template ./config.json.template
COPY docker/docker-entrypoint.sh /docker-entrypoint.sh
RUN chmod +x /docker-entrypoint.sh

# Non-root: nginx:alpine includes the `nginx` user (uid 101).
# We do NOT switch to it here because the upstream image's master process
# needs root to bind :80; nginx will fork workers as the `nginx` user on
# its own per its default config.

EXPOSE 80
ENTRYPOINT ["/docker-entrypoint.sh"]
