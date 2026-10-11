import { afterEach, expect, it, vi } from "vitest";
import { render } from "vitest-browser-svelte";
import { page as browserPage } from "vitest/browser";
import type { ResolvedPathname } from "$app/types";
import AdminSlot from "./admin-slot.svelte";

const createBooking = vi.hoisted(() => vi.fn());
afterEach(() => vi.clearAllMocks());

const slot = {
	id: "slot/7",
	resourceName: "Court 1",
	startDatetime: "2026-08-04T09:00:00Z",
	endDatetime: "2026-08-04T10:00:00Z",
	booked: 1,
	total: 3,
	isEnabled: true,
	isAvailable: true,
	bookings: [{ bookingId: 42, playerName: "Sam Customer", bookingStatusId: 2, bookingStatusName: "Confirmed" }],
};

const commonProps = {
	selectedDate: "2026-08-04",
	adminContext: true,
	onCreateBooking: createBooking,
	bookingHref: (bookingId: number) => `/admin/bookings/${bookingId}` as ResolvedPathname,
};

it("shows unavailable slots and direct booking-management links", async () => {
	render(AdminSlot, { ...commonProps, slot: { ...slot, isEnabled: false } });
	await expect.element(browserPage.getByText("Sam Customer")).toBeVisible();
	await expect.element(browserPage.getByText("Slot unavailable")).toBeVisible();
	await expect.element(browserPage.getByRole("link", { name: "Manage booking #42" })).toHaveAttribute("href", "/admin/bookings/42");
});

it("shows booked and available capacity even when there are no bookings", async () => {
	render(AdminSlot, { ...commonProps, slot: { ...slot, booked: 0, bookings: [] } });
	await expect.element(browserPage.getByText("Booked 0 of 3 · 3 available")).toBeVisible();
});

it("starts admin creation from an available slot with the slot and selected date", async () => {
	render(AdminSlot, { ...commonProps, slot });
	await browserPage.getByRole("button", { name: "Create booking" }).click();
	expect(createBooking).toHaveBeenCalledWith("slot/7", "2026-08-04");
});

it("keeps full slots visible without offering creation", async () => {
	render(AdminSlot, { ...commonProps, slot: { ...slot, booked: 3, isAvailable: false } });
	await expect.element(browserPage.getByText("Fully booked")).toBeVisible();
	await expect.element(browserPage.getByRole("button", { name: "Create booking" })).not.toBeInTheDocument();
});
