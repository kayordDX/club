import { describe, expect, it } from "vitest";

import { getBookingPayUrl } from "./payUrl";

const path = {
	bookingId: 1,
	outletId: 2,
	outletSlug: "ruimsig",
	outletName: "Ruimsig",
	facilityId: 7,
	facilityName: "Course",
	slotId: "slot-456",
	slotStartDatetime: "2026-08-01T09:15:00+02:00",
} as const;

describe("booking pay url", () => {
	it("builds the pay page url with date, slot and player count params", () => {
		expect(getBookingPayUrl(1, path, 3)).toBe("/outlet/ruimsig/7/booking/1/pay?date=2026-08-01&slotId=slot-456&slotCount=3");
	});

	it("omits the player count when unknown", () => {
		expect(getBookingPayUrl(1, path, 0)).toBe("/outlet/ruimsig/7/booking/1/pay?date=2026-08-01&slotId=slot-456");
	});
});
