import { expect, it, vi } from "vitest";
import { render } from "vitest-browser-svelte";
import { page } from "vitest/browser";
import PaymentAmountForm from "./payment-amount-form.svelte";

it("defaults to available funds and submits the chosen partial amount", async () => {
	const onpay = vi.fn().mockResolvedValue(undefined);
	render(PaymentAmountForm, { amountAvailable: 100, methods: [{ providerName: "payfast", type: "Payfast" }], busy: false, onpay });
	await expect.element(page.getByLabelText("Payment amount")).toHaveValue("100.00");
	await page.getByRole("radio", { name: "Payfast" }).click();
	await page.getByLabelText("Payment amount").fill("25.50");
	await page.getByRole("button", { name: "Pay now" }).click();
	expect(onpay).toHaveBeenCalledExactlyOnceWith("payfast", 25.5);
});
it("blocks invalid amounts even with a provider selected", async () => {
	render(PaymentAmountForm, { amountAvailable: 100, methods: [{ providerName: "payfast", type: "Payfast" }], busy: false, onpay: vi.fn() });
	await page.getByRole("radio", { name: "Payfast" }).click();
	for (const value of ["0", "1.001", "100.01"]) {
		await page.getByLabelText("Payment amount").fill(value);
		await expect.element(page.getByRole("button", { name: "Pay now" })).toBeDisabled();
		await expect.element(page.getByText(value === "100.01" ? /You cannot pay more than the outstanding amount/ : /Enter a positive amount/)).toBeVisible();
	}
});
