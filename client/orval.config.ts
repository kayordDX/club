import { defineConfig } from "orval";

export default defineConfig({
	iot: {
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
				mutator: {
					path: "../mutator/customInstance.svelte.ts",
					name: "customInstance",
				},
				query: {
					signal: false,
				},
			},
		},
	},
});
