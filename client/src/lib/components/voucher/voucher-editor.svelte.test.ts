import { expect, it, vi } from "vitest";
import { render } from "vitest-browser-svelte";
import { page } from "vitest/browser";
import VoucherEditor from "./voucher-editor.svelte";

it("submits multiple selected contract IDs through the shared form", async () => {
	const onSave = vi.fn(async () => {});
	const screen = await render(VoucherEditor, {
		contracts: [
			{ id: 1, name: "Member" },
			{ id: 2, name: "Visitor" },
		],
		extras: [],
		onSave,
		onCancel: vi.fn(),
	});
	await screen.getByRole("textbox", { name: "Name", exact: true }).fill("Rounds");
	await screen.getByRole("checkbox", { name: "Member", exact: true }).click();
	await screen.getByRole("checkbox", { name: "Visitor", exact: true }).click();
	await screen.getByRole("button", { name: "Create voucher" }).click();
	await vi.waitFor(() => expect(onSave).toHaveBeenCalledWith(expect.objectContaining({ name: "Rounds", contractIds: [1, 2], extraIds: [] })));
});

it("changing type to credit hides item lists and submits no item restrictions", async () => {
	const onSave = vi.fn(async () => {});
	const screen = await render(VoucherEditor, { contracts: [{ id: 1, name: "Member" }], extras: [], onSave, onCancel: vi.fn() });
	await screen.getByRole("textbox", { name: "Name", exact: true }).fill("Credit");
	await screen.getByRole("checkbox", { name: "Member", exact: true }).click();
	await screen.getByRole("button", { name: "Entitlement", exact: true }).click();
	await page.getByRole("option", { name: "Credit", exact: true }).click();
	await expect.element(screen.getByRole("checkbox", { name: "Member", exact: true })).not.toBeInTheDocument();
	await screen.getByRole("button", { name: "Create voucher" }).click();
	await vi.waitFor(() => expect(onSave).toHaveBeenCalledWith(expect.objectContaining({ redemptionKind: 2, contractIds: [], extraIds: [] })));
});

it("switching to extras submits only selected extra IDs", async () => {
	const onSave = vi.fn(async () => {});
	const screen = await render(VoucherEditor, {
		contracts: [{ id: 1, name: "Member" }],
		extras: [
			{ id: 3, name: "Cart" },
			{ id: 4, name: "Lunch" },
		],
		onSave,
		onCancel: vi.fn(),
	});
	await screen.getByRole("textbox", { name: "Name", exact: true }).fill("Extras");
	await screen.getByRole("checkbox", { name: "Member", exact: true }).click();
	await screen.getByRole("checkbox", { name: "Extra-only", exact: false }).click();
	await screen.getByRole("checkbox", { name: "Cart" }).click();
	await screen.getByRole("checkbox", { name: "Lunch" }).click();
	await screen.getByRole("button", { name: "Create voucher" }).click();
	await vi.waitFor(() => expect(onSave).toHaveBeenCalledWith(expect.objectContaining({ isExtra: true, contractIds: [], extraIds: [3, 4] })));
});
