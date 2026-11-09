# bud-tack-portal — the Angular files behind nginx (ADR-006).
# Build:  docker build -t bud-tack-portal:$(git rev-parse --short HEAD) .

FROM node:22-alpine AS build
WORKDIR /source
COPY package.json package-lock.json ./
RUN npm ci --no-audit --no-fund
COPY . .
RUN npx ng build --configuration production

FROM nginxinc/nginx-unprivileged:1.29-alpine AS runtime
# Where /api/ and /health go; bud-tack-infra's Compose names the API service `api`.
ENV API_ORIGIN=http://api:3000 \
    NGINX_ENTRYPOINT_LOCAL_RESOLVERS=1
COPY nginx/default.conf.template /etc/nginx/templates/default.conf.template
COPY --from=build /source/dist/bud-tack-portal/browser /usr/share/nginx/html
# Port 8080 as the image's non-root user; bud-tack-infra maps it to the host.
EXPOSE 8080
