import { beforeEach, expect, it, vi } from "vitest";
import { render } from "vitest-browser-svelte";
import { page } from "vitest/browser";
import PaymentResultCard from "./testing/payment-result-harness.svelte";
import { rememberPaymentReturn } from "#lib/booking/payment-return-context.js";
import { formatCurrency } from "#lib/booking/format.js";

const api = vi.hoisted(() => ({ admin: vi.fn(), customer: vi.fn(), booking: vi.fn(), path: vi.fn() }));
vi.mock("#lib/api/remote/admin.remote.js", () => ({ adminBookingGetPayments: api.admin }));
vi.mock("#lib/api/remote/payment.remote.js", () => ({ paymentGetBooking: api.customer }));
vi.mock("#lib/api/remote/booking.remote.js", () => ({ bookingGet: api.booking, bookingGetPath: api.path }));
function remote<T>(value: T) {
	return Object.assign(Promise.resolve(value), { refresh: () => Promise.resolve() });
}
const balance = { bookingId: 1, amountPaid: 50, amountOutstanding: 150, amountAvailable: 150, isPaid: false, payments: [] };
const associated = {
	...balance,
	payments: [
		{
			id: 1,
			transactionId: "tx",
			amount: 50,
			paymentStatusId: 5,
			paymentStatus: "Partial",
			paymentTypeId: 1,
			paymentType: "Card",
			paymentStatusDate: "2026-08-02",
			providerName: "payfast",
		},
	],
};
beforeEach(() => {
	vi.clearAllMocks();
	sessionStorage.clear();
	api.admin.mockImplementation(() => remote(associated));
	api.customer.mockImplementation(() => remote(associated));
	api.booking.mockImplementation(() => remote({ slotContractBookings: [{}] }));
	api.path.mockImplementation(() => remote({ outletSlug: "test", facilityId: 1 }));
});
it("shows remaining balance and another payment method after partial success", async () => {
	render(PaymentResultCard, { balance, payUrl: "/bookings" });
	await expect
		.poll(() =>
			page
				.getByText(/Payment received\. Remaining balance/)
				.element()
				.textContent?.replaceAll("\u00a0", " ")
		)
		.toContain(formatCurrency(150).replaceAll("\u00a0", " "));
	await expect.element(page.getByRole("link", { name: "Pay remaining balance / choose another method" })).toBeVisible();
	await expect.element(page.getByText("Booking fully paid", { exact: true })).not.toBeInTheDocument();
});
it("shows fully paid only when the backend paid flag is true", async () => {
	render(PaymentResultCard, { balance: { ...balance, isPaid: true, amountOutstanding: 0 }, payUrl: "/bookings" });
	await expect.element(page.getByText("Your booking is fully paid.", { exact: true })).toBeVisible();
	await expect.element(page.getByRole("link", { name: "Pay remaining balance / choose another method" })).not.toBeInTheDocument();
});
it.each(["success", "failure", "cancelled"] as const)("uses only admin-authorised history and management navigation for %s", async (outcome) => {
	rememberPaymentReturn("tx", 1, { facilityId: 1, returnUrl: "/outlet/test/1/admin/bookings/1" });
	render(PaymentResultCard, { transactionId: "tx", outcome, lookup: true });
	await expect.element(page.getByRole("link", { name: "Back to booking management" })).toHaveAttribute("href", "/outlet/test/1/admin/bookings/1");
	expect(api.admin).toHaveBeenCalledWith({ facilityId: 1, id: 1 });
	expect(api.customer).not.toHaveBeenCalled();
	expect(api.booking).not.toHaveBeenCalled();
	expect(api.path).not.toHaveBeenCalled();
});

it("keeps customer history and customer payment navigation", async () => {
	rememberPaymentReturn("tx", 1);
	render(PaymentResultCard, { transactionId: "tx", lookup: true });
	await expect
		.element(page.getByRole("link", { name: "Pay remaining balance / choose another method" }))
		.toHaveAttribute("href", "/outlet/test/1/booking/1/pay?slotCount=1");
	expect(api.customer).toHaveBeenCalledWith(1);
	expect(api.admin).not.toHaveBeenCalled();
});

it.each(["missing transaction", "different booking", "denied"] as const)("does not expose admin balance or navigation for %s", async (reason) => {
	rememberPaymentReturn("tx", 1, { facilityId: 1, returnUrl: "/outlet/test/1/admin/bookings/1" });
	if (reason === "denied")
		api.admin.mockImplementation(() => Object.assign(Promise.resolve(associated), { refresh: () => Promise.reject(new Error("Forbidden")) }));
	else api.admin.mockImplementation(() => remote(reason === "different booking" ? { ...associated, bookingId: 2 } : balance));
	render(PaymentResultCard, { transactionId: "tx", lookup: true });
	await expect.poll(() => page.getByRole("alert").element().textContent?.replaceAll("\u00a0", " ")).toContain("Unable to verify this payment and booking");
	await expect.element(page.getByRole("link", { name: "Back to booking management" })).not.toBeInTheDocument();
	await expect.element(page.getByRole("link", { name: "Manage remaining balance" })).not.toBeInTheDocument();
	expect(api.customer).not.toHaveBeenCalled();
});

it("does not query or navigate a substituted booking context", async () => {
	rememberPaymentReturn("tx", 1, { facilityId: 1, returnUrl: "/outlet/test/1/admin/bookings/1" });
	render(PaymentResultCard, { transactionId: "tx", requestedBookingId: 2, lookup: true });
	await expect.element(page.getByText(/Check your booking for the latest payment status/)).toBeVisible();
	expect(api.admin).not.toHaveBeenCalled();
	expect(api.customer).not.toHaveBeenCalled();
});

it("does not claim full settlement without a verified balance", async () => {
	render(PaymentResultCard);
	await expect.element(page.getByText(/Check your booking for the latest payment status/)).toBeVisible();
	await expect.element(page.getByText("Booking fully paid", { exact: true })).not.toBeInTheDocument();
});
