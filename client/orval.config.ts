import { defineConfig } from "orval";

export default defineConfig({
	// Client-accessible types/enums (api.schemas.ts). The per-tag fetch functions
	// are unused — the app uses SvelteKit remote functions for all fetching — but
	// this client emits the shared TS types/enums (BookingStatusEnum, ...) without
	// needing @tanstack/svelte-query.
	api: {
		input: "./swagger.json",
		output: {
			mode: "tags",
			workspace: "./src/lib/api/generated",
			target: "api.ts",
			client: "fetch",
			prettier: true,
			headers: false,
			clean: true,
			// Mutator deliberately omitted: generated fetch functions are dead code.
		},
	},
	// Server-side typed fetch (transport) consumed by .remote.ts.
	serverApi: {
		input: "./swagger.json",
		output: {
			mode: "tags",
			workspace: "./src/lib/server/api/generated",
			target: "server.ts",
			client: "fetch",
			prettier: true,
			headers: false,
			clean: true,
			override: {
				fetch: {
					includeHttpResponseReturnType: false,
				},
				mutator: {
					path: "../client.ts",
					name: "customServerInstance",
				},
			},
		},
	},
	// Zod validation schemas used by remote functions.
	zodSchemas: {
		input: "./swagger.json",
		output: {
			mode: "tags",
			workspace: "./src/lib/server/api/schemas",
			target: "schemas.ts",
			client: "zod",
			prettier: true,
			headers: false,
			clean: true,
		},
	},
});
