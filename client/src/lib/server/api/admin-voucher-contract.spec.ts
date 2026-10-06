import { describe, expect, it } from "vitest";
import { VoucherDiscountMode, VoucherRedemptionKind } from "$lib/api";
import { AdminVoucherCreateBody } from "./schemas/admin";

const voucher = {
	name: "Facility voucher",
	description: "",
	isExtra: false,
	redemptionKind: VoucherRedemptionKind.Entitlement,
	discountMode: null,
	discountValue: null,
	maxDiscountAmount: null,
};

describe("admin voucher remote contract", () => {
	it.each([VoucherRedemptionKind.Entitlement, VoucherRedemptionKind.Credit])(
		"accepts non-discount vouchers with null discount fields (%s)",
		(redemptionKind) => {
			expect(AdminVoucherCreateBody.safeParse({ ...voucher, redemptionKind }).success).toBe(true);
		}
	);

	it("accepts fixed discounts above 100 without applying the percentage limit", () => {
		expect(
			AdminVoucherCreateBody.safeParse({
				...voucher,
				redemptionKind: VoucherRedemptionKind.Discount,
				discountMode: VoucherDiscountMode.FixedAmount,
				discountValue: 150,
			}).success
		).toBe(true);
	});
});
