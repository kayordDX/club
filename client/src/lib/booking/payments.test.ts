import { describe, expect, it } from "vitest";
import { validatePaymentAmount, voucherRequest, paymentMessage } from "./payments";
import { VoucherRedemptionKind, type BookingVoucherDTO } from "$lib/api";

const voucher: BookingVoucherDTO = {
	grantId: "grant-1",
	voucherId: 1,
	name: "Rounds",
	description: "Free rounds",
	isExtra: false,
	redemptionKind: VoucherRedemptionKind.Entitlement,
	amountRemaining: 2,
	expiryDate: "2027-01-01",
	isEligible: true,
	eligibleAmount: 200,
	paymentValue: 100,
	targets: [{ slotContractBookingId: 9, name: "Player", unitsAvailable: 1, unitPrice: 100 }],
};

describe("split payment amounts", () => {
	it.each(["", "0", "-1", "1.001", "NaN", "Infinity", "1e2", "1.", "100.01"])("rejects %s", (value) => expect(validatePaymentAmount(value, 100)).toBeDefined());
	it.each(["0.01", "1", "1.2", "100.00"])("accepts %s", (value) => expect(validatePaymentAmount(value, 100)).toBeUndefined());
	it("rejects all positive amounts when pending payments reserve the balance", () => expect(validatePaymentAmount("0.01", 0)).toBeDefined());
});
describe("voucher requests", () => {
	it("requires an entitlement target", () => expect(voucherRequest(1, voucher, undefined, 1)).toBeUndefined());
	it("sends only a selected round ID", () =>
		expect(voucherRequest(1, voucher, voucher.targets[0], 1)).toEqual({ bookingId: 1, grantId: "grant-1", slotContractBookingId: 9, quantity: 1 }));
	it.each([0, 2, 1.5, NaN])("rejects invalid quantity %s", (quantity) => expect(voucherRequest(1, voucher, voucher.targets[0], quantity)).toBeUndefined());
	it("limits quantity to remaining grant units", () => {
		const limited = { ...voucher, amountRemaining: 0 };
		expect(voucherRequest(1, limited, limited.targets[0], 1)).toBeUndefined();
	});
	it("sends only an extra ID for extra entitlements", () => {
		const extra = { ...voucher, isExtra: true, targets: [{ extraId: 5, name: "Cart", unitsAvailable: 2, unitPrice: 50 }] };
		expect(voucherRequest(1, extra, extra.targets[0], 2)).toEqual({ bookingId: 1, grantId: "grant-1", extraId: 5, quantity: 2 });
	});
	it.each([VoucherRedemptionKind.Discount, VoucherRedemptionKind.Credit])("does not select an item for kind %s", (redemptionKind) =>
		expect(voucherRequest(1, { ...voucher, redemptionKind }, undefined, 20)).toEqual({ bookingId: 1, grantId: "grant-1", quantity: 1 })
	);
	it("rejects ineligible grants", () => expect(voucherRequest(1, { ...voucher, isEligible: false }, voucher.targets[0], 1)).toBeUndefined());
});
it("does not infer full payment from a successful partial payment or zero available funds", () => {
	expect(paymentMessage(false, 50)).toContain("Remaining balance");
	expect(paymentMessage(false, 0)).not.toContain("fully paid");
	expect(paymentMessage(true, 0)).toBe("Your booking is fully paid.");
});
