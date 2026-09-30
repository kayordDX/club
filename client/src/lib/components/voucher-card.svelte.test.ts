import { expect, it, vi } from "vitest";
import { render } from "vitest-browser-svelte";
import { page } from "vitest/browser";
import VoucherCard from "./voucher-card.svelte";
import { VoucherDiscountMode, VoucherRedemptionKind, type BookingVoucherDTO } from "$lib/api";

const voucher: BookingVoucherDTO = {
	grantId: "grant-1",
	voucherId: 1,
	name: "Free rounds",
	description: "Your rounds",
	isExtra: false,
	redemptionKind: VoucherRedemptionKind.Entitlement,
	amountRemaining: 2,
	expiryDate: "2027-01-01",
	isEligible: true,
	eligibleAmount: 200,
	paymentValue: 100,
	targets: [{ slotContractBookingId: 9, name: "Player One", unitsAvailable: 1, unitPrice: 100 }],
};

it("selects an eligible round and redeems the documented request", async () => {
	const onredeem = vi.fn().mockResolvedValue(undefined);
	render(VoucherCard, { voucher, bookingId: 123, onredeem });
	const redeem = page.getByRole("button", { name: "Redeem voucher" });
	await expect.element(redeem).toBeDisabled();
	await page.getByLabelText("Choose an eligible round").selectOptions("9");
	await page.getByLabelText("Quantity (maximum 1)").fill("2");
	await expect.element(redeem).toBeDisabled();
	await page.getByLabelText("Quantity (maximum 1)").fill("1");
	await redeem.click();
	expect(onredeem).toHaveBeenCalledExactlyOnceWith({ bookingId: 123, grantId: "grant-1", slotContractBookingId: 9, quantity: 1 });
});

it("shows backend rejection and disables an ineligible voucher", async () => {
	render(VoucherCard, { voucher: { ...voucher, isEligible: false, ineligibleReason: "This facility is not eligible." }, bookingId: 123, onredeem: vi.fn() });
	await expect.element(page.getByText("Not eligible: This facility is not eligible.")).toBeVisible();
	await expect.element(page.getByRole("button", { name: "Redeem voucher" })).toBeDisabled();
});

it("applies a capped percentage discount without selecting an item", async () => {
	const onredeem = vi.fn().mockResolvedValue(undefined);
	render(VoucherCard, {
		voucher: {
			...voucher,
			redemptionKind: VoucherRedemptionKind.Discount,
			discountMode: VoucherDiscountMode.Percentage,
			discountValue: 20,
			maxDiscountAmount: 50,
		},
		bookingId: 123,
		onredeem,
	});
	await expect.element(page.getByText(/20% discount/)).toBeVisible();
	await expect.element(page.getByText(/Maximum/)).toBeVisible();
	await page.getByRole("button", { name: "Redeem voucher" }).click();
	expect(onredeem).toHaveBeenCalledExactlyOnceWith({ bookingId: 123, grantId: "grant-1", quantity: 1 });
});

it("redeems multiple extra units using only the returned target ID", async () => {
	const onredeem = vi.fn().mockResolvedValue(undefined);
	render(VoucherCard, {
		voucher: { ...voucher, isExtra: true, targets: [{ extraId: 5, name: "Cart", unitsAvailable: 2, unitPrice: 50 }] },
		bookingId: 123,
		onredeem,
	});
	await page.getByLabelText("Choose an eligible extra").selectOptions("5");
	await page.getByLabelText("Quantity (maximum 2)").fill("2");
	await page.getByRole("button", { name: "Redeem voucher" }).click();
	expect(onredeem).toHaveBeenCalledExactlyOnceWith({ bookingId: 123, grantId: "grant-1", extraId: 5, quantity: 2 });
});

it.each([VoucherRedemptionKind.Credit, VoucherRedemptionKind.Discount])("redeems credit or fixed discounts without targets (%s)", async (redemptionKind) => {
	const onredeem = vi.fn().mockResolvedValue(undefined);
	render(VoucherCard, {
		voucher: { ...voucher, redemptionKind, discountMode: VoucherDiscountMode.FixedAmount, discountValue: 50, targets: [] },
		bookingId: 123,
		onredeem,
	});
	await expect.element(page.getByRole("combobox")).not.toBeInTheDocument();
	await page.getByRole("button", { name: "Redeem voucher" }).click();
	expect(onredeem).toHaveBeenCalledExactlyOnceWith({ bookingId: 123, grantId: "grant-1", quantity: 1 });
});

it("prevents duplicate submissions while a redemption is in progress", async () => {
	render(VoucherCard, { voucher: { ...voucher, redemptionKind: VoucherRedemptionKind.Credit }, bookingId: 123, busy: true, onredeem: vi.fn() });
	await expect.element(page.getByRole("button", { name: "Redeem voucher" })).toBeDisabled();
});
