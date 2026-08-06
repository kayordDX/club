import adapter from "@sveltejs/adapter-node";
import { vitePreprocess } from "@sveltejs/vite-plugin-svelte";

/** @type {import('@sveltejs/kit').Config} */
const config = {
	preprocess: vitePreprocess(),
	kit: {
		alias: {
			$lib: "./src/lib",
		},
		// Switched from adapter-static (SPA) to adapter-node (SSR server / BFF).
		// Remote functions ($app/server) require a running server.
		adapter: adapter(),
		experimental: {
			// Enables .remote.ts modules (query/command/form/prerender).
			remoteFunctions: true,
		},
	},
	// Svelte compiler experimental: await expressions in components, used with
	// <svelte:boundary> for remote-function data loading.
	compilerOptions: {
		experimental: {
			async: true,
		},
	},
	vitePlugin: {
		inspector: {
			showToggleButton: "never",
		},
	},
};

export default config;
