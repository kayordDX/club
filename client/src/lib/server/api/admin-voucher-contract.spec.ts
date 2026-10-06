import { describe, expect, it } from "vitest";
import { VoucherDiscountMode, VoucherRedemptionKind } from "$lib/api";
import { AdminVoucherCreateBody, AdminVoucherUpdateBody } from "./schemas/admin";

const voucher = {
	name: "Facility voucher",
	description: "",
	isExtra: false,
	redemptionKind: VoucherRedemptionKind.Entitlement,
	discountMode: null,
	discountValue: null,
	maxDiscountAmount: null,
};

describe.each([
	{ name: "create", schema: AdminVoucherCreateBody },
	{ name: "update", schema: AdminVoucherUpdateBody },
])("admin voucher $name remote contract", ({ schema }) => {
	it.each([VoucherRedemptionKind.Entitlement, VoucherRedemptionKind.Credit])(
		"accepts non-discount vouchers with null discount fields (%s)",
		(redemptionKind) => {
			expect(schema.safeParse({ ...voucher, redemptionKind }).success).toBe(true);
		}
	);

	it("accepts fixed discounts above 100 without applying the percentage limit", () => {
		expect(
			schema.safeParse({
				...voucher,
				redemptionKind: VoucherRedemptionKind.Discount,
				discountMode: VoucherDiscountMode.FixedAmount,
				discountValue: 150,
			}).success
		).toBe(true);
	});
});
