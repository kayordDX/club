import { BookingStatusEnum } from "$lib/api";
import type { BadgeVariant } from "@kayord/ui";

export const BOOKING_STATUS_OPTIONS = [
	{ value: BookingStatusEnum.Pending, label: "Pending" },
	{ value: BookingStatusEnum.Confirmed, label: "Confirmed" },
	{ value: BookingStatusEnum.Cancelled, label: "Cancelled" },
	{ value: BookingStatusEnum.Expired, label: "Expired" },
] as const;

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
