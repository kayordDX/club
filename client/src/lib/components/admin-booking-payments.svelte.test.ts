import { beforeEach, expect, it, vi } from "vitest";
import { render } from "vitest-browser-svelte";
import { page } from "vitest/browser";
import { BookingStatusEnum, type BookingPaymentDTO } from "$lib/api";
import Harness from "./testing/admin-booking-payments-harness.svelte";

const api = vi.hoisted(() => ({
	history: vi.fn(),
	methods: vi.fn(),
	initiate: vi.fn(),
	vouchers: vi.fn(),
	redeem: vi.fn(),
	customerHistory: vi.fn(),
	remember: vi.fn(),
}));
vi.mock("$app/env", () => ({ browser: true, dev: true, building: false, version: "test" }));
vi.mock("$lib/api/remote/admin.remote", () => ({ adminBookingGetPayments: api.history, adminBookingAcceptPayment: api.initiate }));
vi.mock("$lib/api/remote/facility.remote", () => ({ facilityPaymentMethods: api.methods }));
vi.mock("$lib/api/remote/payment.remote", () => ({ paymentVouchers: api.vouchers, paymentVoucher: api.redeem, paymentGetBooking: api.customerHistory }));
vi.mock("$lib/booking/payment-return-context", () => ({ rememberPaymentReturn: api.remember }));

let balance: BookingPaymentDTO;
let failHistory: boolean;
let failMethods: boolean;
function remote<T>(value: T, failed = false) {
	return Object.assign(Promise.resolve(value), { refresh: () => (failed ? Promise.reject(new Error("Offline")) : Promise.resolve()) });
}
function mount(status: BookingStatusEnum = BookingStatusEnum.Confirmed, expiresAt = "2026-01-01", disabled = false) {
	const onrefresh = vi.fn().mockResolvedValue(undefined);
	const onbusy = vi.fn();
	render(Harness, {
		facilityId: 7,
		bookingId: 123,
		booking: { bookingStatusId: status, expiresAt },
		returnUrl: "/outlet/club/7/admin/bookings/123",
		customerPayUrl: "/outlet/club/7/booking/123/pay?slotCount=1",
		disabled,
		onrefresh,
		onbusy,
	});
	return { onrefresh, onbusy };
}
beforeEach(() => {
	vi.resetAllMocks();
	balance = {
		bookingId: 123,
		amountPaid: 40,
		amountOutstanding: 160,
		amountAvailable: 100,
		isPaid: false,
		payments: [
			{
				id: 1,
				transactionId: "settled",
				amount: 40,
				paymentTypeId: 1,
				paymentType: "Credit card",
				paymentStatusId: 2,
				paymentStatus: "Success",
				providerName: "payfast",
				paymentStatusDate: "2026-01-01",
			},
			{
				id: 2,
				transactionId: "reserved",
				amount: 60,
				paymentTypeId: 1,
				paymentType: "Credit card",
				paymentStatusId: 1,
				paymentStatus: "Pending",
				providerName: "payfast",
				paymentStatusDate: "2026-01-01",
			},
			{
				id: 3,
				transactionId: "failed",
				amount: 25,
				paymentTypeId: 1,
				paymentType: "Credit card",
				paymentStatusId: 3,
				paymentStatus: "Failed",
				providerName: "payfast",
				paymentStatusDate: "2026-01-01",
			},
		],
	};
	failHistory = false;
	failMethods = false;
	api.history.mockImplementation(() => remote(balance, failHistory));
	api.methods.mockImplementation(() => remote([{ providerName: "payfast", type: "Payfast" }], failMethods));
	api.remember.mockReturnValue(true);
});

it("shows authorised history and reservable amount without querying or redeeming any vouchers", async () => {
	mount();
	await expect.element(page.getByText("Success", { exact: true })).toBeVisible();
	await expect.element(page.getByText("Pending", { exact: true })).toBeVisible();
	await expect.element(page.getByText("Failed", { exact: true })).toBeVisible();
	await expect.element(page.getByLabelText("Payment amount")).toHaveValue("100.00");
	await expect.element(page.getByText("Your vouchers")).not.toBeInTheDocument();
	await expect.element(page.getByRole("button", { name: "Redeem voucher" })).not.toBeInTheDocument();
	expect(api.history).toHaveBeenCalledWith({ facilityId: 7, id: 123 });
	expect(api.customerHistory).not.toHaveBeenCalled();
	expect(api.vouchers).not.toHaveBeenCalled();
	expect(api.redeem).not.toHaveBeenCalled();
});

it("allows admins to receive guest payments without offering an authenticated customer payment link", async () => {
	render(Harness, {
		facilityId: 7,
		bookingId: 123,
		booking: { bookingStatusId: BookingStatusEnum.Confirmed, expiresAt: "2999-01-01", userId: null, guestName: "Walk-in Guest" },
		returnUrl: "/outlet/club/7/admin/bookings/123",
		onrefresh: vi.fn().mockResolvedValue(undefined),
		onbusy: vi.fn(),
	});
	await expect.element(page.getByText(/Guest booking: accept payment in the club/)).toBeVisible();
	await expect.element(page.getByRole("button", { name: "Copy customer payment URL" })).not.toBeInTheDocument();
	await page.getByRole("button", { name: "Accept payment" }).click();
	await vi.waitFor(() => expect(api.initiate).toHaveBeenCalledExactlyOnceWith({ facilityId: 7, id: 123, body: { amount: 100 } }));
});

it("accepts an in-club partial payment without choosing a provider or redirecting", async () => {
	api.initiate.mockResolvedValue(balance);
	const { onbusy, onrefresh } = mount();
	await expect.element(page.getByRole("radio", { name: "Payfast" })).not.toBeInTheDocument();
	await page.getByLabelText("Payment amount").fill("25.50");
	await page.getByRole("button", { name: "Accept payment" }).click();
	await vi.waitFor(() => expect(api.initiate).toHaveBeenCalledExactlyOnceWith({ facilityId: 7, id: 123, body: { amount: 25.5 } }));
	await vi.waitFor(() => expect(onbusy).toHaveBeenLastCalledWith(false));
	expect(api.remember).not.toHaveBeenCalled();
	expect(onrefresh.mock.calls.length).toBeGreaterThanOrEqual(2);
});

it.each([BookingStatusEnum.Cancelled, BookingStatusEnum.Expired, BookingStatusEnum.Pending])(
	"shows history but requires restoration for inactive status %s",
	async (status) => {
		mount(status);
		await expect.element(page.getByText(/Restore the booking before checkout/)).toBeVisible();
		await expect.element(page.getByText("Pending", { exact: true })).toBeVisible();
		await expect.element(page.getByLabelText("Payment amount")).not.toBeInTheDocument();
		expect(api.initiate).not.toHaveBeenCalled();
		expect(api.vouchers).not.toHaveBeenCalled();
	}
);

it("allows active Pending checkout and blocks it while another booking mutation is busy", async () => {
	mount(BookingStatusEnum.Pending, "2999-01-01", true);
	await expect.element(page.getByLabelText("Payment amount")).toHaveValue("100.00");
	await expect.element(page.getByLabelText("Payment amount")).toBeDisabled();
	await expect.element(page.getByRole("button", { name: "Processing..." })).toBeDisabled();
	expect(api.initiate).not.toHaveBeenCalled();
});

it("does not duplicate a pending initiation and refreshes reservations after failure", async () => {
	let reject!: (_error: Error) => void;
	api.initiate.mockImplementation(
		() =>
			new Promise((_resolve, rejectPromise) => {
				reject = rejectPromise;
			})
	);
	const { onbusy } = mount();
	await page.getByRole("button", { name: "Accept payment" }).click();
	await expect.element(page.getByRole("button", { name: "Processing..." })).toBeDisabled();
	await expect.element(page.getByRole("button", { name: "Refresh payments" })).toBeDisabled();
	expect(api.initiate).toHaveBeenCalledTimes(1);
	balance = { ...balance, amountAvailable: 75 };
	reject(new Error("Connection lost"));
	await expect.element(page.getByText(/Check the payment history and refresh before trying again/)).toBeVisible();
	await expect.element(page.getByLabelText("Payment amount")).toHaveValue("75.00");
	await expect.element(page.getByLabelText("Payment amount")).toBeDisabled();
	expect(onbusy).toHaveBeenLastCalledWith(false);
	await page.getByRole("button", { name: "Refresh payments" }).click();
	await expect.element(page.getByLabelText("Payment amount")).toBeEnabled();
	expect(api.initiate).toHaveBeenCalledTimes(1);
});

it("retries failed history refresh without resubmitting payment", async () => {
	api.initiate.mockImplementation(async () => {
		failHistory = true;
		throw new Error("Connection lost");
	});
	const { onbusy } = mount();
	await page.getByRole("button", { name: "Accept payment" }).click();
	await expect.element(page.getByText(/Unable to refresh payment details/)).toBeVisible();
	await expect.element(page.getByRole("button", { name: "Retry", exact: true })).toBeEnabled();
	expect(onbusy).toHaveBeenLastCalledWith(false);
	failHistory = false;
	await page.getByRole("button", { name: "Retry", exact: true }).click();
	await expect.element(page.getByLabelText("Payment amount")).toBeEnabled();
	expect(api.initiate).toHaveBeenCalledTimes(1);
});

it("does not depend on online payment method configuration", async () => {
	failMethods = true;
	mount();
	await expect.element(page.getByRole("button", { name: "Accept payment" })).toBeEnabled();
	expect(api.methods).not.toHaveBeenCalled();
});

it("refreshes balances and attempts after settlement", async () => {
	const { onrefresh } = mount();
	await expect.element(page.getByLabelText("Payment amount")).toHaveValue("100.00");
	balance = {
		...balance,
		amountPaid: 200,
		amountOutstanding: 0,
		amountAvailable: 0,
		isPaid: true,
		payments: balance.payments.map((attempt) => ({ ...attempt, paymentStatus: "Success" })),
	};
	await page.getByRole("button", { name: "Refresh payments" }).click();
	await expect.element(page.getByText("This booking is fully paid.")).toBeVisible();
	await expect.element(page.getByLabelText("Payment amount")).not.toBeInTheDocument();
	expect(onrefresh.mock.calls.length).toBeGreaterThanOrEqual(2);
	expect(api.vouchers).not.toHaveBeenCalled();
});

it("copies only the existing authenticated customer payment URL", async () => {
	const writeText = vi.fn().mockResolvedValue(undefined);
	vi.spyOn(navigator.clipboard, "writeText").mockImplementation(writeText);
	mount();
	await page.getByRole("button", { name: "Copy customer payment URL" }).click();
	await expect.element(page.getByText(/The booking owner must sign in to access it/)).toBeVisible();
	expect(writeText).toHaveBeenCalledExactlyOnceWith(`${window.location.origin}/outlet/club/7/booking/123/pay?slotCount=1`);
	expect(api.vouchers).not.toHaveBeenCalled();
});

it("provides manual authenticated link fallback when copying fails", async () => {
	vi.spyOn(navigator.clipboard, "writeText").mockRejectedValue(new Error("Permission denied"));
	mount();
	await page.getByRole("button", { name: "Copy customer payment URL" }).click();
	await expect
		.element(page.getByRole("link", { name: "Customer payment link (sign-in required)" }))
		.toHaveAttribute("href", "/outlet/club/7/booking/123/pay?slotCount=1");
	expect(api.initiate).not.toHaveBeenCalled();
});
