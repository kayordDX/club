import { beforeEach, expect, it, vi } from "vitest";
import { render } from "vitest-browser-svelte";
import { page } from "vitest/browser";
import Harness from "./testing/wallet-vouchers-harness.svelte";
import { VoucherDiscountMode, VoucherRedemptionKind, type WalletVoucherDTO } from "$lib/api/generated";

const api = vi.hoisted(() => ({ vouchers: vi.fn() }));
vi.mock("$app/env", () => ({ browser: true, dev: true, building: false, version: "test" }));
vi.mock("$lib/api/remote/wallet.remote", () => ({ walletVouchers: api.vouchers }));
const grant: WalletVoucherDTO = {
	grantId: "grant-1",
	voucherId: 1,
	name: "Member rounds",
	description: "Rounds for members",
	isExtra: false,
	redemptionKind: VoucherRedemptionKind.Entitlement,
	amountGranted: 3,
	amountRemaining: 3,
	grantedAt: "2020-01-01T00:00:00Z",
	expiryDate: "2099-01-01T00:00:00Z",
	currency: "ZAR",
	isWalletActive: true,
};
function respond(grants: WalletVoucherDTO[]) {
	api.vouchers.mockImplementation(() => Object.assign(Promise.resolve(grants), { refresh: vi.fn().mockResolvedValue(undefined) }));
}
beforeEach(() => {
	vi.clearAllMocks();
	respond([]);
});

it("groups matching entitlements with an available count and preserves each grant's expiry", async () => {
	respond([grant, { ...grant, grantId: "grant-2", amountRemaining: 2, expiryDate: "2099-02-01T00:00:00Z" }]);
	render(Harness);
	await expect.element(page.getByText("5 rounds available")).toBeVisible();
	await expect.element(page.getByText("Member rounds", { exact: true })).toBeVisible();
	await expect.element(page.getByText("Rounds only · 2 voucher grants")).toBeVisible();
	await expect.element(page.getByText("3 rounds remaining")).toBeVisible();
	await expect.element(page.getByText("2 rounds remaining")).toBeVisible();
	await expect.element(page.getByText(/Expires/)).toHaveLength(2);
});

it("lists unavailable grants without counting them as available entitlements", async () => {
	respond([
		grant,
		{ ...grant, grantId: "expired", expiryDate: "2020-02-01T00:00:00Z" },
		{ ...grant, grantId: "future", grantedAt: "2098-01-01T00:00:00Z" },
		{ ...grant, grantId: "used", amountRemaining: 0 },
		{ ...grant, grantId: "inactive", isWalletActive: false },
	]);
	render(Harness);
	await expect.element(page.getByText("3 rounds available")).toBeVisible();
	for (const status of ["Expired", "Not yet valid", "Used up", "Wallet inactive"]) {
		await expect.element(page.getByText(status, { exact: true })).toBeVisible();
	}
});

it("keeps credit and discount grants separate and shows their values", async () => {
	respond([
		{ ...grant, name: "Credit voucher", redemptionKind: VoucherRedemptionKind.Credit, amountRemaining: 150 },
		{ ...grant, grantId: "credit-2", name: "Credit voucher", redemptionKind: VoucherRedemptionKind.Credit, amountRemaining: 75 },
		{
			...grant,
			grantId: "discount",
			name: "Discount voucher",
			redemptionKind: VoucherRedemptionKind.Discount,
			discountMode: VoucherDiscountMode.Percentage,
			discountValue: 20,
			maxDiscountAmount: 50,
		},
		{
			...grant,
			grantId: "fixed",
			name: "Fixed voucher",
			redemptionKind: VoucherRedemptionKind.Discount,
			discountMode: VoucherDiscountMode.FixedAmount,
			discountValue: 25,
		},
		{ ...grant, grantId: "extra", voucherId: 2, name: "Cart voucher", isExtra: true },
	]);
	render(Harness);
	await expect.element(page.getByText("Credit voucher", { exact: true })).toHaveLength(2);
	await expect.element(page.getByText(/150,00 remaining/)).toBeVisible();
	await expect.element(page.getByText(/75,00 remaining/)).toBeVisible();
	await expect.element(page.getByText("20% discount")).toBeVisible();
	await expect.element(page.getByText(/Maximum discount:/)).toBeVisible();
	await expect.element(page.getByText(/25,00 discount/)).toBeVisible();
	await expect.element(page.getByText("3 extra units available")).toBeVisible();
});

it("shows an empty state instead of mock balances or transactions", async () => {
	render(Harness);
	await expect.element(page.getByText("No vouchers yet")).toBeVisible();
	await expect.element(page.getByText("Current balance")).not.toBeInTheDocument();
});

it("shows loading while fetching vouchers", async () => {
	api.vouchers.mockReturnValue(Object.assign(new Promise(() => {}), { refresh: vi.fn().mockResolvedValue(undefined) }));
	render(Harness);
	await expect.element(page.getByRole("status")).toHaveTextContent("Loading your vouchers...");
});

it("shows a recoverable error and refreshes the data on retry", async () => {
	api.vouchers.mockImplementation(() => Object.assign(Promise.resolve([]), { refresh: vi.fn().mockRejectedValue(new Error("Offline")) }));
	render(Harness);
	await expect.element(page.getByText("Unable to load vouchers")).toBeVisible();
	respond([grant]);
	await page.getByRole("button", { name: "Refresh" }).click();
	await expect.element(page.getByText("3 rounds available")).toBeVisible();
});
