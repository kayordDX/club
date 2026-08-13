import { defineConfig } from "@playwright/test";

export default defineConfig({
	testDir: "e2e",
	globalSetup: "./e2e/global-setup.ts",
	use: {
		baseURL: process.env.E2E_BASE_URL ?? "http://localhost:5173",
	},
});
