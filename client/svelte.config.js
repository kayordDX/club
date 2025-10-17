import adapter from "@sveltejs/adapter-auto";
import { vitePreprocess } from "@sveltejs/vite-plugin-svelte";

/** @type {import('@sveltejs/kit').Config} */
const config = {
	preprocess: vitePreprocess(),
	kit: {
		alias: {
			$lib: "./src/lib",
		},
		adapter: adapter({
			fallback: "index.html",
		}),
		experimental: {
			remoteFunctions: true,
		},
	},
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
