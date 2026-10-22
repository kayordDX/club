# Club Front End

## Toolchain

Uses SvelteKit 3, configured in `vite.config.ts`, with `#lib/*` subpath imports.
The migration follows the [official SvelteKit 3 guide](https://svelte.dev/docs/kit/migrating-to-sveltekit-3).
Node 22.17+ is required (the container uses Node 24). TypeScript stays on 6.0.3:
TypeScript 7 is not yet supported by SvelteKit, svelte-check, or typescript-eslint.
Other direct dependencies are updated to their latest stable releases.

Run `pnpm install`, `pnpm check`, `pnpm lint`, `pnpm exec vitest run`, and `pnpm build`.
Check service-worker types separately with `pnpm exec tsc -p src/service-worker/tsconfig.json --noEmit`.
`pnpm test:e2e:payments` uses an isolated fixture; the full E2E suite needs the application stack.

`pnpm api:remote` uses `scripts/gen-remote.mjs` to preserve SvelteKit 3 subpath imports.

Behind a trusted reverse proxy, configure adapter-node `PROTOCOL_HEADER` and `HOST_HEADER`
(and ensure the proxy overwrites those headers) so request origins reflect the public URL.
SvelteKit 3 no longer uses adapter-node `ORIGIN`; a fixed build-time origin can instead be
configured with `paths.origin` in `vite.config.ts`.

### Icons

https://studio.lucide.dev/
https://simpleicons.org/
https://tablericons.com/
https://tabler.io/icons
