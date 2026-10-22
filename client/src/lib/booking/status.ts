import { BookingStatusEnum, type BookingDTO } from "#lib/api/index.js";
import type { BadgeVariant } from "@kayord/ui";

export const BOOKING_STATUS_OPTIONS = [
	{ value: BookingStatusEnum.Pending, label: "Pending" },
	{ value: BookingStatusEnum.Confirmed, label: "Confirmed" },
	{ value: BookingStatusEnum.Cancelled, label: "Cancelled" },
	{ value: BookingStatusEnum.Expired, label: "Expired" },
] as const;

export function isActiveReservation(booking: Pick<BookingDTO, "bookingStatusId" | "expiresAt">, now: number): boolean {
	return (
		booking.bookingStatusId === BookingStatusEnum.Confirmed || (booking.bookingStatusId === BookingStatusEnum.Pending && Date.parse(booking.expiresAt) > now)
	);
}

export const statusLabel = (id: number | undefined): string => BOOKING_STATUS_OPTIONS.find((option) => option.value === id)?.label ?? "Unknown";

export const statusBadgeVariant = (id: number | undefined): BadgeVariant => {
	switch (id) {
		case BookingStatusEnum.Confirmed:
			return "default";
		case BookingStatusEnum.Cancelled:
			return "destructive";
		case BookingStatusEnum.Expired:
			return "secondary";
		default:
			return "outline";
	}
};
