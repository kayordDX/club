import adapter from "@sveltejs/adapter-node";
import { vitePreprocess } from "@sveltejs/vite-plugin-svelte";

import tailwindcss from "@tailwindcss/vite";
import { sveltekit } from "@sveltejs/kit/vite";
import { defineConfig } from "vitest/config";
import { playwright } from "@vitest/browser-playwright";

export default defineConfig({
	plugins: [
		tailwindcss(),
		sveltekit({
			preprocess: vitePreprocess(),
			compilerOptions: { experimental: { async: true } },
			inspector: { showToggleButton: "never" },
			adapter: adapter(),
			experimental: { remoteFunctions: true },
		}),
	],
	// Isolated E2E already records browser console diagnostics separately from unhandled errors.
	server: { forwardConsole: process.env.PAYMENT_ERROR_LOG ? { logLevels: [], unhandledErrors: true } : undefined },
	ssr: { noExternal: ["@tanstack/svelte-query"] },
	test: {
		expect: { requireAssertions: true },
		projects: [
			{
				extends: "./vite.config.ts",
				test: {
					name: "client",
					browser: {
						enabled: true,
						provider: playwright(),
						instances: [{ browser: "chromium" }],
						headless: true,
					},
					include: ["src/**/*.svelte.{test,spec}.{js,ts}"],
					exclude: ["src/lib/server/**"],
					setupFiles: ["./vitest-setup-client.ts"],
				},
			},
			{
				extends: "./vite.config.ts",
				test: {
					name: "server",
					environment: "node",
					// Deterministic timezone for date/time formatters (CI runs UTC too).
					env: { TZ: "UTC" },
					include: ["src/**/*.{test,spec}.{js,ts}"],
					exclude: ["src/**/*.svelte.{test,spec}.{js,ts}"],
				},
			},
		],
	},
});
