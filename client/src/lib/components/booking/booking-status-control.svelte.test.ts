import { expect, it, vi } from "vitest";
import { render } from "vitest-browser-svelte";
import { page } from "vitest/browser";
import { BookingStatusEnum } from "$lib/api";
import BookingStatusControl from "./booking-status-control.svelte";

async function chooseStatus(name: string) {
	await page.getByRole("button", { name: "Change status", exact: true }).click();
	await page.getByRole("button", { name: "New booking status" }).click();
	await page.getByRole("option", { name, exact: true }).click();
}

it("requires an explicit save before changing status", async () => {
	const onChange = vi.fn().mockResolvedValue(undefined);
	render(BookingStatusControl, { status: BookingStatusEnum.Pending, onChange });
	await page.getByRole("button", { name: "Change status", exact: true }).click();
	await expect.element(page.getByRole("button", { name: "Save status" })).toBeDisabled();
	await page.getByRole("button", { name: "New booking status" }).click();
	await page.getByRole("option", { name: "Confirmed", exact: true }).click();
	expect(onChange).not.toHaveBeenCalled();
	await page.getByRole("button", { name: "Save status" }).click();
	expect(onChange).toHaveBeenCalledExactlyOnceWith(BookingStatusEnum.Confirmed);
	await expect.element(page.getByRole("dialog")).not.toBeInTheDocument();
});

it("allows cancelling without submitting a status change", async () => {
	const onChange = vi.fn().mockResolvedValue(undefined);
	render(BookingStatusControl, { status: BookingStatusEnum.Confirmed, onChange });
	await chooseStatus("Cancelled");
	await expect.element(page.getByText(/Cancelling releases the reserved places/)).toBeVisible();
	await page.getByRole("button", { name: "Cancel", exact: true }).click();
	expect(onChange).not.toHaveBeenCalled();
});

it("keeps the dialog open with an error when a status change fails", async () => {
	const onChange = vi.fn().mockRejectedValue(new Error("Not enough availability"));
	render(BookingStatusControl, { status: BookingStatusEnum.Cancelled, onChange });
	await chooseStatus("Confirmed");
	await page.getByRole("button", { name: "Save status" }).click();
	await expect.element(page.getByText("Not enough availability")).toBeVisible();
	await expect.element(page.getByRole("button", { name: "Save status" })).toBeEnabled();
});

it("disables repeat saves and cancellation while a change is pending", async () => {
	let finish!: () => void;
	const onChange = vi.fn(() => new Promise<void>((resolve) => (finish = resolve)));
	render(BookingStatusControl, { status: BookingStatusEnum.Pending, onChange });
	await chooseStatus("Confirmed");
	await page.getByRole("button", { name: "Save status" }).click();
	await expect.element(page.getByRole("button", { name: "Saving..." })).toBeDisabled();
	await expect.element(page.getByRole("button", { name: "Cancel", exact: true })).toBeDisabled();
	finish();
	await expect.element(page.getByRole("dialog")).not.toBeInTheDocument();
	expect(onChange).toHaveBeenCalledOnce();
});

it("disables status changes while booking details are being edited", async () => {
	render(BookingStatusControl, { status: BookingStatusEnum.Pending, disabled: true, onChange: vi.fn() });
	await expect.element(page.getByRole("button", { name: "Change status", exact: true })).toBeDisabled();
});
