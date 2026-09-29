import { expect, it } from "vitest";
import { render } from "vitest-browser-svelte";
import { page } from "vitest/browser";
import PaymentResultCard from "./payment-result-card.svelte";
import { formatCurrency } from "$lib/booking/format";

const balance = { bookingId: 1, amountPaid: 50, amountOutstanding: 150, amountAvailable: 150, isPaid: false, payments: [] };
it("shows remaining balance and another payment method after partial success", async () => {
	render(PaymentResultCard, { balance, payUrl: "/bookings" });
	await expect.element(page.getByText(/Payment received\. Remaining balance/)).toHaveTextContent(formatCurrency(150).replaceAll("\u00a0", " "));
	await expect.element(page.getByRole("link", { name: "Pay remaining balance / choose another method" })).toBeVisible();
	await expect.element(page.getByText("Booking fully paid", { exact: true })).not.toBeInTheDocument();
});
it("shows fully paid only when the backend paid flag is true", async () => {
	render(PaymentResultCard, { balance: { ...balance, isPaid: true, amountOutstanding: 0 }, payUrl: "/bookings" });
	await expect.element(page.getByText("Your booking is fully paid.", { exact: true })).toBeVisible();
	await expect.element(page.getByRole("link", { name: "Pay remaining balance / choose another method" })).not.toBeInTheDocument();
});
it("does not claim full settlement without a verified balance", async () => {
	render(PaymentResultCard);
	await expect.element(page.getByText(/Check your booking for the latest payment status/)).toBeVisible();
	await expect.element(page.getByText("Booking fully paid", { exact: true })).not.toBeInTheDocument();
});
