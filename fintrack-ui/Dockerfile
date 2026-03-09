# ── Build stage ──
FROM node:22-alpine AS build
WORKDIR /app

COPY package*.json ./
RUN npm ci

COPY . .
RUN npx ng build --configuration production

# ── Serve stage (nginx) ──
FROM nginx:alpine AS final

# Copy built Angular app
COPY --from=build /app/dist/fintrack-ui/browser /usr/share/nginx/html

# Custom nginx config to proxy /api to the backend
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
