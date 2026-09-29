import { describe, expect, it } from "vitest";
import { render } from "vitest-browser-svelte";
import VoucherItemSelection from "./voucher-item-selection.svelte";
import { VoucherRedemptionKind } from "$lib/api/generated/api.schemas";

const contracts = [
	{ id: 1, name: "Member round" },
	{ id: 2, name: "Visitor round" },
];
const extras = [
	{ id: 3, name: "Cart" },
	{ id: 4, name: "Lunch" },
];

describe("voucher item selections", () => {
	it("allows multiple contracts and deselection", async () => {
		const screen = await render(VoucherItemSelection, { kind: VoucherRedemptionKind.Entitlement, isExtra: false, contracts, extras, contractIds: [1] });
		await expect.element(screen.getByRole("checkbox", { name: "Member round" })).toBeChecked();
		await screen.getByRole("checkbox", { name: "Visitor round" }).click();
		await expect.element(screen.getByRole("checkbox", { name: "Visitor round" })).toBeChecked();
		await screen.getByRole("checkbox", { name: "Member round" }).click();
		await expect.element(screen.getByRole("checkbox", { name: "Member round" })).not.toBeChecked();
	});
	it("shows only extras for extra entitlements and explains empty lists", async () => {
		const screen = await render(VoucherItemSelection, { kind: VoucherRedemptionKind.Entitlement, isExtra: true, contracts, extras });
		await expect.element(screen.getByText("No selection means no eligible items.", { exact: false })).toBeVisible();
		await expect.element(screen.getByRole("checkbox", { name: "Member round" })).not.toBeInTheDocument();
		await screen.getByRole("checkbox", { name: "Cart" }).click();
		await screen.getByRole("checkbox", { name: "Lunch" }).click();
		await expect.element(screen.getByRole("checkbox", { name: "Cart" })).toBeChecked();
		await expect.element(screen.getByRole("checkbox", { name: "Lunch" })).toBeChecked();
	});
	it.each([VoucherRedemptionKind.Credit, VoucherRedemptionKind.Discount])("does not expose restrictions for non-entitlement %s", async (kind) => {
		const screen = await render(VoucherItemSelection, { kind, isExtra: false, contracts, extras });
		await expect.element(screen.getByRole("checkbox")).not.toBeInTheDocument();
		await expect.element(screen.getByText("Contract and extra selections do not restrict", { exact: false })).toBeVisible();
	});
});
