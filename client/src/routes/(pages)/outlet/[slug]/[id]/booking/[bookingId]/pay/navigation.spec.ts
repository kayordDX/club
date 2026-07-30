import { describe, expect, it } from "vitest";

import { canReturnToBasket, getBasketUrl } from "./navigation";

describe("payment navigation", () => {
	it("preserves the slot basket query parameters when present", () => {
		const searchParams = new URLSearchParams({
			slotId: "slot-123",
			slotCount: "4",
			date: "2026-07-30",
		});

		expect(
			getBasketUrl({
				slug: "ruimsig",
				facilityId: 7,
				searchParams,
			})
		).toBe("/outlet/ruimsig/7/slot/slot-123?date=2026-07-30&slotCount=4");
	});

	it("falls back to booking data when the payment page query string is missing", () => {
		expect(
			getBasketUrl({
				slug: "ruimsig",
				facilityId: 7,
				searchParams: new URLSearchParams(),
				booking: {
					slotContractBookings: [
						{
							slotContract: {
								slotId: "slot-456",
								slot: {
									startDatetime: "2026-08-01T09:15:00+02:00",
								},
							},
						},
						{
							slotContract: {
								slotId: "slot-456",
							},
						},
					],
				},
			})
		).toBe("/outlet/ruimsig/7/slot/slot-456?date=2026-08-01&slotCount=2");
	});

	it("only allows returning to the basket while the booking is unpaid", () => {
		expect(canReturnToBasket({ isPaid: false })).toBe(true);
		expect(canReturnToBasket({ isPaid: true })).toBe(false);
	});
});
