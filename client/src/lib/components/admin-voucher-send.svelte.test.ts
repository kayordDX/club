import { expect, it, vi } from "vitest";
import { render } from "vitest-browser-svelte";
import { page } from "vitest/browser";
import AdminVoucherSend from "./admin-voucher-send.svelte";
import { VoucherRedemptionKind, type AdminVoucherDTO } from "$lib/api";

const voucher: AdminVoucherDTO = {
	id: 9,
	name: "Credit voucher",
	description: "",
	isExtra: false,
	redemptionKind: VoucherRedemptionKind.Credit,
	isInUse: false,
};

it("submits a credit amount with two decimal places", async () => {
	const onsend = vi.fn().mockResolvedValue(undefined);
	render(AdminVoucherSend, { voucher, onsend, oncancel: vi.fn() });
	await page.getByLabelText("Recipient email or phone").fill("member@example.com");
	await page.getByLabelText("Amount (ZAR)").fill("12.35");
	await page.getByRole("button", { name: "Send voucher" }).click();
	expect(onsend).toHaveBeenCalledExactlyOnceWith(expect.objectContaining({ voucherId: 9, recipient: "member@example.com", amount: 12.35 }));
});

it("requires a recipient and a valid future expiry date", async () => {
	const onsend = vi.fn();
	render(AdminVoucherSend, { voucher, onsend, oncancel: vi.fn() });
	await page.getByRole("button", { name: "Send voucher" }).click();
	await expect.element(page.getByText("Enter an email address or phone number.", { exact: true })).toBeVisible();
	await page.getByLabelText("Recipient email or phone").fill("member@example.com");
	await page.getByLabelText("Expiry date").fill("2000-01-01");
	await page.getByRole("button", { name: "Send voucher" }).click();
	await expect.element(page.getByText("Expiry date must be in the future.", { exact: true })).toBeVisible();
	expect(onsend).not.toHaveBeenCalled();
});
