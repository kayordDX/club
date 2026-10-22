import { expect, it, vi } from "vitest";
import { page } from "vitest/browser";
import { render } from "vitest-browser-svelte";
import Harness from "#lib/components/testing/booking-editor-harness.svelte";

vi.mock("#lib/auth", () => ({ useUser: () => ({ name: "Acting Admin" }) }));
vi.mock("#lib/api/remote/slot.remote.js", () => ({ slotGetAll: vi.fn(), slotGetContracts: vi.fn() }));
vi.mock("#lib/api/remote/extra.remote.js", () => ({ extraGetFacility: vi.fn() }));

it("retains drafts on destination changes, requires explicit corrections and previews destination prices", async () => {
	const save = vi.fn().mockResolvedValue(undefined);
	render(Harness, { onSubmit: save });
	await page.getByLabelText("Name").fill("Unsaved Player");
	await page.getByLabelText("Cell No").fill("0839999999");
	await expect.element(page.getByLabelText("Booking review").getByText("R 130,00")).toBeVisible();
	await page.getByRole("button", { name: "Change destination" }).click();
	await expect.element(page.getByLabelText("Name")).toHaveValue("Unsaved Player");
	await expect.element(page.getByLabelText("Cell No")).toHaveValue("0839999999");
	await expect.element(page.getByText("Correct selections before saving")).toBeVisible();
	await expect.element(page.getByRole("button", { name: "Save changes" })).toBeDisabled();
	await page.getByRole("button", { name: "Contract", exact: true }).click();
	await page.getByRole("option", { name: "Destination Visitor - R 150.00" }).click();
	await expect.element(page.getByRole("button", { name: "Save changes" })).toBeDisabled();
	await page.getByRole("button", { name: "Remove extra Cart" }).click();
	await expect.element(page.getByLabelText("Booking review").getByText("R 150,00")).toBeVisible();
	await page.getByRole("button", { name: "Save changes" }).click();
	expect(save).toHaveBeenCalledExactlyOnceWith({
		players: [{ name: "Unsaved Player", cellNo: "0839999999", email: "player@example.test", contractId: "41" }],
		extras: [],
	});
});

it("keeps contact drafts usable while destination options reload and blocks saving", async () => {
	render(Harness, { onSubmit: vi.fn() });
	await page.getByRole("button", { name: "Toggle loading" }).click();
	await page.getByLabelText("Email").fill("draft@example.test");
	await expect.element(page.getByRole("button", { name: "Save changes" })).toBeDisabled();
	await page.getByRole("button", { name: "Toggle loading" }).click();
	await expect.element(page.getByLabelText("Email")).toHaveValue("draft@example.test");
	await expect.element(page.getByRole("button", { name: "Save changes" })).toBeEnabled();
});

it("preserves an unchanged legacy contract when a preceding player is removed, but denies it to new players", async () => {
	render(Harness, { onSubmit: vi.fn(), legacy: true });
	await page.getByRole("button", { name: "Add player" }).click();
	await page.getByRole("button", { name: "Contract", exact: true }).click();
	await expect.element(page.getByRole("option", { name: "Legacy One - R 100.00" })).not.toBeInTheDocument();
	await expect.element(page.getByRole("option", { name: "Legacy Two - R 100.00" })).not.toBeInTheDocument();
	await page.getByRole("option", { name: "Public - R 200.00" }).click();
	await page.getByRole("button", { name: "Remove player 1" }).click();
	// Player Two's unchanged Legacy Two contract is retained even though its position shifted.
	await expect.element(page.getByText("Correct selections before saving")).not.toBeInTheDocument();
	await expect.element(page.getByRole("button", { name: "Save changes" })).toBeEnabled();
});

it("allows inactive reservation edits even when the slot is full", async () => {
	const save = vi.fn().mockResolvedValue(undefined);
	render(Harness, { onSubmit: save, inactive: true });
	await expect.element(page.getByRole("button", { name: "Save changes" })).toBeEnabled();
	await expect.element(page.getByRole("button", { name: "Add player" })).toBeEnabled();
	await page.getByRole("button", { name: "Save changes" }).click();
	expect(save).toHaveBeenCalledOnce();
	expect(save.mock.calls[0][0].extras[0].price).toBe(30);
});
