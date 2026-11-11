# Build context: front/ (the pnpm workspace root)
#   docker build -f front/website/Dockerfile -t trainera-web:latest ./front

# =========================
# 1. BUILD STAGE
# =========================
FROM node:22-alpine AS builder
WORKDIR /app
RUN corepack enable

# NEXT_PUBLIC_* values are inlined into the static export at build time
ARG NEXT_PUBLIC_API_URL
ARG NEXT_PUBLIC_SITE_URL
ARG NEXT_PUBLIC_LAUNCH_STATE
ARG NEXT_PUBLIC_COOKIEBOT_ID
ENV NEXT_PUBLIC_API_URL=$NEXT_PUBLIC_API_URL
ENV NEXT_PUBLIC_SITE_URL=$NEXT_PUBLIC_SITE_URL
ENV NEXT_PUBLIC_LAUNCH_STATE=$NEXT_PUBLIC_LAUNCH_STATE
ENV NEXT_PUBLIC_COOKIEBOT_ID=$NEXT_PUBLIC_COOKIEBOT_ID

COPY . .
# Install only this package and its workspace dependencies (skips the Expo app)
RUN pnpm install --frozen-lockfile --filter website...
RUN pnpm --filter website build

# =========================
# 2. RUNTIME STAGE
# =========================
# output: "export" produces a static site; serve exposes it and the server's nginx proxies to it
FROM node:22-alpine AS runner
WORKDIR /app
RUN npm install -g serve@14
COPY --from=builder /app/website/out ./out

EXPOSE 3000

CMD ["serve", "out", "-l", "3000"]
