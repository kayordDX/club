import { expect, it, vi } from "vitest";
import { render } from "vitest-browser-svelte";
import { page } from "vitest/browser";
import VoucherSend from "./voucher-send.svelte";
import { VoucherRedemptionKind, type WalletVoucherDTO } from "#lib/api/index.js";

const grant: WalletVoucherDTO = {
	grantId: "gift",
	voucherId: 1,
	name: "Credit",
	description: "",
	facilityNames: [],
	gameTypes: [],
	extraNames: [],
	isExtra: false,
	redemptionKind: VoucherRedemptionKind.Credit,
	amountGranted: 100,
	amountRemaining: 75,
	grantedAt: "2020-01-01T00:00:00Z",
	expiryDate: "2099-01-01T00:00:00Z",
	currency: "ZAR",
	isWalletActive: true,
	canTransfer: true,
};

it("requires a recipient and disables repeat submission while sending", async () => {
	let finish!: () => void;
	const onsend = vi.fn(
		() =>
			new Promise<void>((resolve) => {
				finish = resolve;
			})
	);
	render(VoucherSend, { grant, onsend, oncancel: vi.fn() });
	await page.getByRole("button", { name: "Send voucher" }).click();
	await expect.element(page.getByText("Enter an email address or phone number.", { exact: true })).toBeVisible();
	expect(onsend).not.toHaveBeenCalled();
	await page.getByLabelText("Recipient email or phone").fill(" member@example.com ");
	await page.getByRole("button", { name: "Send voucher" }).click();
	await expect.element(page.getByRole("button", { name: "Sending…" })).toBeDisabled();
	await expect.element(page.getByRole("button", { name: "Cancel" })).toBeDisabled();
	await expect.element(page.getByLabelText("Recipient email or phone")).toBeDisabled();
	expect(onsend).toHaveBeenCalledExactlyOnceWith({ recipient: "member@example.com" });
	finish();
	await expect.element(page.getByRole("button", { name: "Send voucher" })).toBeEnabled();
});
