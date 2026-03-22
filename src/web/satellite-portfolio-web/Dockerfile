FROM node:22-bookworm-slim AS deps
WORKDIR /app

COPY src/web/satellite-portfolio-web/package*.json ./
RUN npm ci

FROM node:22-bookworm-slim AS builder
WORKDIR /app

COPY --from=deps /app/node_modules ./node_modules
COPY src/web/satellite-portfolio-web ./

RUN npm run build

FROM node:22-bookworm-slim AS runner
WORKDIR /app
ENV NODE_ENV=production
ENV PORT=3000
EXPOSE 3000

COPY src/web/satellite-portfolio-web/package*.json ./
RUN npm ci --omit=dev

COPY --from=builder /app/.next ./.next
COPY --from=builder /app/public ./public
COPY --from=builder /app/next.config.ts ./next.config.ts

ENTRYPOINT ["npm", "run", "start"]
