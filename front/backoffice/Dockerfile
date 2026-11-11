# Build context: front/ (the pnpm workspace root)
#   docker build -f front/backoffice/Dockerfile -t trainera-app:latest ./front

# =========================
# 1. BUILD STAGE
# =========================
FROM node:22-alpine AS builder
WORKDIR /app
RUN corepack enable

# NEXT_PUBLIC_* values are inlined into the static export at build time
ARG NEXT_PUBLIC_API_URL
ENV NEXT_PUBLIC_API_URL=$NEXT_PUBLIC_API_URL

COPY . .
# Install only this package and its workspace dependencies (skips the Expo app)
RUN pnpm install --frozen-lockfile --filter backoffice...
RUN pnpm --filter backoffice build

# =========================
# 2. RUNTIME STAGE
# =========================
# output: "export" produces a static site; serve exposes it and the server's nginx proxies to it
FROM node:22-alpine AS runner
WORKDIR /app
RUN npm install -g serve@14
COPY --from=builder /app/backoffice/out ./out

EXPOSE 3000

# -s falls back to index.html for client-side routes
CMD ["serve", "-s", "out", "-l", "3000"]
