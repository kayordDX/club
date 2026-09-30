import { describe, expect, it } from "vitest";
import { VoucherRedemptionKind, type WalletVoucherDTO } from "$lib/api/generated";
import { groupVouchers, voucherStatus } from "./vouchers";

const now = Date.parse("2026-10-01T00:00:00Z");
const grant: WalletVoucherDTO = {
	grantId: "grant-1",
	voucherId: 1,
	name: "Rounds",
	description: "Member rounds",
	facilityNames: [],
	gameTypes: [],
	extraNames: [],
	isExtra: false,
	redemptionKind: VoucherRedemptionKind.Entitlement,
	amountGranted: 5,
	amountRemaining: 3,
	grantedAt: "2026-10-01T00:00:00Z",
	expiryDate: "2026-11-01T00:00:00Z",
	currency: "ZAR",
	isWalletActive: true,
};

describe("voucher availability", () => {
	it.each([
		[{}, "Available"],
		[{ amountRemaining: 0 }, "Used up"],
		[{ expiryDate: "2026-10-01T00:00:00Z" }, "Expired"],
		[{ grantedAt: "2026-10-01T00:00:01Z" }, "Not yet valid"],
		[{ isWalletActive: false }, "Wallet inactive"],
		[{ currency: "USD" }, "Unsupported currency"],
	])("determines status from grant and wallet (%s)", (changes, status) => {
		expect(voucherStatus({ ...grant, ...changes }, now)).toBe(status);
	});
});

it("groups entitlement grants by definition ID, not display name", () => {
	const groups = groupVouchers([grant, { ...grant, grantId: "grant-2" }, { ...grant, grantId: "grant-3", voucherId: 2 }]);
	expect(groups).toHaveLength(2);
	expect(groups[0].grants.map((item) => item.grantId)).toEqual(["grant-1", "grant-2"]);
	expect(groups[1].grants.map((item) => item.grantId)).toEqual(["grant-3"]);
});

it.each([VoucherRedemptionKind.Credit, VoucherRedemptionKind.Discount])("does not aggregate separate %s grants", (redemptionKind) => {
	expect(
		groupVouchers([
			{ ...grant, redemptionKind },
			{ ...grant, redemptionKind, grantId: "grant-2" },
		])
	).toHaveLength(2);
});
