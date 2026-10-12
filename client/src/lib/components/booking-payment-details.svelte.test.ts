import { expect, it, vi } from "vitest";
import { render } from "vitest-browser-svelte";
import { page } from "vitest/browser";
import BookingPaymentDetails from "./booking-payment-details.svelte";

const props = {
	balance: {
		bookingId: 1,
		amountPaid: 25,
		amountOutstanding: 75,
		amountAvailable: 35,
		isPaid: false,
		payments: [
			{
				id: 1,
				transactionId: "pending-1",
				amount: 40,
				paymentStatusId: 1,
				paymentStatus: "Pending",
				paymentTypeId: 1,
				paymentType: "Card",
				providerName: "payfast",
				paymentStatusDate: "2026-10-12T10:00:00Z",
			},
		],
	},
	methods: [{ providerName: "payfast", type: "Payfast" }],
	busy: false,
	checkoutDisabled: false,
	refreshing: false,
	checkoutAllowed: true,
	onpay: vi.fn().mockResolvedValue(undefined),
	onrefresh: vi.fn().mockResolvedValue(undefined),
};

it("presents pending history and selects the unreserved amount without customer operations", async () => {
	render(BookingPaymentDetails, { ...props, acceptInClub: true });
	await expect.element(page.getByRole("cell", { name: "Pending", exact: true })).toBeVisible();
	await expect.element(page.getByLabelText("Payment amount")).toHaveValue("35.00");
	await expect.element(page.getByText("Your vouchers")).not.toBeInTheDocument();
	await page.getByRole("button", { name: "Refresh payments" }).click();
	expect(props.onrefresh).toHaveBeenCalledOnce();
});

it("shows history but no checkout controls for an inactive reservation", async () => {
	render(BookingPaymentDetails, { ...props, checkoutAllowed: false });
	await expect.element(page.getByText(/Restore the booking before checkout/)).toBeVisible();
	await expect.element(page.getByLabelText("Payment amount")).not.toBeInTheDocument();
	await expect.element(page.getByRole("cell", { name: "Pending", exact: true })).toBeVisible();
});

it("blocks checkout and refresh while an operation is busy", async () => {
	render(BookingPaymentDetails, { ...props, busy: true, acceptInClub: true });
	await expect.element(page.getByRole("button", { name: "Refresh payments" })).toBeDisabled();
	await expect.element(page.getByLabelText("Payment amount")).toBeDisabled();
	await expect.element(page.getByRole("button", { name: "Processing..." })).toBeDisabled();
});
