import { defineConfig } from "@playwright/test";
import { fileURLToPath } from "node:url";

export default defineConfig({
	testDir: "e2e",
	testMatch: /(?:booking-payments|admin-booking-creation|payment-results)\.spec\.ts/,
	globalTeardown: fileURLToPath(new URL("./e2e/fixtures/payment-teardown.ts", import.meta.url)),
	workers: 1,
	timeout: 60000,
	expect: { timeout: 15000 },
	reporter: [["list"], ["html", { outputFolder: "playwright-report/payments", open: "never" }]],
	outputDir: "test-results/payments",
	use: {
		baseURL: "http://127.0.0.1:5174",
		trace: "retain-on-failure",
		screenshot: "only-on-failure",
	},
	webServer: {
		cwd: fileURLToPath(new URL(".", import.meta.url)),
		command: "node e2e/fixtures/payment-app.mjs",
		url: "http://127.0.0.1:5174/robots.txt",
		reuseExistingServer: false,
		timeout: 120000,
	},
});
