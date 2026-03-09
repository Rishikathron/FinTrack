# ── Build stage ──
FROM node:22-alpine AS build
WORKDIR /app

COPY fintrack-ui/package*.json ./
RUN npm ci

COPY fintrack-ui/ .
RUN npx ng build --configuration production

# ── Serve stage (nginx) ──
FROM nginx:alpine AS final

# Copy built Angular app
COPY --from=build /app/dist/fintrack-ui/browser /usr/share/nginx/html

# Copy nginx config as a template (contains ${API_URL} placeholder)
COPY --from=build /app/nginx.conf /etc/nginx/templates/default.conf.template

# Default API_URL for Docker Compose (overridden by Railway env var)
ENV API_URL=http://api:8080

# Only substitute API_URL — leave nginx variables ($host, $uri, etc.) untouched
ENV NGINX_ENVSUBST_FILTER=API_URL

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
