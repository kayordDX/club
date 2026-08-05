import { defineConfig } from "orval";

export default defineConfig({
	// Existing client: svelte-query hooks consumed in the browser.
	api: {
		input: "./swagger.json",
		output: {
			mode: "tags",
			workspace: "./src/lib/api/generated",
			target: "api.ts",
			client: "svelte-query",
			prettier: true,
			headers: false,
			clean: true,
			override: {
				fetch: {
					includeHttpResponseReturnType: false,
				},
				mutator: {
					path: "../mutator/customInstance.svelte.ts",
					name: "customInstance",
				},
			},
		},
	},
	// NEW: plain typed fetch functions for the SvelteKit server (consumed by
	// .remote.ts). Same swagger.json, different client + server-side mutator.
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
});
