FROM node:24-slim AS deps
WORKDIR /app
RUN corepack enable && corepack prepare pnpm@11.19.0 --activate
COPY package.json pnpm-lock.yaml pnpm-workspace.yaml .npmrc ./
RUN pnpm install --prod --frozen-lockfile

FROM node:24-slim AS build
WORKDIR /app
RUN corepack enable && corepack prepare pnpm@11.19.0 --activate

# API_URL / APP_URL / IDENTITY_URL / SESSION_SECRET are RUNTIME config — the server
# reads them from process.env at startup and they are NOT inlined into the bundle.
RUN printf '%s\n' \
        'API_URL=http://localhost:5000' \
        'APP_URL=http://localhost' \
        'IDENTITY_URL=http://localhost/realms/kayord' \
        'SESSION_SECRET=build-time-stub-superseded-by-runtime-env-0123456789' \
        > .env

COPY package.json pnpm-lock.yaml pnpm-workspace.yaml .npmrc ./
RUN pnpm install --frozen-lockfile

COPY . .
RUN pnpm build

FROM node:24-slim AS runtime
WORKDIR /app

COPY --from=deps /app/node_modules ./node_modules
COPY --from=build /app/build ./build
COPY package.json ./

ENV NODE_ENV=production
# adapter-node binds to HOST:PORT. ORIGIN should be set at deploy time to the
# public URL (e.g. https://club.example.com) so the server can validate requests.
ENV HOST=0.0.0.0
ENV PORT=3000
EXPOSE 3000

CMD ["node", "build"]
