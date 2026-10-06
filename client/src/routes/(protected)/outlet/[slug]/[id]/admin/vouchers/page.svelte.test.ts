import { beforeEach, expect, it, vi } from "vitest";
import { render } from "vitest-browser-svelte";
import { page } from "vitest/browser";
import Harness from "./voucher-page-harness.svelte";
import { VoucherRedemptionKind, type AdminVoucherDTO } from "$lib/api";

const api = vi.hoisted(() => ({ list: vi.fn(), create: vi.fn(), update: vi.fn(), delete: vi.fn() }));
vi.mock("$app/environment", () => ({ browser: true, dev: true, building: false, version: "test" }));
vi.mock("$app/state", () => ({ page: { params: { id: "7", slug: "test-club" } } }));
vi.mock("$lib/api/remote/admin.remote", () => ({
	adminVoucherGetAll: api.list,
	adminVoucherCreate: api.create,
	adminVoucherUpdate: api.update,
	adminVoucherDelete: api.delete,
}));
const voucher: AdminVoucherDTO = {
	id: 1,
	name: "Free round",
	description: "Complimentary round",
	isExtra: false,
	redemptionKind: VoucherRedemptionKind.Entitlement,
	isInUse: false,
};
let rows: AdminVoucherDTO[];
beforeEach(() => {
	vi.resetAllMocks();
	rows = [{ ...voucher }];
	api.list.mockImplementation(() => Object.assign(Promise.resolve(structuredClone(rows)), { refresh: vi.fn().mockResolvedValue(undefined) }));
	api.create.mockImplementation(async ({ body }) => {
		const created = { id: rows.length + 1, isInUse: false, ...body };
		rows.push(created);
		return created;
	});
	api.update.mockImplementation(async ({ id, body }) => {
		rows = rows.map((row) => (row.id === id ? { ...row, ...body } : row));
	});
	api.delete.mockImplementation(async ({ id }) => {
		rows = rows.filter((row) => row.id !== id);
	});
});

it("displays a sortable paginated grid and resets pagination when searching", async () => {
	rows = Array.from({ length: 12 }, (_, index) => ({ ...voucher, id: index + 1, name: `Voucher ${String(index + 1).padStart(2, "0")}` }));
	render(Harness);
	await expect.element(page.getByRole("table")).toBeVisible();
	await expect.element(page.getByText("Page 1 of 2")).toBeVisible();
	await expect.element(page.getByRole("cell", { name: "Voucher 01", exact: true })).toBeVisible();
	await page.getByRole("button", { name: "Go to next page", exact: true }).click();
	await expect.element(page.getByText("Page 2 of 2")).toBeVisible();
	await page.getByLabelText("Search vouchers").fill("Voucher 01");
	await expect.element(page.getByText("Page 1 of 1")).toBeVisible();
	await expect.element(page.getByRole("cell", { name: "Voucher 01", exact: true })).toBeVisible();
	await expect.element(page.getByRole("cell", { name: "Voucher 12", exact: true })).not.toBeInTheDocument();
	await page.getByLabelText("Search vouchers").fill("");
	await page.getByRole("button", { name: "Sort by name", exact: true }).click();
	await expect.element(page.getByRole("row").nth(1)).toHaveTextContent("Voucher 12");
});

it("creates a voucher from the grid toolbar and refreshes the rows", async () => {
	render(Harness);
	await page.getByRole("button", { name: "New voucher", exact: true }).click();
	await page.getByRole("textbox", { name: "Name", exact: true }).fill("New round");
	await page.getByRole("button", { name: "Save voucher" }).click();
	await expect.element(page.getByRole("cell", { name: "New round", exact: true })).toBeVisible();
	expect(api.create).toHaveBeenCalledExactlyOnceWith({
		facilityId: 7,
		body: { name: "New round", description: "", isExtra: false, redemptionKind: 1, discountMode: null, discountValue: null, maxDiscountAmount: null },
	});
});

it("edits an existing voucher with its facility and ID and refreshes the grid", async () => {
	render(Harness);
	await page.getByRole("button", { name: "Actions for Free round" }).click();
	await page.getByRole("menuitem", { name: "Edit", exact: true }).click();
	await expect.element(page.getByRole("textbox", { name: "Name", exact: true })).toHaveValue("Free round");
	await page.getByRole("textbox", { name: "Name", exact: true }).fill("Renamed round");
	await page.getByRole("button", { name: "Save changes" }).click();
	await expect.element(page.getByRole("cell", { name: "Renamed round", exact: true })).toBeVisible();
	expect(api.update).toHaveBeenCalledExactlyOnceWith({
		facilityId: 7,
		id: 1,
		body: {
			name: "Renamed round",
			description: "Complimentary round",
			isExtra: false,
			redemptionKind: 1,
			discountMode: null,
			discountValue: null,
			maxDiscountAmount: null,
		},
	});
});

it("keeps an unsuccessful edit open with entered values for retry", async () => {
	api.update.mockRejectedValueOnce(new Error("Conflict"));
	render(Harness);
	await page.getByRole("button", { name: "Actions for Free round" }).click();
	await page.getByRole("menuitem", { name: "Edit", exact: true }).click();
	await page.getByRole("textbox", { name: "Name", exact: true }).fill("Retry round");
	await page.getByRole("button", { name: "Save changes" }).click();
	await expect.element(page.getByText("Unable to save voucher. Please try again.")).toBeVisible();
	await expect.element(page.getByRole("textbox", { name: "Name", exact: true })).toHaveValue("Retry round");
	await page.getByRole("button", { name: "Save changes" }).click();
	await expect.element(page.getByRole("cell", { name: "Retry round", exact: true })).toBeVisible();
	expect(api.update).toHaveBeenCalledTimes(2);
});

it("prevents duplicate deletion and cancellation while deletion is pending", async () => {
	let finish!: () => void;
	const gate = new Promise<void>((resolve) => {
		finish = resolve;
	});
	api.delete.mockImplementationOnce(async ({ id }) => {
		await gate;
		rows = rows.filter((row) => row.id !== id);
	});
	render(Harness);
	await page.getByRole("button", { name: "Actions for Free round" }).click();
	await page.getByRole("menuitem", { name: "Delete", exact: true }).click();
	await page.getByRole("button", { name: "Delete", exact: true }).click();
	await expect.element(page.getByRole("button", { name: "Deleting...", exact: true })).toBeDisabled();
	await expect.element(page.getByRole("button", { name: "Cancel", exact: true })).toBeDisabled();
	expect(api.delete).toHaveBeenCalledTimes(1);
	finish();
	await expect.element(page.getByText("No vouchers yet")).toBeVisible();
});

it("requires deletion confirmation and removes the row only after success", async () => {
	render(Harness);
	await page.getByRole("button", { name: "Actions for Free round" }).click();
	await page.getByRole("menuitem", { name: "Delete", exact: true }).click();
	await expect.element(page.getByRole("alertdialog")).toBeVisible();
	expect(api.delete).not.toHaveBeenCalled();
	await page.getByRole("button", { name: "Cancel", exact: true }).click();
	await expect.element(page.getByRole("alertdialog")).not.toBeInTheDocument();
	expect(api.delete).not.toHaveBeenCalled();
	await page.getByRole("button", { name: "Actions for Free round" }).click();
	await page.getByRole("menuitem", { name: "Delete", exact: true }).click();
	await page.getByRole("button", { name: "Delete", exact: true }).click();
	await expect.element(page.getByText("No vouchers yet")).toBeVisible();
	expect(api.delete).toHaveBeenCalledExactlyOnceWith({ facilityId: 7, id: 1 });
});

it("keeps a failed deletion open for retry without losing the voucher", async () => {
	api.delete.mockRejectedValueOnce(new Error("Conflict"));
	render(Harness);
	await page.getByRole("button", { name: "Actions for Free round" }).click();
	await page.getByRole("menuitem", { name: "Delete", exact: true }).click();
	await page.getByRole("button", { name: "Delete", exact: true }).click();
	await expect.element(page.getByRole("alert")).toHaveTextContent(/Unable to delete this voucher/);
	await expect.element(page.getByRole("alertdialog")).toBeVisible();
	await page.getByRole("button", { name: "Delete", exact: true }).click();
	await expect.element(page.getByText("No vouchers yet")).toBeVisible();
	expect(api.delete).toHaveBeenCalledTimes(2);
});

it("disables deletion of used vouchers but allows editing their metadata", async () => {
	rows[0].isInUse = true;
	render(Harness);
	await page.getByRole("button", { name: "Actions for Free round" }).click();
	await expect.element(page.getByRole("menuitem", { name: "Delete", exact: true })).toHaveAttribute("aria-disabled", "true");
	await page.getByRole("menuitem", { name: "Edit", exact: true }).click();
	await expect.element(page.getByLabelText("Redemption kind")).toBeDisabled();
	await page.getByRole("textbox", { name: "Description", exact: true }).fill("Renamed benefit");
	await page.getByRole("button", { name: "Save changes" }).click();
	await expect.element(page.getByRole("cell", { name: "Renamed benefit", exact: true })).toBeVisible();
	expect(api.delete).not.toHaveBeenCalled();
});
