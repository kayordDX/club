import { createCipheriv, createHash, randomBytes } from "node:crypto";
import { expect, test, type Page } from "@playwright/test";

const payUrl = "/outlet/test/1/booking/123/pay";
const apiUrl = "http://127.0.0.1:5194";
const grantId = (id: number) => `00000000-0000-4000-8000-00000000000${id}`;

// Only the isolated test app knows this secret. No production auth bypass is installed.
async function authenticate(page: Page) {
	const iv = randomBytes(12);
	const cipher = createCipheriv("aes-256-gcm", createHash("sha256").update("isolated-payment-playwright-secret").digest(), iv);
	const payload = JSON.stringify({
		user: { sub: "payment-tester", username: "payment-tester", name: "Payment Tester", email: "tester@example.test" },
		tokens: { accessToken: "payment-test-token", expiresAt: Date.now() + 3600000 },
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
async function open(page: Page, scenario: string) {
	await page.request.post(`${apiUrl}/_test/reset`, { data: { scenario } });
	await authenticate(page);
	await page.goto(payUrl);
	await expect(page.getByRole("region", { name: "Booking payments" }).getByLabel("Payment amount")).toBeVisible();
}
const card = (page: Page, name: string) => page.locator("form").filter({ has: page.getByText(name, { exact: true }) });

test("payment routes require an authenticated session", async ({ page }) => {
	await page.route("**/auth/login?**", (route) => route.fulfill({ status: 200, body: "Sign in required" }));
	await page.goto(payUrl);
	await expect(page).toHaveURL(/\/auth\/login/);
});

test("returned vouchers support round, extra quantity, discount and credit split settlement", async ({ page }) => {
	await open(page, "vouchers");
	await expect(page.getByText("Not eligible: This facility is not eligible.")).toBeVisible();
	await expect(card(page, "Wrong facility").getByRole("button", { name: "Redeem voucher" })).toBeDisabled();
	await card(page, "Round entitlement").getByLabel("Choose an eligible round").selectOption("9");
	await card(page, "Round entitlement").getByRole("button", { name: "Redeem voucher" }).click();
	await expect(page.getByText(/Payment received. Remaining balance/)).toBeVisible();
	await expect(page.getByLabel("Payment amount")).toHaveValue("200.00");
	await card(page, "Extra entitlement").getByLabel("Choose an eligible extra").selectOption("5");
	await card(page, "Extra entitlement").getByLabel("Quantity (maximum 2)").fill("2");
	await card(page, "Extra entitlement").getByRole("button", { name: "Redeem voucher" }).click();
	await expect(page.getByLabel("Payment amount")).toHaveValue("100.00");
	await card(page, "Capped discount").getByRole("button", { name: "Redeem voucher" }).click();
	await expect(page.getByLabel("Payment amount")).toHaveValue("60.00");
	await card(page, "Credit voucher").getByRole("button", { name: "Redeem voucher" }).click();
	await expect(page.getByRole("region", { name: "Booking payments" }).getByText("Your booking is fully paid.").last()).toBeVisible();
	await expect(page.getByText("Your booking is confirmed.")).toBeVisible();
	const state = await (await page.request.get(`${apiUrl}/_test/state`)).json();
	const redemptions = state.calls.filter((call: { path: string }) => call.path === "/payment/voucher");
	expect(redemptions.map((call: { body: unknown }) => call.body)).toEqual([
		{ bookingId: 123, grantId: grantId(1), slotContractBookingId: 9, quantity: 1 },
		{ bookingId: 123, grantId: grantId(2), extraId: 5, quantity: 2 },
		{ bookingId: 123, grantId: grantId(3), quantity: 1 },
		{ bookingId: 123, grantId: grantId(4), quantity: 1 },
	]);
	expect(state.calls.every((call: { authorization: string }) => call.authorization === "Bearer payment-test-token")).toBe(true);
	await expect(page.getByRole("cell", { name: "Completed", exact: true }).last()).toBeVisible();
});

test("partial provider payments reserve funds, then refresh after success and failure callbacks", async ({ page }) => {
	await open(page, "split");
	await page.getByRole("radio", { name: "Payfast" }).click();
	await page.getByLabel("Payment amount").fill("300.01");
	await expect(page.getByRole("button", { name: "Pay now" })).toBeDisabled();
	await page.getByLabel("Payment amount").fill("75");
	await page.getByRole("button", { name: "Pay now" }).click();
	await expect(page).toHaveURL(/\/payment\/success/);
	await page.goto(payUrl);
	await expect(page.getByLabel("Payment amount")).toHaveValue("225.00");
	await expect(page.getByRole("cell", { name: "Pending", exact: true })).toBeVisible();
	await page.request.post(`${apiUrl}/_test/callback`, { data: { success: true } });
	await page.getByRole("button", { name: "Refresh payments" }).click();
	await expect(page.getByRole("cell", { name: "Partial", exact: true })).toBeVisible();
	await expect(page.getByText(/Remaining balance:.*Pay the rest/)).toBeVisible();
	await page.getByRole("radio", { name: "Other provider" }).click();
	await page.getByLabel("Payment amount").fill("25");
	await page.getByRole("button", { name: "Pay now" }).click();
	await expect(page).toHaveURL(/\/payment\/success/);
	await page.request.post(`${apiUrl}/_test/callback`, { data: { success: false } });
	await page.goto(payUrl);
	await expect(page.getByLabel("Payment amount")).toHaveValue("225.00");
	await expect(page.getByRole("cell", { name: "Failed", exact: true })).toBeVisible();
});

test("pending reservations never offer reserved funds", async ({ page }) => {
	await page.request.post(`${apiUrl}/_test/reset`, { data: { scenario: "reserved" } });
	await authenticate(page);
	await page.goto(payUrl);
	await expect(page.getByText(/All outstanding funds are reserved/)).toBeVisible();
	await expect(page.getByLabel("Payment amount")).toHaveCount(0);
	await expect(card(page, "Credit voucher").getByRole("button", { name: "Redeem voucher" })).toBeDisabled();
});

test("stale selection rejection clears selection and prevents duplicate submissions", async ({ page }) => {
	await open(page, "stale");
	const round = card(page, "Round entitlement");
	await round.getByLabel("Choose an eligible round").selectOption("9");
	await round.getByRole("button", { name: "Redeem voucher" }).click();
	await expect(round.getByRole("button", { name: "Redeeming..." })).toBeDisabled();
	await expect(page.getByText(/Selected units are no longer available/)).toBeVisible();
	await expect(round.getByLabel("Choose an eligible round")).toHaveValue("");
	await expect(round.getByRole("button", { name: "Redeem voucher" })).toBeDisabled();
	const state = await (await page.request.get(`${apiUrl}/_test/state`)).json();
	expect(state.calls.filter((call: { path: string }) => call.path === "/payment/voucher")).toHaveLength(1);
});

test("failed refresh releases busy state and requires recovery before payment", async ({ page }) => {
	await open(page, "refresh-failure");
	await card(page, "Round entitlement").getByLabel("Choose an eligible round").selectOption("9");
	await card(page, "Round entitlement").getByRole("button", { name: "Redeem voucher" }).click();
	await expect(page.getByText(/Unable to refresh payment details/)).toBeVisible();
	await expect(page.getByRole("button", { name: "Retry", exact: true })).toBeEnabled();
	await page.request.post(`${apiUrl}/_test/recover`);
	await page.getByRole("button", { name: "Retry", exact: true }).click();
	await expect(page.getByLabel("Payment amount")).toBeEnabled();
});

test("provider rejection is actionable and never adds payment history", async ({ page }) => {
	await open(page, "provider-failure");
	await page.getByRole("radio", { name: "Payfast" }).click();
	await page.getByRole("button", { name: "Pay now" }).click();
	await expect(page.getByText(/Provider unavailable.*Check the refreshed history/)).toBeVisible();
	await expect(page.getByText("No payments yet.")).toBeVisible();
	await expect(page.getByRole("button", { name: "Pay now" })).toBeEnabled();
});
