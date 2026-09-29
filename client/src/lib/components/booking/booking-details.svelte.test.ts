import { expect, it } from "vitest";
import { render } from "vitest-browser-svelte";
import { page } from "vitest/browser";
import type { BookingDTO, BookingPathDTO } from "$lib/api";
import BookingDetails from "./booking-details.svelte";

const booking: BookingDTO = {
	id: 42,
	bookingStatusId: 4,
	bookingStatus: { id: 1, name: "Pending" },
	bookingStatusDate: "2026-08-01T10:00:00Z",
	expiresAt: "2026-08-01T10:10:00Z",
	isPaid: false,
	amountPaid: 50,
	amountOutstanding: 150,
	user: { firstName: "Test", lastName: "Customer" },
	slotContractBookings: [],
	extraBookings: [],
};
const path: BookingPathDTO = {
	bookingId: 42,
	outletId: 1,
	outletSlug: "club",
	outletName: "Test outlet",
	facilityId: 7,
	facilityName: "Test facility",
	slotId: "slot",
	slotStartDatetime: "2026-08-01T10:00:00Z",
};

it("shows the shared booking summary using the effective status", async () => {
	render(BookingDetails, { booking, path });
	await expect.element(page.getByText("Booking #42", { exact: true })).toBeVisible();
	await expect.element(page.getByText("Expired", { exact: true })).toBeVisible();
	await expect.element(page.getByText("Pending", { exact: true })).not.toBeInTheDocument();
	await expect.element(page.getByText("Test Customer", { exact: true })).toBeVisible();
	await expect.element(page.getByText("Test facility", { exact: true })).toBeVisible();
	await expect.element(page.getByText("Unpaid", { exact: true })).toBeVisible();
});

it("handles older bookings without slot data", async () => {
	render(BookingDetails, { booking });
	await expect.element(page.getByText("No players booked.")).toBeVisible();
	await expect.element(page.getByText("No extras booked.")).toBeVisible();
});

it("hides the read-only player and extras sections while editing", async () => {
	render(BookingDetails, { booking, showPlayers: false });
	await expect.element(page.getByText("No players booked.")).not.toBeInTheDocument();
	await expect.element(page.getByText("No extras booked.")).not.toBeInTheDocument();
	await expect.element(page.getByText("Amount outstanding", { exact: true })).toBeVisible();
});
