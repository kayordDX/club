import { describe, expect, it } from "vitest";
import { VoucherDiscountMode, VoucherRedemptionKind } from "$lib/api";
import { AdminVoucherCreateBody, AdminVoucherIssueBody, AdminVoucherUpdateBody } from "./schemas/admin";

describe("admin voucher issue remote contract", () => {
	const grant = { voucherId: 1, amount: 2, validFrom: "2026-10-06T10:00:00Z", expiryDate: "2026-11-06T10:00:00Z" };

	it.each(["recipient@example.com", "+27821234567"])("accepts an exact recipient contact: %s", (recipient) => {
		expect(AdminVoucherIssueBody.safeParse({ ...grant, recipient }).success).toBe(true);
	});

	it("requires a recipient instead of a wallet ID", () => {
		expect(AdminVoucherIssueBody.safeParse({ ...grant, walletId: "00000000-0000-0000-0000-000000000001" }).success).toBe(false);
	});
});

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
