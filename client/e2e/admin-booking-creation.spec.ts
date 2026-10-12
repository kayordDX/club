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

async function createForMove(page: Page, scenario = "admin-creation") {
	await page.request.post(`${apiUrl}/_test/reset`, { data: { scenario } });
	await authenticate(page);
	await page.goto(scheduleUrl);
	await page.getByRole("button", { name: "Create booking", exact: true }).click();
	await page.getByLabel("Search customers").fill("casey@example.test");
	await page.getByRole("button", { name: /Casey Customer.*casey@example.test/ }).click();
	await page.getByLabel("Name").fill("Player One");
	await page.getByLabel("Cell No").fill("0821234567");
	await page.getByLabel("Email").fill("player@example.test");
	await page.getByRole("button", { name: "Contract" }).click();
	await page.getByRole("option", { name: /Visitor Standard - R 100.00/ }).click();
	await page.getByRole("button", { name: "Add Extra" }).click();
	await page.getByRole("dialog").getByText("Cart", { exact: true }).click();
	await page.getByRole("button", { name: "Create booking", exact: true }).click();
	await expect(page).toHaveURL(/\/admin\/bookings\/44$/);
}

async function selectDestination(page: Page) {
	await page.getByRole("button", { name: "Edit booking", exact: true }).click();
	await page.getByLabel("Name", { exact: true }).fill("Retained Draft");
	await page.getByLabel("Cell No").fill("0839999999");
	await page.getByRole("button", { name: "Next day" }).click();
	await page.getByRole("button", { name: "Destination slot", exact: true }).click();
	await page.getByRole("option", { name: /Court A/ }).click();
	await expect(page.getByText("Correct selections before saving")).toBeVisible();
	await expect(page.getByLabel("Name", { exact: true })).toHaveValue("Retained Draft");
	await expect(page.getByLabel("Cell No")).toHaveValue("0839999999");
	await expect(page.getByRole("button", { name: "Save changes" })).toBeDisabled();
	await page.getByRole("button", { name: "Contract", exact: true }).click();
	await page.getByRole("option", { name: /Visitor Standard - R 150.00/ }).click();
	await expect(page.getByRole("button", { name: "Save changes" })).toBeDisabled();
	await page.getByRole("button", { name: "Remove extra Cart" }).click();
	await expect(page.getByLabel("Booking review").getByText("R 150,00")).toBeVisible();
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

test("creation → move retains drafts, requires corrections, previews prices and refreshes both days", async ({ page }) => {
	await createForMove(page);
	await selectDestination(page);
	await page.getByRole("button", { name: "Save changes" }).click();
	await expect(page.getByRole("button", { name: "Edit booking", exact: true })).toBeEnabled();
	await expect(page.getByText("Retained Draft", { exact: true })).toBeVisible();
	const state = await (await page.request.get(`${apiUrl}/_test/state`)).json();
	const update = state.calls.find((call: { method: string; path: string }) => call.method === "PUT" && call.path === "/admin/facility/1/booking/44");
	expect(update.body).toEqual({
		bookings: [
			{ slotId: "00000000-0000-4000-8000-000000000002", slotContractId: 41, name: "Retained Draft", cellphone: "0839999999", email: "player@example.test" },
		],
		extras: [],
	});
	const afterSave = state.calls.slice(state.calls.indexOf(update) + 1);
	for (const path of ["/admin/facility/1/slot", "/slot"]) {
		for (const date of ["2026-08-02", "2026-08-03"]) {
			expect(afterSave.some((call: { path: string; query: string }) => call.path === path && call.query.includes(date))).toBe(true);
		}
	}
	const optionsCalls = state.calls.filter((call: { path: string }) => call.path === "/admin/facility/1/booking/options");
	expect(optionsCalls.every((call: { query: string }) => call.query.includes("userId=00000000-0000-4000-8000-000000000099"))).toBe(true);
	await page.goto("/outlet/test/1/admin/schedule?date=2026-08-03");
	await expect(page.getByRole("link", { name: /#44/ })).toBeVisible();
	await page.getByRole("link", { name: /#44/ }).click();
	await expect(page).toHaveURL(/\/admin\/bookings\/44$/);
});

test("schedule → select customer → Confirmed unpaid → edit/move → checkout → admin return", async ({ page }) => {
	let phase = "creation";
	const runtimeErrors: string[] = [];
	page.on("pageerror", (error) => {
		runtimeErrors.push(`${phase}: ${error.message}`);
	});
	await createForMove(page);
	let state = await (await page.request.get(`${apiUrl}/_test/state`)).json();
	const creation = state.calls.find((call: { path: string; method: string }) => call.path === "/admin/facility/1/booking" && call.method === "POST");
	expect(creation.body.userId).toBe("00000000-0000-4000-8000-000000000099");
	expect(creation.body.bookingStatusId).toBe(2);
	const paymentRegion = page.getByRole("region", { name: "Booking payments" });
	await expect(paymentRegion.getByLabel("Payment amount")).toHaveValue("125.00");
	phase = "move";
	await selectDestination(page);
	await page.getByRole("button", { name: "Save changes" }).click();
	await expect(page.getByRole("button", { name: "Edit booking", exact: true })).toBeEnabled();
	await expect(page.getByText("Retained Draft", { exact: true })).toBeVisible();
	await expect(paymentRegion.getByLabel("Payment amount")).toHaveValue("150.00");
	phase = "checkout";
	await paymentRegion.getByRole("radio", { name: "Payfast" }).click();
	await paymentRegion.getByLabel("Payment amount").fill("50");
	await paymentRegion.getByRole("button", { name: "Pay now" }).click();
	await expect(page).toHaveURL(/\/payment\/success\?transactionId=tx-1$/);
	await page.request.post(`${apiUrl}/_test/callback`, { data: { success: true } });
	await page.reload();
	await expect(page.getByText(/Payment received\. Remaining balance/)).toContainText("100,00");
	phase = "admin return";
	await page.getByRole("link", { name: "Back to booking management" }).click();
	await expect(page).toHaveURL(/\/admin\/bookings\/44$/);
	await expect(page.getByText("Retained Draft", { exact: true })).toBeVisible();
	await expect(paymentRegion.getByLabel("Payment amount")).toHaveValue("100.00");
	await expect(paymentRegion.getByRole("cell", { name: "Partial", exact: true })).toBeVisible();
	state = await (await page.request.get(`${apiUrl}/_test/state`)).json();
	const checkout = state.calls.find((call: { path: string }) => call.path === "/admin/facility/1/booking/44/payment/initiate");
	expect(checkout.body).toEqual({ providerName: "payfast", amount: 50 });
	const latestDetails = await (
		await page.request.get(`${apiUrl}/admin/facility/1/booking/44`, { headers: { authorization: "Bearer payment-test-token" } })
	).json();
	expect(latestDetails.bookingStatusId).toBe(2);
	expect(latestDetails.amountPaid).toBe(50);
	expect(latestDetails.amountOutstanding).toBe(100);
	expect(latestDetails.slotContractBookings[0].slotId).toBe("00000000-0000-4000-8000-000000000002");
	expect(state.calls.some((call: { path: string }) => call.path.startsWith("/payment/booking/") || call.path === "/payment/voucher")).toBe(false);
	phase = "customer view";
	await page.context().clearCookies();
	await authenticate(page, true);
	await page.goto("/outlet/test/1/booking/44/pay");
	await expect(page.getByText("Your booking is confirmed.", { exact: true })).toBeVisible();
	await expect(paymentRegion.getByLabel("Payment amount")).toHaveValue("100.00");
	await expect(paymentRegion.getByRole("cell", { name: "Partial", exact: true })).toBeVisible();
	expect(runtimeErrors).toEqual([]);
});

test("failed moves retain drafts for retry, while cancel discards them without saving", async ({ page }) => {
	await createForMove(page, "admin-move-save-error");
	await selectDestination(page);
	await page.getByRole("button", { name: "Save changes" }).click();
	await expect(page.getByText("Changes not saved", { exact: true })).toBeVisible();
	await expect(page.getByLabel("Name", { exact: true })).toHaveValue("Retained Draft");
	await expect(page.getByLabel("Booking review").getByText("R 150,00")).toBeVisible();
	await page.getByRole("button", { name: "Save changes" }).click();
	await expect(page.getByRole("button", { name: "Edit booking", exact: true })).toBeEnabled();
	await page.getByRole("button", { name: "Edit booking", exact: true }).click();
	await page.getByLabel("Name", { exact: true }).fill("Discard this draft");
	await page.getByRole("button", { name: "Cancel", exact: true }).click();
	await expect(page.getByText("Retained Draft", { exact: true })).toBeVisible();
	await page.getByRole("button", { name: "Edit booking", exact: true }).click();
	await expect(page.getByLabel("Name", { exact: true })).toHaveValue("Retained Draft");
	const state = await (await page.request.get(`${apiUrl}/_test/state`)).json();
	expect(state.calls.filter((call: { method: string }) => call.method === "PUT")).toHaveLength(2);
	await expect(page.getByRole("link", { name: "Back to bookings", exact: true })).toHaveAttribute("href", "/outlet/test/1/admin/bookings");
});

test("a saved move with failed refresh blocks duplicate mutation and retries refresh only", async ({ page }) => {
	await createForMove(page, "admin-move-refresh-error");
	await selectDestination(page);
	await page.getByRole("button", { name: "Save changes" }).click();
	await expect(page.getByText("Refresh required", { exact: true })).toBeVisible();
	await expect(page.getByRole("button", { name: "Edit booking", exact: true })).toBeDisabled();
	await expect(page.getByRole("button", { name: "Save changes" })).toHaveCount(0);
	await page.request.post(`${apiUrl}/_test/recover`);
	await page.getByRole("button", { name: "Refresh booking", exact: true }).click();
	await expect(page.getByRole("button", { name: "Edit booking", exact: true })).toBeEnabled();
	await expect(page.getByText("Retained Draft", { exact: true })).toBeVisible();
	const state = await (await page.request.get(`${apiUrl}/_test/state`)).json();
	expect(state.calls.filter((call: { method: string }) => call.method === "PUT")).toHaveLength(1);
});
