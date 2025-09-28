import { defineConfig } from "@hey-api/openapi-ts";

export default defineConfig({
	input: "./swagger.json",
	output: {
		path: "./src/lib/api/generated",
	},
	plugins: [
		{
			name: "@hey-api/client-fetch",
			runtimeConfigPath: "$lib/api/hey-api",
		},
		"@tanstack/svelte-query",
		"zod",
	],
});
