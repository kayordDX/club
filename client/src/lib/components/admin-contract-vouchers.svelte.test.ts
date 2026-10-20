import { expect, it, vi } from "vitest";
import { render } from "vitest-browser-svelte";
import { page } from "vitest/browser";
import AdminContractVouchers from "./admin-contract-vouchers.svelte";
import { VoucherRedemptionKind, type AdminContractVouchersDTO } from "$lib/api/generated/api.schemas";

const details: AdminContractVouchersDTO = {
	vouchers: [{ voucherId: 1, amount: 10 }],
	availableVouchers: [
		{ id: 1, name: "Free round", description: "One round per unit", isExtra: false, redemptionKind: VoucherRedemptionKind.Entitlement, isInUse: true },
		{ id: 2, name: "Member discount", description: "10% off", isExtra: true, redemptionKind: VoucherRedemptionKind.Discount, isInUse: false },
		{ id: 3, name: "Golf credit", description: "Rand credit", isExtra: false, redemptionKind: VoucherRedemptionKind.Credit, isInUse: false },
	],
};

it("prepopulates links, adds different vouchers and saves kind-specific amounts", async () => {
	const onsave = vi.fn().mockResolvedValue(undefined);
	render(AdminContractVouchers, { details, onsave, oncancel: vi.fn() });
	await expect.element(page.getByLabelText("Voucher 1", { exact: true })).toHaveTextContent("Free round");
	await expect.element(page.getByLabelText("Number of units")).toHaveValue("10");
	await page.getByRole("button", { name: "Add voucher" }).click();
	await expect.element(page.getByLabelText("Voucher 2", { exact: true })).toHaveTextContent("Member discount");
	await page.getByLabelText("Number of uses").fill("2");
	await page.getByLabelText("Voucher 2", { exact: true }).click();
	await expect.element(page.getByRole("option", { name: "Free round", exact: true })).not.toBeInTheDocument();
	await page.getByRole("option", { name: "Member discount", exact: true }).click();
	await page.getByRole("button", { name: "Add voucher" }).click();
	await page.getByLabelText("Credit amount (R)").fill("50.25");
	await expect.element(page.getByRole("button", { name: "Add voucher" })).toBeDisabled();
	await page.getByRole("button", { name: "Save vouchers" }).click();
	expect(onsave).toHaveBeenCalledExactlyOnceWith([
		{ voucherId: 1, amount: 10 },
		{ voucherId: 2, amount: 2 },
		{ voucherId: 3, amount: 50.25 },
	]);
});

it("removes links and allows saving an empty contract allowance", async () => {
	const onsave = vi.fn().mockResolvedValue(undefined);
	render(AdminContractVouchers, { details, onsave, oncancel: vi.fn() });
	await page.getByRole("button", { name: "Remove voucher 1", exact: true }).click();
	await expect.element(page.getByText("No vouchers linked to this contract.")).toBeVisible();
	await page.getByRole("button", { name: "Save vouchers" }).click();
	expect(onsave).toHaveBeenCalledExactlyOnceWith([]);
});

it("validates positive whole units and credit precision", async () => {
	const onsave = vi.fn();
	render(AdminContractVouchers, { details, onsave, oncancel: vi.fn() });
	for (const amount of ["", "0", "-2", "1.5"]) {
		await page.getByLabelText("Number of units").fill(amount);
		await page.getByRole("button", { name: "Save vouchers" }).click();
		await expect.element(page.getByRole("alert")).toHaveTextContent("Enter positive whole units/uses");
	}
	await page.getByLabelText("Voucher 1", { exact: true }).click();
	await page.getByRole("option", { name: "Golf credit", exact: true }).click();
	await page.getByLabelText("Credit amount (R)").fill("1.001");
	await page.getByRole("button", { name: "Save vouchers" }).click();
	expect(onsave).not.toHaveBeenCalled();
});

it("preserves input on rejection and prevents duplicate submission", async () => {
	let rejectSave!: (_error: Error) => void;
	const onsave = vi
		.fn()
		.mockImplementationOnce(
			() =>
				new Promise<void>((_resolve, reject) => {
					rejectSave = reject;
				})
		)
		.mockResolvedValue(undefined);
	render(AdminContractVouchers, { details, onsave, oncancel: vi.fn() });
	await page.getByLabelText("Number of units").fill("7");
	await page.getByRole("button", { name: "Save vouchers" }).click();
	await expect.element(page.getByRole("button", { name: "Saving…" })).toBeDisabled();
	await expect.element(page.getByRole("button", { name: "Cancel" })).toBeDisabled();
	await expect.element(page.getByLabelText("Number of units")).toBeDisabled();
	rejectSave(new Error("Server rejection"));
	await expect.element(page.getByRole("alert")).toHaveTextContent("Unable to save contract vouchers");
	await expect.element(page.getByLabelText("Number of units")).toHaveValue("7");
	await page.getByRole("button", { name: "Save vouchers" }).click();
	expect(onsave).toHaveBeenCalledTimes(2);
});

it("explains the empty catalog", async () => {
	render(AdminContractVouchers, { details: { vouchers: [], availableVouchers: [] }, onsave: vi.fn(), oncancel: vi.fn() });
	await expect.element(page.getByText(/No vouchers are available/)).toBeVisible();
	await expect.element(page.getByRole("button", { name: "Add voucher" })).toBeDisabled();
});
