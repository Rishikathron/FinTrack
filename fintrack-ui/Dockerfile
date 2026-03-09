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

# Copy nginx config template
COPY --from=build /app/nginx.conf /etc/nginx/nginx.conf.template

# Default API_URL for Docker Compose (overridden by Railway env var)
ENV API_URL=http://api:8080

EXPOSE 80

# At startup: substitute only API_URL in the template, then start nginx
CMD ["/bin/sh", "-c", "envsubst '${API_URL}' < /etc/nginx/nginx.conf.template > /etc/nginx/conf.d/default.conf && nginx -g 'daemon off;'"]
