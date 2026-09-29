import { expect, test } from "@playwright/test";

test("a provider return without booking context never claims the booking is fully paid", async ({ page }) => {
	await page.goto("/payment/success?transactionId=unknown-transaction");
	await expect(page.getByText("Payment received", { exact: true })).toBeVisible();
	await expect(page.getByText(/Check your booking for the latest payment status and remaining balance/)).toBeVisible();
	await expect(page.getByText("Booking fully paid", { exact: true })).toHaveCount(0);
	await expect(page.getByRole("link", { name: "View My Bookings" })).toHaveAttribute("href", "/bookings");
});
