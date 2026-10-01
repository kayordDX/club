import { beforeEach, expect, it, vi } from "vitest";
import { render } from "vitest-browser-svelte";
import { page } from "vitest/browser";
import type { BookingPaymentDTO, PaymentDTO } from "$lib/api";
import { formatCurrency, formatDateTime } from "$lib/booking/format";
import Harness from "../testing/booking-successful-payments-harness.svelte";

const api = vi.hoisted(() => ({ history: vi.fn() }));
vi.mock("$app/env", () => ({ browser: true, dev: true, building: false, version: "test" }));
vi.mock("$lib/api/remote/payment.remote", () => ({ paymentGetBooking: api.history }));

function payment(id: number, paymentStatusId: number, amount: number): PaymentDTO {
	return {
		id,
		paymentStatusId,
		amount,
		transactionId: `reference-${id}`,
		paymentTypeId: 4,
		paymentType: "Voucher",
		providerName: "Voucher",
		paymentStatus: "Status",
		paymentStatusDate: "2026-08-01T10:00:00Z",
	};
}
const balance: BookingPaymentDTO = {
	bookingId: 42,
	amountPaid: 150,
	amountOutstanding: 50,
	amountAvailable: 50,
	isPaid: false,
	payments: [payment(1, 2, 100), payment(2, 5, 50), payment(3, 1, 20), payment(4, 3, 30), payment(5, 4, 40)],
};
function remote(value: BookingPaymentDTO) {
	return Object.assign(Promise.resolve(value), { refresh: () => Promise.resolve() });
}
beforeEach(() => {
	vi.clearAllMocks();
	api.history.mockImplementation(() => remote(balance));
});

it("shows completed and partial payments that contribute to the amount paid, excluding other statuses", async () => {
	render(Harness, { bookingId: 42 });
	await expect.element(page.getByText("reference-1", { exact: true })).toBeVisible();
	await expect.element(page.getByText("reference-2", { exact: true })).toBeVisible();
	for (const id of [3, 4, 5]) {
		await expect.element(page.getByText(`reference-${id}`, { exact: true })).not.toBeInTheDocument();
	}
	await expect.element(page.getByText(formatCurrency(100), { exact: true })).toBeVisible();
	await expect.element(page.getByText(formatCurrency(50), { exact: true })).toBeVisible();
	await expect.element(page.getByText(formatCurrency(150), { exact: true })).toBeVisible();
	await expect.element(page.getByText("Voucher / Voucher", { exact: true }).first()).toBeVisible();
	await expect.element(page.getByText(formatDateTime(balance.payments[0].paymentStatusDate), { exact: true }).first()).toBeVisible();
	expect(api.history).toHaveBeenCalledWith(42);
});

it("shows an empty state when no payments have succeeded", async () => {
	api.history.mockImplementation(() => remote({ ...balance, amountPaid: 0, payments: [payment(3, 1, 20)] }));
	render(Harness, { bookingId: 42 });
	await expect.element(page.getByText("No successful payments yet.")).toBeVisible();
});

it("allows retrying a failed payment history request", async () => {
	api.history.mockImplementationOnce(() => Object.assign(Promise.resolve(balance), { refresh: () => Promise.reject(new Error("Offline")) }));
	render(Harness, { bookingId: 42 });
	await expect.element(page.getByRole("alert")).toHaveTextContent("Unable to load successful payments.");
	await page.getByRole("button", { name: "Retry" }).click();
	await expect.element(page.getByText("reference-1", { exact: true })).toBeVisible();
});
