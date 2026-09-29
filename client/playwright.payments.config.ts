import { defineConfig } from "@playwright/test";

export default defineConfig({
	testDir: "e2e",
	testMatch: "booking-payments.spec.ts",
	workers: 1,
	timeout: 60000,
	expect: { timeout: 15000 },
	use: { baseURL: "http://127.0.0.1:5174" },
	webServer: {
		command: "node e2e/fixtures/payment-app.mjs",
		url: "http://127.0.0.1:5174",
		reuseExistingServer: false,
		timeout: 120000,
	},
});
