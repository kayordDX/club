import { createCipheriv, createHash, randomBytes } from "node:crypto";
import { expect, test, type Page } from "@playwright/test";

const scheduleUrl = "/outlet/test/1/admin/schedule?date=2026-08-02";
const apiUrl = "http://127.0.0.1:5194";

async function authenticate(page: Page, owner = false) {
	const iv = randomBytes(12);
	const cipher = createCipheriv("aes-256-gcm", createHash("sha256").update("isolated-payment-playwright-secret").digest(), iv);
	const payload = JSON.stringify({
		user: owner
			? { sub: "00000000-0000-4000-8000-000000000099", username: "casey", name: "Casey Customer", email: "casey@example.test" }
			: { sub: "payment-tester", username: "payment-tester", name: "Payment Tester", email: "tester@example.test" },
		tokens: { accessToken: owner ? "customer-test-token" : "payment-test-token", expiresAt: Date.now() + 3600000 },
	});
	const encrypted = Buffer.concat([cipher.update(payload, "utf8"), cipher.final()]);
	await page.context().addCookies([
		{
			name: "sid",
			value: `${iv.toString("base64url")}.${encrypted.toString("base64url")}.${cipher.getAuthTag().toString("base64url")}`,
			url: "http://127.0.0.1:5174",
			httpOnly: true,
			sameSite: "Lax",
		},
	]);
}

test("an empty customer lookup cannot select an owner or create a booking", async ({ page }) => {
	await page.request.post(`${apiUrl}/_test/reset`, { data: { scenario: "admin-creation" } });
	await authenticate(page);
	await page.goto("/outlet/test/1/admin/bookings/new?slotId=00000000-0000-4000-8000-000000000001&date=2026-08-02&slotCount=1");
	await page.getByLabel("Search customers").fill("absent@example.test");
	await expect(page.getByText("No registered customer found. Check the email or phone number.")).toBeVisible();
	await expect(page.getByRole("button", { name: "Select customer", exact: true })).toBeDisabled();
	await expect(page.getByRole("button", { name: "Initial booking status" })).toHaveCount(0);
	const state = await (await page.request.get(`${apiUrl}/_test/state`)).json();
	expect(state.calls.some((call: { path: string; method: string }) => call.path === "/admin/facility/1/booking" && call.method === "POST")).toBe(false);
});

test("admin schedule creates a confirmed customer-owned booking and opens admin details", async ({ page }) => {
	await page.request.post(`${apiUrl}/_test/reset`, { data: { scenario: "admin-creation" } });
	await authenticate(page);
	await page.goto(scheduleUrl);

	await page.getByRole("button", { name: "Create booking", exact: true }).click();
	await expect(page).toHaveURL(/\/admin\/bookings\/new\?/);
	await page.getByLabel("Search customers").fill("casey@example.test");
	await page.getByRole("button", { name: /Casey Customer.*casey@example.test/ }).click();
	await expect(page.getByRole("button", { name: "Initial booking status" })).toHaveText("Confirmed");
	await page.getByLabel("Name").fill("Player One");
	await page.getByLabel("Cell No").fill("0821234567");
	await page.getByLabel("Email").fill("player@example.test");
	await page.getByRole("button", { name: "Contract" }).click();
	await page.getByRole("option", { name: /Visitor Standard - R 100.00/ }).click();
	await page.getByRole("button", { name: "Create booking" }).click();

	await expect(page).toHaveURL(/\/admin\/bookings\/44$/);
	await expect(page.getByText("Booking #44", { exact: true })).toBeVisible();
	await expect(page.getByText("Casey Customer", { exact: true })).toBeVisible();
	const state = await (await page.request.get(`${apiUrl}/_test/state`)).json();
	const creation = state.calls.find((call: { path: string; method: string }) => call.path === "/admin/facility/1/booking" && call.method === "POST");
	expect(creation.body).toEqual({
		userId: "00000000-0000-4000-8000-000000000099",
		bookingStatusId: 2,
		bookings: [
			{ slotId: "00000000-0000-4000-8000-000000000001", slotContractId: 31, name: "Player One", cellphone: "0821234567", email: "player@example.test" },
		],
		extras: [],
	});

	await page.context().clearCookies();
	await authenticate(page, true);
	await page.goto("/bookings");
	await expect(page.getByRole("cell", { name: "44", exact: true })).toBeVisible();
	await expect(page.getByText("Test Course", { exact: true })).toBeVisible();
	const ownerState = await (await page.request.get(`${apiUrl}/_test/state`)).json();
	const ownerCalls = ownerState.calls.filter(
		(call: { path: string; authorization: string }) => call.path === "/booking/user" && call.authorization === "Bearer customer-test-token"
	);
	expect(ownerCalls.length).toBeGreaterThan(0);
});
