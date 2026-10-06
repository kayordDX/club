import { expect, it, vi } from "vitest";
import { render } from "vitest-browser-svelte";
import { page } from "vitest/browser";
import AdminVoucherForm from "./admin-voucher-form.svelte";
import { VoucherDiscountMode, VoucherRedemptionKind } from "$lib/api/generated/api.schemas";

it("submits the defaults and allows targeting rounds as extras", async () => {
	const onsave = vi.fn().mockResolvedValue(undefined);
	render(AdminVoucherForm, { onsave, oncancel: vi.fn() });
	await page.getByLabelText("Name").fill("Free swim");
	await page.getByLabelText("Use for extras instead of rounds").click();
	await page.getByRole("button", { name: "Save voucher" }).click();
	expect(onsave).toHaveBeenCalledExactlyOnceWith({
		name: "Free swim",
		description: "",
		isExtra: true,
		redemptionKind: VoucherRedemptionKind.Entitlement,
		discountMode: null,
		discountValue: null,
		maxDiscountAmount: null,
	});
});

it("validates discount inputs and clears stale discount values for other kinds", async () => {
	const onsave = vi.fn().mockResolvedValue(undefined);
	render(AdminVoucherForm, { onsave, oncancel: vi.fn() });
	await page.getByLabelText("Name").fill("Discount");
	await page.getByLabelText("Redemption kind").selectOptions(String(VoucherRedemptionKind.Discount));
	await page.getByLabelText("Discount value").fill("100.001");
	await page.getByRole("button", { name: "Save voucher" }).click();
	expect(onsave).not.toHaveBeenCalled();
	await expect.element(page.getByText("Enter a positive amount with up to 2 decimal places.")).toBeVisible();
	await page.getByLabelText("Discount value").fill("101");
	await page.getByRole("button", { name: "Save voucher" }).click();
	expect(onsave).not.toHaveBeenCalled();
	await expect.element(page.getByText("Percentage cannot exceed 100.")).toBeVisible();
	await page.getByLabelText("Discount value").fill("15.50");
	await page.getByLabelText("Maximum discount amount (optional)").fill("20.999");
	await page.getByRole("button", { name: "Save voucher" }).click();
	expect(onsave).not.toHaveBeenCalled();
	await expect.element(page.getByText("Maximum discount must be a positive amount with up to 2 decimal places.")).toBeVisible();
	await page.getByLabelText("Maximum discount amount (optional)").fill("20.25");
	await page.getByLabelText("Maximum discount amount (optional)").fill("20.25");
	await page.getByRole("button", { name: "Save voucher" }).click();
	expect(onsave).toHaveBeenLastCalledWith(
		expect.objectContaining({
			discountMode: VoucherDiscountMode.Percentage,
			discountValue: 15.5,
			maxDiscountAmount: 20.25,
		})
	);
	await page.getByLabelText("Discount mode").selectOptions(String(VoucherDiscountMode.FixedAmount));
	await page.getByLabelText("Discount value").fill("25.00");
	await page.getByRole("button", { name: "Save voucher" }).click();
	expect(onsave).toHaveBeenLastCalledWith(expect.objectContaining({ discountMode: VoucherDiscountMode.FixedAmount, discountValue: 25 }));
	await page.getByLabelText("Redemption kind").selectOptions(String(VoucherRedemptionKind.Credit));
	await page.getByRole("button", { name: "Save voucher" }).click();
	expect(onsave).toHaveBeenLastCalledWith(expect.objectContaining({ discountMode: null, discountValue: null, maxDiscountAmount: null }));
});

it("disables controls during save and preserves entered data for retry after failure", async () => {
	let rejectSave!: (error: Error) => void;
	const onsave = vi
		.fn()
		.mockImplementationOnce(
			() =>
				new Promise<void>((_resolve, reject) => {
					rejectSave = reject;
				})
		)
		.mockResolvedValue(undefined);
	const oncancel = vi.fn();
	render(AdminVoucherForm, { onsave, oncancel });
	await page.getByLabelText("Name").fill("Preserved");
	await page.getByRole("button", { name: "Save voucher" }).click();
	await expect.element(page.getByRole("button", { name: "Saving…" })).toBeDisabled();
	await expect.element(page.getByLabelText("Name")).toBeDisabled();
	await expect.element(page.getByRole("button", { name: "Cancel" })).toBeDisabled();
	expect(oncancel).not.toHaveBeenCalled();
	rejectSave(new Error("rejected"));
	await expect.element(page.getByText("Unable to save voucher. Please try again.")).toBeVisible();
	await expect.element(page.getByLabelText("Name")).toHaveValue("Preserved");
	await page.getByRole("button", { name: "Save voucher" }).click();
	expect(onsave).toHaveBeenCalledTimes(2);
});
