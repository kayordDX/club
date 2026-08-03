import { describe, expect, it } from "vitest";

import { buildExtras, buildPlayers } from "./bookingForm";

const booking = {
	id: 1,
	bookingStatusId: 1,
	bookingStatus: { id: 1, name: "Pending" },
	bookingStatusDate: "2026-08-01T09:15:00+02:00",
	userId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
	user: { firstName: "Test", lastName: "User" },
	isPaid: false,
	amountOutstanding: 700,
	amountPaid: 0,
	expiresAt: "2026-08-01T09:25:00+02:00",
	slotContractBookings: [
		{
			id: 11,
			slotContractId: 5,
			slotContract: {
				id: 5,
				slotId: "slot-456",
				slot: {
					id: "slot-456",
					startDatetime: "2026-08-01T09:15:00+02:00",
					endDatetime: "2026-08-01T10:15:00+02:00",
					maxBookings: 4,
				},
				contractId: 2,
				price: 100,
				canPayLater: false,
				description: "Guest 18 Holes",
			},
			bookingId: 1,
			userId: null,
			name: "Jaco Taute",
			email: "jaco@example.com",
			cellphone: "0842502311",
		},
	],
	extraBookings: [
		{
			extraId: 3,
			extra: { id: 3, facilityId: 7, outletId: 2, name: "Golf Cart", code: "CART", price: 300 },
			bookingId: 1,
			amount: 2,
		},
	],
} as never;

describe("booking edit form defaults", () => {
	it("maps slot contract bookings to player form values", () => {
		expect(buildPlayers(booking)).toEqual([
			{
				name: "Jaco Taute",
				cellNo: "0842502311",
				email: "jaco@example.com",
				contractId: "5",
			},
		]);
	});

	it("falls back to an empty player list without a booking", () => {
		expect(buildPlayers(null)).toEqual([]);
	});

	it("maps extra bookings to selected extras", () => {
		expect(buildExtras(booking)).toEqual([
			{
				id: 3,
				name: "Golf Cart",
				price: 300,
				amount: 2,
			},
		]);
	});

	it("falls back to an empty extras list without a booking", () => {
		expect(buildExtras(undefined)).toEqual([]);
	});
});
