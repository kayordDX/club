import type { ResolvedPathname } from "$app/types";
import type { BookingPathDTO } from "$lib/api";

/**
 * Builds the pay page URL for a booking using the outlet/facility path resolved
 * from the database. Used after editing a booking so the payment flow is the
 * same no matter where the edit page was opened from.
 */
export const getBookingPayUrl = (bookingId: number, path: BookingPathDTO, slotCount: number): ResolvedPathname => {
	const params = new URLSearchParams();

	if (path.slotStartDatetime) {
		params.set("date", path.slotStartDatetime.slice(0, 10));
	}

	if (path.slotId) {
		params.set("slotId", path.slotId);
	}

	if (slotCount > 0) {
		params.set("slotCount", String(slotCount));
	}

	const query = params.toString();

	return `/outlet/${path.outletSlug}/${path.facilityId}/booking/${bookingId}/pay${query ? `?${query}` : ""}` as ResolvedPathname;
};
