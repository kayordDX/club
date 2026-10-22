import { afterEach, expect, it, vi } from "vitest";
import { render } from "vitest-browser-svelte";
import { page } from "vitest/browser";
import type { ResolvedPathname } from "$app/types";
import type { AdminSlotGetAllResponse } from "#lib/api/index.js";
import "../../../layout.css";
import Slots from "./slots.svelte";

vi.mock("#lib/api/remote/slot.remote.js", () => ({ slotGetContracts: vi.fn() }));

const onBook = vi.fn();
const onCreateBooking = vi.fn();
const makeSlot = (id: string, resourceName: string | null, overrides: Partial<AdminSlotGetAllResponse> = {}): AdminSlotGetAllResponse => ({
	id,
	resourceName,
	startDatetime: "2026-08-04T09:00:00Z",
	endDatetime: "2026-08-04T09:10:00Z",
	booked: 1,
	total: 4,
	isEnabled: true,
	isAvailable: true,
	bookings: [],
	...overrides,
});
const slots = [makeSlot("first", "1st"), makeSlot("tenth", "10th"), makeSlot("later", "1st")];
const props = {
	slots,
	selectedDate: "2026-08-04",
	onPublicBook: onBook,
	onCreateBooking,
	bookingHref: (id: number) => `/bookings/${id}` as ResolvedPathname,
};

afterEach(async () => {
	vi.clearAllMocks();
	await page.viewport(1280, 720);
});

it("filters golf tees, announces the result and restores all slots", async () => {
	await render(Slots, props);
	await page.getByRole("button", { name: "10th", exact: true }).click();
	await expect.element(page.getByRole("button", { name: "10th", exact: true })).toHaveAttribute("aria-pressed", "true");
	await expect.element(page.getByRole("status")).toHaveTextContent("1 time slot · 10th · 1 available");
	await page.getByRole("button", { name: "Book", exact: true }).click();
	expect(onBook).toHaveBeenCalledWith("tenth", 1, "2026-08-04");
	await page.getByRole("button", { name: "All", exact: true }).click();
	await expect.element(page.getByRole("status")).toHaveTextContent("3 time slots · 3 available");
});

it("supports padel courts and keeps manager booking actions attached to the filtered slot", async () => {
	await render(Slots, { ...props, isAdmin: true, adminContext: true, slots: [makeSlot("court-1", "Court 1"), makeSlot("court-2", "Court 2")] });
	await page.getByRole("button", { name: "Court 2", exact: true }).click();
	await page.getByRole("button", { name: "Create booking", exact: true }).click();
	expect(onCreateBooking).toHaveBeenCalledWith("court-2", "2026-08-04");
});

it("falls back to all resources when the selected resource disappears on another day", async () => {
	const screen = await render(Slots, props);
	await page.getByRole("button", { name: "10th", exact: true }).click();
	await screen.rerender({ slots: [makeSlot("lane-1", "Lane 1"), makeSlot("lane-2", "Lane 2")], selectedDate: "2026-08-05" });
	await expect.element(page.getByRole("button", { name: "All", exact: true })).toHaveAttribute("aria-pressed", "true");
	await expect.element(page.getByRole("status")).toHaveTextContent("2 time slots · 2 available");
});

it("shows fully booked and disabled cards without allowing customer bookings", async () => {
	await render(Slots, {
		...props,
		slots: [makeSlot("full", "1st", { booked: 4, isAvailable: false }), makeSlot("disabled", "10th", { isEnabled: false })],
	});
	await page.getByRole("button", { name: "1st", exact: true }).click();
	await expect.element(page.getByText("No Slots Available", { exact: true })).toBeVisible();
	await expect.element(page.getByText("Fully booked", { exact: true })).toBeVisible();
	await expect.element(page.getByRole("button", { name: "Book", exact: true })).toBeDisabled();
	await page.getByRole("button", { name: "10th", exact: true }).click();
	await expect.element(page.getByText("Slot unavailable", { exact: true })).toBeVisible();
	await expect.element(page.getByRole("button", { name: "Book", exact: true })).toBeDisabled();
});

it("handles an empty day and a single unnamed resource without redundant filters", async () => {
	const screen = await render(Slots, { ...props, slots: [] });
	await expect.element(page.getByText("No Slots Available", { exact: true })).toBeVisible();
	await expect.element(page.getByRole("group", { name: "Filter by tee, court or lane" })).not.toBeInTheDocument();
	await screen.rerender({ slots: [makeSlot("unnamed", null)] });
	await expect.element(page.getByRole("group", { name: "Filter by tee, court or lane" })).not.toBeInTheDocument();
	await expect.element(page.getByRole("button", { name: "Book", exact: true })).toBeEnabled();
});

it("filters available slots on a single resource and restores unavailable cards", async () => {
	await render(Slots, {
		...props,
		slots: [
			makeSlot("open", "1st"),
			makeSlot("full", "1st", { booked: 4 }),
			makeSlot("disabled", "1st", { isEnabled: false }),
			makeSlot("unavailable", "1st", { isAvailable: false }),
		],
	});
	await page.getByRole("button", { name: "Filter by availability" }).click();
	await page.getByRole("option", { name: "Available only", exact: true }).click();
	await expect.element(page.getByRole("status")).toHaveTextContent("1 time slot · 1 available");
	await page.getByRole("button", { name: "Book", exact: true }).click();
	expect(onBook).toHaveBeenCalledWith("open", 1, "2026-08-04");
	await page.getByRole("button", { name: "Filter by availability" }).click();
	await page.getByRole("option", { name: "All slots", exact: true }).click();
	await expect.element(page.getByRole("status")).toHaveTextContent("4 time slots · 1 available");
});

it("combines minimum free spaces with resource filters for managers", async () => {
	await render(Slots, {
		...props,
		isAdmin: true,
		adminContext: true,
		slots: [makeSlot("group", "1st", { booked: 0 }), makeSlot("partial", "1st"), makeSlot("other-tee", "10th", { booked: 0 })],
	});
	await page.getByRole("button", { name: "1st", exact: true }).click();
	await page.getByRole("button", { name: "Filter by availability" }).click();
	await page.getByRole("option", { name: "4+ spaces available", exact: true }).click();
	await expect.element(page.getByRole("status")).toHaveTextContent("1 time slot · 1st · 1 available");
	await page.getByRole("button", { name: "Create booking", exact: true }).click();
	expect(onCreateBooking).toHaveBeenCalledWith("group", "2026-08-04");
});

it("shows an empty state when another day cannot accommodate the requested group", async () => {
	const screen = await render(Slots, props);
	await page.getByRole("button", { name: "Filter by availability" }).click();
	await page.getByRole("option", { name: "4+ spaces available", exact: true }).click();
	await expect.element(page.getByText("No Slots Available", { exact: true })).toBeVisible();
	await expect.element(page.getByRole("button", { name: "Book", exact: true })).not.toBeInTheDocument();
	await screen.rerender({ slots: [makeSlot("court", "Court 1", { total: 1, booked: 0 })] });
	await expect.element(page.getByRole("status")).toHaveTextContent("0 time slots · 0 available");
	await page.getByRole("button", { name: "Filter by availability" }).click();
	await page.getByRole("option", { name: "Available only", exact: true }).click();
	await expect.element(page.getByRole("status")).toHaveTextContent("1 time slot · 1 available");
});

it("shows availability without counts for single-capacity slots", async () => {
	const screen = await render(Slots, { ...props, slots: [makeSlot("single", "Court 1", { total: 1, booked: 0 })] });
	await expect.element(page.getByText("Available", { exact: true })).toBeVisible();
	await expect.element(page.getByText("1 of 1 available", { exact: true })).not.toBeInTheDocument();
	for (const overrides of [{ booked: 1 }, { isEnabled: false }, { isAvailable: false }]) {
		await screen.rerender({ slots: [makeSlot("single", "Court 1", { total: 1, booked: 0, ...overrides })] });
		await expect.element(page.getByText("Unavailable", { exact: true })).toBeVisible();
		await expect.element(page.getByRole("button", { name: "Book", exact: true })).toBeDisabled();
	}
});

it("keeps the price label, price trigger and booking action in one compact row", async () => {
	await page.viewport(375, 800);
	const { container } = await render(Slots, { ...props, slots: [makeSlot("compact", "1st")] });
	const priceLabel = Array.from(container.querySelectorAll("div")).find((element) => element.textContent === "Price")!;
	const priceTrigger = container.querySelector<HTMLButtonElement>('button[aria-label="View prices"]')!;
	const bookButton = Array.from(container.querySelectorAll("button")).find((button) => button.textContent?.trim() === "Book")!;
	const centerY = (element: Element) => {
		const rect = element.getBoundingClientRect();
		return rect.top + rect.height / 2;
	};
	expect(Math.abs(centerY(priceLabel) - centerY(priceTrigger))).toBeLessThanOrEqual(1);
	expect(Math.abs(centerY(priceTrigger) - centerY(bookButton))).toBeLessThanOrEqual(1);
	await expect.element(page.getByText("Booking", { exact: true })).not.toBeInTheDocument();
});

it("places compact cards side by side on desktop and stacks them without overflow on mobile", async () => {
	await page.viewport(1280, 800);
	const { container } = await render(Slots, props);
	const cards = () => Array.from(container.querySelectorAll<HTMLElement>('[data-slot="card"]'));
	const desktop = cards().map((card) => card.getBoundingClientRect());
	expect(desktop[0].top).toBe(desktop[1].top);
	expect(desktop[1].left).toBeGreaterThan(desktop[0].right);
	await page.viewport(375, 800);
	const mobile = cards().map((card) => card.getBoundingClientRect());
	expect(mobile[1].top).toBeGreaterThan(mobile[0].bottom);
	expect(document.documentElement.scrollWidth).toBeLessThanOrEqual(375);
	for (const card of cards()) expect(card.scrollWidth).toBeLessThanOrEqual(card.clientWidth);
});
