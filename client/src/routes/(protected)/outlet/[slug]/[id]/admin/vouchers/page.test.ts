import { render } from "svelte/server";
import { beforeEach, describe, expect, it, vi } from "vitest";
import VouchersPage from "./+page.svelte";
import { VoucherDiscountMode, VoucherRedemptionKind, type AdminVoucherDTO } from "$lib/api";

const mocks = vi.hoisted(() => ({
	query: { isPending: false, isError: false, isFetching: false, data: [] as unknown[], refetch: vi.fn() },
	invalidateQueries: vi.fn(),
}));
vi.mock("$app/environment", () => ({ browser: false }));
vi.mock("$app/state", () => ({ page: { params: { slug: "test-club", id: "7" } } }));
vi.mock("$lib/api/remote/admin.remote", () => ({
	adminVoucherGetAll: vi.fn(),
	adminVoucherCreate: vi.fn(),
	adminVoucherUpdate: vi.fn(),
	adminVoucherDelete: vi.fn(),
	adminVoucherIssue: vi.fn(),
}));
vi.mock("@tanstack/svelte-query", () => ({
	createQuery: vi.fn((options: () => unknown) => {
		options();
		return mocks.query;
	}),
	createMutation: vi.fn(() => ({ isPending: false, mutateAsync: vi.fn() })),
	useQueryClient: () => ({ invalidateQueries: mocks.invalidateQueries }),
}));

import { createQuery } from "@tanstack/svelte-query";
import { adminVoucherGetAll } from "$lib/api/remote/admin.remote";

beforeEach(() => {
	mocks.query.isPending = false;
	mocks.query.isError = false;
	mocks.query.data = [];
	vi.clearAllMocks();
});

const voucher: AdminVoucherDTO = {
	id: 1,
	name: "Complimentary round",
	description: "A facility-only benefit",
	isExtra: false,
	redemptionKind: VoucherRedemptionKind.Entitlement,
	isInUse: false,
};

describe("admin vouchers", () => {
	it("keys requests to the current facility without fetching during SSR", () => {
		const { body } = render(VouchersPage);
		expect(body).toContain("Vouchers");
		expect(body).toContain("No vouchers yet");
		expect(body).toContain("New voucher");
		const options = vi.mocked(createQuery).mock.calls.at(-1)?.[0];
		expect(typeof options === "function" ? options() : undefined).toMatchObject({ queryKey: ["admin-vouchers", 7], enabled: false });
		expect(adminVoucherGetAll).not.toHaveBeenCalled();
	});

	it("shows loading and retryable errors rather than an empty list", () => {
		mocks.query.isPending = true;
		expect(render(VouchersPage).body).toContain("Loading vouchers...");
		mocks.query.isPending = false;
		mocks.query.isError = true;
		const { body } = render(VouchersPage);
		expect(body).toContain("Unable to load vouchers");
		expect(body).toContain("Retry");
		expect(body).not.toContain("No vouchers yet");
	});

	it("displays voucher types, targets, discounts and the facility restriction", () => {
		mocks.query.data = [
			voucher,
			{ ...voucher, id: 2, name: "Cart credit", isExtra: true, redemptionKind: VoucherRedemptionKind.Credit },
			{
				...voucher,
				id: 3,
				name: "Member discount",
				redemptionKind: VoucherRedemptionKind.Discount,
				discountMode: VoucherDiscountMode.Percentage,
				discountValue: 20,
				maxDiscountAmount: 50,
			},
			{
				...voucher,
				id: 4,
				name: "Fixed discount",
				redemptionKind: VoucherRedemptionKind.Discount,
				discountMode: VoucherDiscountMode.FixedAmount,
				discountValue: 25,
			},
		];
		const { body } = render(VouchersPage);
		for (const text of [
			"Complimentary round",
			"Cart credit",
			"Member discount",
			"Fixed discount",
			"Entitlement",
			"Credit",
			"Discount",
			"Rounds only",
			"Extras only",
			"20%",
			"Maximum discount",
			"Search vouchers",
			"Usage",
			"Unused",
		])
			expect(body).toContain(text);
		expect(body).not.toContain("No vouchers yet");
		expect(body).toContain("<table");
		expect(body).toContain("Actions for Complimentary round");
	});
});
