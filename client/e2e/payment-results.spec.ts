import { expect, test, type Page } from "@playwright/test";
import { authenticatePaymentSession, paymentApiUrl, paymentAppUrl } from "./fixtures/payment-session";

const adminUrl = "/outlet/test/1/admin/bookings/44";
const customerUrl = "/outlet/test/1/booking/123/pay";
const payments = (page: Page) => page.getByRole("region", { name: "Booking payments" });
async function openAdmin(page: Page, outcome = "success") {
	await page.request.post(`${paymentApiUrl}/_test/reset`, { data: { scenario: outcome === "success" ? "admin-payment" : `admin-payment-${outcome}` } });
	await authenticatePaymentSession(page);
	await page.goto(adminUrl);
	await expect(payments(page).getByLabel("Payment amount")).toHaveValue("100.00");
}
async function checkout(page: Page, amount = "30") {
	await payments(page).getByRole("radio", { name: "Payfast" }).click();
	await payments(page).getByLabel("Payment amount").fill(amount);
	await payments(page).getByRole("button", { name: "Pay now" }).click();
	await expect(page).toHaveURL(/\/payment\/(success|failure|cancelled)\?transactionId=tx-\d+$/);
}
async function state(page: Page) {
	return await (await page.request.get(`${paymentApiUrl}/_test/state`)).json();
}

test("a provider return without booking context never claims the booking is fully paid", async ({ page }) => {
	await page.goto("/payment/success?transactionId=unknown-transaction");
	await expect(page.getByText("Payment received", { exact: true })).toBeVisible();
	await expect(page.getByText(/Check your booking for the latest payment status and remaining balance/)).toBeVisible();
	await expect(page.getByText("Booking fully paid", { exact: true })).toHaveCount(0);
	await expect(page.getByRole("link", { name: "View My Bookings" })).toHaveAttribute("href", "/bookings");
});

for (const outcome of ["success", "failure", "cancelled"]) {
	test(`admin ${outcome} verifies facility history and returns to booking management, never customer payments`, async ({ page }) => {
		await openAdmin(page, outcome);
		await checkout(page);
		await expect(page.getByRole("link", { name: "Back to booking management" })).toHaveAttribute("href", adminUrl);
		await expect(page.getByRole("link", { name: "View My Bookings" })).toHaveCount(0);
		await expect(page.getByRole("link", { name: "Pay remaining balance / choose another method" })).toHaveCount(0);
		await expect(page.getByText("Booking fully paid", { exact: true })).toHaveCount(0);
		if (outcome === "cancelled") await expect(page.getByText(/Your booking is still pending/)).toHaveCount(0);
		const beforeReturn = await state(page);
		expect(beforeReturn.calls.some((call: { path: string }) => call.path === "/admin/facility/1/booking/44/payments")).toBe(true);
		expect(beforeReturn.calls.some((call: { path: string }) => call.path.startsWith("/payment/booking/") || call.path === "/payment/voucher")).toBe(false);
		const initiate = beforeReturn.calls.find((call: { path: string }) => call.path === "/admin/facility/1/booking/44/payment/initiate");
		expect(initiate.body).toEqual({ providerName: "payfast", amount: 30 });
		await page.getByRole("link", { name: "Back to booking management" }).click();
		await expect(page).toHaveURL(adminUrl);
		await expect(payments(page).getByRole("cell", { name: "Pending", exact: true })).toBeVisible();
		await expect(payments(page).getByLabel("Payment amount")).toHaveValue("70.00");
	});
}

test("customer payment hydration does not emit runtime errors", async ({ page }) => {
	const runtimeErrors: string[] = [];
	page.on("pageerror", (error) => runtimeErrors.push(error.message));
	await page.request.post(`${paymentApiUrl}/_test/reset`, { data: { scenario: "customer-success" } });
	await authenticatePaymentSession(page);
	await page.goto(customerUrl);
	await expect(payments(page).getByLabel("Payment amount")).toHaveValue("300.00");
	await expect(payments(page).getByRole("radio", { name: "Payfast" })).toBeVisible();
	expect(runtimeErrors).toEqual([]);
});

test("admin payment hydration does not emit runtime errors", async ({ page }) => {
	const runtimeErrors: string[] = [];
	page.on("pageerror", (error) => runtimeErrors.push(error.message));
	await openAdmin(page);
	expect(runtimeErrors).toEqual([]);
});

for (const outcome of ["success", "failure", "cancelled"]) {
	test(`customer ${outcome} retains owner history and customer navigation`, async ({ page }) => {
		await page.request.post(`${paymentApiUrl}/_test/reset`, { data: { scenario: `customer-${outcome}` } });
		await authenticatePaymentSession(page);
		await page.goto(customerUrl);
		await checkout(page);
		await expect(page.getByRole("link", { name: outcome === "success" ? "View My Bookings" : "Back to Bookings" })).toHaveAttribute("href", "/bookings");
		await expect(page.getByRole("link", { name: "Back to booking management" })).toHaveCount(0);
		if (outcome === "success") {
			await expect(page.getByRole("link", { name: "Pay remaining balance / choose another method" })).toHaveAttribute(
				"href",
				customerUrl + "?date=2026-06-01&slotCount=1"
			);
		}
		const current = await state(page);
		expect(current.calls.some((call: { path: string }) => call.path === "/payment/booking/123")).toBe(true);
		expect(current.calls.some((call: { path: string }) => call.path.startsWith("/admin/"))).toBe(false);
	});
}

test("admin result refresh distinguishes partial settlement from fully paid", async ({ page }) => {
	await openAdmin(page);
	await checkout(page, "30");
	await page.request.post(`${paymentApiUrl}/_test/callback`, { data: { success: true } });
	await page.reload();
	await expect(page.getByText(/Payment received\. Remaining balance/)).toContainText("70,00");
	await expect(page.getByRole("link", { name: "Manage remaining balance" })).toHaveAttribute("href", adminUrl);
	await page.getByRole("link", { name: "Back to booking management" }).click();
	await checkout(page, "70");
	await page.request.post(`${paymentApiUrl}/_test/callback`, { data: { success: true } });
	await page.reload();
	await expect(page.getByText("Booking fully paid", { exact: true })).toBeVisible();
	await expect(page.getByRole("link", { name: "Manage remaining balance" })).toHaveCount(0);
	await expect(page.getByRole("link", { name: "Back to booking management" })).toHaveAttribute("href", adminUrl);
});

test("copied customer payment URL requires sign-in and only the owner can open it", async ({ page }) => {
	await openAdmin(page);
	await page.context().grantPermissions(["clipboard-read", "clipboard-write"], { origin: paymentAppUrl });
	await page.getByRole("button", { name: "Copy customer payment URL" }).click();
	await expect(page.getByText("Customer payment URL copied. The booking owner must sign in to access it.")).toBeVisible();
	const copied = await page.evaluate(() => navigator.clipboard.readText());
	expect(new URL(copied).pathname).toBe("/outlet/test/1/booking/44/pay");
	expect(new URL(copied).searchParams.has("token")).toBe(false);

	await page.context().clearCookies();
	await page.route("**/auth/login?**", (route) => route.fulfill({ status: 200, body: "Sign in required" }));
	await page.goto(copied);
	await expect(page).toHaveURL(/\/auth\/login/);
	await expect(page.getByRole("region", { name: "Booking payments" })).toHaveCount(0);
	await page.unroute("**/auth/login?**");

	for (const actor of ["other", "manager"] as const) {
		await authenticatePaymentSession(page, actor);
		await page.goto(copied);
		await expect(page.getByRole("region", { name: "Booking payments" })).toHaveCount(0);
		await expect(page.getByText("Player One", { exact: true })).toHaveCount(0);
	}
	await authenticatePaymentSession(page, "owner");
	await page.goto(copied);
	await expect(payments(page).getByLabel("Payment amount")).toHaveValue("100.00");
	await expect(page.getByText("Your booking is confirmed.", { exact: true })).toBeVisible();
	const current = await state(page);
	expect(
		current.calls.some(
			(call: { path: string; authorization: string }) => call.path === "/payment/booking/44" && call.authorization === "Bearer customer-test-token"
		)
	).toBe(true);
});

test("a transaction cannot be substituted onto a different booking using valid-looking admin storage", async ({ page }) => {
	await openAdmin(page);
	await checkout(page);
	await page.evaluate(() => {
		sessionStorage.setItem(
			"payment-return:tx-1",
			JSON.stringify({ transactionId: "tx-1", bookingId: 123, admin: { facilityId: 1, returnUrl: "/outlet/test/1/admin/bookings/123" } })
		);
	});
	await page.reload();
	await expect(page.getByRole("alert")).toContainText("Unable to verify this payment and booking");
	await expect(page.getByRole("link", { name: "Back to booking management" })).toHaveCount(0);
	await expect(page.getByRole("link", { name: "Manage remaining balance" })).toHaveCount(0);
	const current = await state(page);
	expect(current.calls.some((call: { path: string }) => call.path === "/payment/booking/123")).toBe(false);
});

test("unsafe or cross-facility return storage and booking query substitution are ignored", async ({ page }) => {
	await openAdmin(page);
	await checkout(page);
	const original = await page.evaluate(() => sessionStorage.getItem("payment-return:tx-1"));
	for (const returnUrl of ["https://evil.test", "//evil.test", "/outlet/test/2/admin/bookings/44", "/outlet/test/1/admin/bookings/45"]) {
		await page.evaluate((url) => {
			sessionStorage.setItem("payment-return:tx-1", JSON.stringify({ transactionId: "tx-1", bookingId: 44, admin: { facilityId: 1, returnUrl: url } }));
		}, returnUrl);
		await page.reload();
		await expect(page.getByRole("link", { name: "View My Bookings" })).toHaveAttribute("href", "/bookings");
		await expect(page.getByRole("link", { name: "Back to booking management" })).toHaveCount(0);
	}
	await page.evaluate((stored) => sessionStorage.setItem("payment-return:tx-1", stored!), original);
	await page.goto("/payment/success?transactionId=tx-1&bookingId=123");
	await expect(page.getByRole("link", { name: "View My Bookings" })).toHaveAttribute("href", "/bookings");
	await expect(page.getByRole("link", { name: "Back to booking management" })).toHaveCount(0);
});

test("a copied admin result context does not grant facility permission to another signed-in customer", async ({ page }) => {
	await openAdmin(page);
	await checkout(page);
	await authenticatePaymentSession(page, "other");
	await page.reload();
	await expect(page.getByRole("alert")).toContainText("Unable to verify this payment and booking");
	await expect(page.getByRole("link", { name: "Back to booking management" })).toHaveCount(0);
	const current = await state(page);
	expect(
		current.calls.some(
			(call: { path: string; authorization: string }) =>
				call.path === "/admin/facility/1/booking/44/payments" && call.authorization === "Bearer other-customer-test-token"
		)
	).toBe(true);
});
