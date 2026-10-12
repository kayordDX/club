import type { EventEmitter } from "node:events";
import { test, expect } from "./payment-test";
import { paymentApiUrl } from "./payment-session";

async function completed(testInfo: import("@playwright/test").TestInfo) {
	await testInfo.attach("probe-body-completed", { body: "Business assertions passed", contentType: "text/plain" });
}

test("browser exception", async ({ page }, testInfo) => {
	await page.goto("/robots.txt");
	await page.evaluate(() =>
		setTimeout(() => {
			throw new Error("probe-browser-exception");
		}, 0)
	);
	await page.waitForTimeout(100);
	await completed(testInfo);
});

test("browser rejection", async ({ page }, testInfo) => {
	await page.goto("/robots.txt");
	await page.evaluate(() => {
		void Promise.reject(new Error("probe-browser-rejection"));
	});
	await page.waitForTimeout(100);
	await completed(testInfo);
});

test("additional page exception", async ({ context }, testInfo) => {
	const extra = await context.newPage();
	await extra.goto("/robots.txt");
	await extra.evaluate(() =>
		setTimeout(() => {
			throw new Error("probe-extra-page");
		}, 0)
	);
	await extra.waitForTimeout(100);
	await completed(testInfo);
});

test("fixture exception", async ({ request }, testInfo) => {
	const response = await request.post(`${paymentApiUrl}/_test/reset`, {
		data: "{",
		headers: { "content-type": "application/json" },
	});
	expect(response.status()).toBe(500);
	await completed(testInfo);
});

test("context shutdown exception", async ({ page, context }, testInfo) => {
	await page.goto("/robots.txt");
	// Inject at the lifecycle boundary rather than waiting for an error in the test body.
	context.on("close", () => (page as unknown as EventEmitter).emit("pageerror", new Error("probe-context-close")));
	await completed(testInfo);
});

test("warnings and handled negative responses", async ({ page, request }, testInfo) => {
	await page.goto("/robots.txt");
	await page.evaluate(() => {
		console.warn("probe-warning");
		console.error("probe-handled-console-message");
	});
	const response = await request.get(`${paymentApiUrl}/booking/123`);
	expect(response.status()).toBe(401);
	await completed(testInfo);
});

test("server crash retains diagnostics", async ({ page, request }, testInfo) => {
	await page.goto("/robots.txt");
	const response = await request.post(`${paymentApiUrl}/_test/crash-vite`);
	expect(response.ok()).toBe(true);
	await page.waitForTimeout(100);
	await completed(testInfo);
});
