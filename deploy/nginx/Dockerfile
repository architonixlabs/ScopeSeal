# Multi-stage nginx edge: bakes static Angular builds + nginx config into the image so
# deploy over an SSH Docker context does not rely on host-side bind mounts for config.
FROM node:22-alpine AS clients
WORKDIR /app
COPY src/clients/package*.json ./
RUN npm ci
COPY src/clients/ ./
RUN npm run build:product && npm run build:admin

FROM nginx:alpine
COPY deploy/nginx/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=clients /app/dist/product-app/browser /usr/share/nginx/product
COPY --from=clients /app/dist/admin-portal/browser /usr/share/nginx/admin
