import { expect, it } from "vitest";
import { BookingStatusEnum } from "#lib/api/index.js";
import { isActiveReservation } from "./status";

const now = Date.parse("2026-10-12T12:00:00Z");
it.each([
	[BookingStatusEnum.Confirmed, "2026-10-11T12:00:00Z", true],
	[BookingStatusEnum.Pending, "2026-10-12T12:00:01Z", true],
	[BookingStatusEnum.Pending, "2026-10-12T12:00:00Z", false],
	[BookingStatusEnum.Cancelled, "2026-10-13T12:00:00Z", false],
	[BookingStatusEnum.Expired, "2026-10-13T12:00:00Z", false],
] as const)("uses the same active reservation policy for checkout (%s, %s)", (bookingStatusId, expiresAt, active) => {
	expect(isActiveReservation({ bookingStatusId, expiresAt }, now)).toBe(active);
});
