import { beforeEach, expect, it, vi } from "vitest";
import { render } from "vitest-browser-svelte";
import { page } from "vitest/browser";
import Harness from "./testing/booking-payments-harness.svelte";
import { VoucherRedemptionKind, type BookingPaymentDTO, type BookingVoucherDTO } from "$lib/api";

const api = vi.hoisted(() => ({ history: vi.fn(), vouchers: vi.fn(), redeem: vi.fn(), initiate: vi.fn() }));
vi.mock("$app/env", () => ({ browser: true, dev: true, building: false, version: "test" }));
vi.mock("$lib/api/remote/payment.remote", () => ({
	paymentGetBooking: api.history,
	paymentVouchers: api.vouchers,
	paymentVoucher: api.redeem,
	paymentInitiate: api.initiate,
}));

const grant: BookingVoucherDTO = {
	grantId: "7e9d6dd5-ea64-4f63-b3c8-0d1e81a5b734",
	voucherId: 1,
	name: "Round voucher",
	description: "Issued rounds",
	isExtra: false,
	redemptionKind: VoucherRedemptionKind.Entitlement,
	amountRemaining: 2,
	expiryDate: "2027-01-01",
	isEligible: true,
	eligibleAmount: 100,
	paymentValue: 100,
	targets: [{ slotContractBookingId: 9, name: "Player One", unitsAvailable: 1, unitPrice: 100 }],
};
let balance: BookingPaymentDTO;
let grants: BookingVoucherDTO[];
let failRefresh: boolean;
function remote<T>(value: T) {
	return Object.assign(Promise.resolve(value), { refresh: () => (failRefresh ? Promise.reject(new Error("Offline")) : Promise.resolve()) });
}
function mount() {
	const onrefresh = vi.fn().mockResolvedValue(undefined);
	const onbusy = vi.fn();
	render(Harness, { bookingId: 123, methods: [{ providerName: "payfast", type: "Payfast" }], onrefresh, onbusy, oneditable: vi.fn() });
	return { onrefresh, onbusy };
}
beforeEach(() => {
	vi.clearAllMocks();
	balance = { bookingId: 123, amountPaid: 0, amountOutstanding: 200, amountAvailable: 200, isPaid: false, payments: [] };
	grants = [{ ...grant }];
	failRefresh = false;
	api.history.mockImplementation(() => remote(balance));
	api.vouchers.mockImplementation(() => remote(grants));
});

it("refreshes amounts, history, booking and eligibility after a partial voucher payment", async () => {
	api.redeem.mockImplementation(async () => {
		balance = {
			...balance,
			amountPaid: 100,
			amountOutstanding: 100,
			amountAvailable: 100,
			payments: [
				{
					id: 1,
					transactionId: "voucher-tx",
					amount: 100,
					paymentTypeId: 4,
					paymentType: "Voucher",
					paymentStatusId: 5,
					paymentStatus: "Partial",
					providerName: "Voucher",
					paymentStatusDate: "2026-01-01",
				},
			],
		};
		grants = [{ ...grant, isEligible: false, ineligibleReason: "No eligible rounds remain.", targets: [] }];
		return { isPaid: false, amountOutstanding: 100 };
	});
	const { onrefresh } = mount();
	await page.getByLabelText("Choose an eligible round").selectOptions("9");
	await page.getByRole("button", { name: "Redeem voucher" }).click();
	await expect.element(page.getByText(/Payment received. Remaining balance/)).toBeVisible();
	await expect.element(page.getByText("Partial", { exact: true })).toBeVisible();
	await expect.element(page.getByText("Not eligible: No eligible rounds remain.")).toBeVisible();
	await expect.element(page.getByLabelText("Payment amount")).toHaveValue("100.00");
	expect(onrefresh.mock.calls.length).toBeGreaterThanOrEqual(2);
	expect(api.redeem).toHaveBeenCalledExactlyOnceWith({ bookingId: 123, grantId: grant.grantId, slotContractBookingId: 9, quantity: 1 });
});

it("clears rejected selections and allows deliberate reselection", async () => {
	api.redeem.mockRejectedValue(new Error("Selected units are no longer available."));
	mount();
	await page.getByLabelText("Choose an eligible round").selectOptions("9");
	await page.getByRole("button", { name: "Redeem voucher" }).click();
	await expect.element(page.getByText(/Eligibility or the selected quantity may have changed/)).toBeVisible();
	await expect.element(page.getByLabelText("Choose an eligible round")).toHaveValue("");
	await expect.element(page.getByRole("button", { name: "Redeem voucher" })).toBeDisabled();
	expect(api.vouchers.mock.calls.length).toBeGreaterThan(1);
});

it("releases the submission lock but blocks payments if refresh fails", async () => {
	api.redeem.mockImplementation(async () => {
		failRefresh = true;
		throw new Error("Connection lost");
	});
	const { onbusy } = mount();
	await page.getByLabelText("Choose an eligible round").selectOptions("9");
	await page.getByRole("button", { name: "Redeem voucher" }).click();
	await expect.element(page.getByText(/Unable to refresh payment details/)).toBeVisible();
	await expect.element(page.getByRole("button", { name: "Retry", exact: true })).toBeEnabled();
	expect(onbusy).toHaveBeenLastCalledWith(false);
	failRefresh = false;
	await page.getByRole("button", { name: "Retry", exact: true }).click();
	await expect.element(page.getByLabelText("Choose an eligible round")).toBeEnabled();
});

it("keeps payment methods visible after a partial payment with an abandoned pending attempt", async () => {
	balance = {
		...balance,
		amountPaid: 40,
		amountOutstanding: 60,
		amountAvailable: 60,
		payments: [
			{
				id: 1,
				transactionId: "abandoned",
				amount: 60,
				paymentTypeId: 1,
				paymentType: "Credit card",
				paymentStatusId: 1,
				paymentStatus: "Pending",
				providerName: "other",
				paymentStatusDate: "2026-01-01",
			},
		],
	};
	mount();
	await expect.element(page.getByText("Pending", { exact: true })).toBeVisible();
	await expect.element(page.getByLabelText("Payment amount")).toHaveValue("60.00");
	await page.getByRole("radio", { name: "Payfast" }).click();
	await expect.element(page.getByRole("button", { name: "Pay now" })).toBeEnabled();
});

it("does not retry or duplicate a pending voucher submission", async () => {
	let complete!: (_value: { isPaid: boolean; amountOutstanding: number }) => void;
	api.redeem.mockImplementation(
		() =>
			new Promise((resolve) => {
				complete = resolve;
			})
	);
	mount();
	await page.getByLabelText("Choose an eligible round").selectOptions("9");
	await page.getByRole("button", { name: "Redeem voucher" }).click();
	await expect.element(page.getByRole("button", { name: "Redeeming..." })).toBeDisabled();
	await expect.element(page.getByRole("button", { name: "Processing..." })).toBeDisabled();
	expect(api.redeem).toHaveBeenCalledTimes(1);
	complete({ isPaid: false, amountOutstanding: 100 });
	await expect.element(page.getByText(/Payment received. Remaining balance/)).toBeVisible();
});
