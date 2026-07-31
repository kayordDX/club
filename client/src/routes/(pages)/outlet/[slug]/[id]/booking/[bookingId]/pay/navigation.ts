import type { ResolvedPathname } from "$app/types";

type BookingNavigationData = {
	isPaid?: boolean;
	slotContractBookings?: Array<{
		slotContract?: {
			slotId?: string | null;
			slot?: {
				startDatetime?: string | null;
			} | null;
		} | null;
	}> | null;
};

type BasketNavigationOptions = {
	slug: string;
	facilityId: number;
	searchParams: URLSearchParams;
	booking?: BookingNavigationData | null;
};

const getBookingDate = (booking?: BookingNavigationData | null) => {
	const startDatetime = booking?.slotContractBookings?.[0]?.slotContract?.slot?.startDatetime;

	return startDatetime?.slice(0, 10) ?? "";
};

export const canReturnToBasket = (booking?: BookingNavigationData | null) => !booking?.isPaid;

export const getBasketUrl = ({
	slug,
	facilityId,
	searchParams,
	booking,
}: BasketNavigationOptions): ResolvedPathname => {
	const slotId =
		searchParams.get("slotId") ?? booking?.slotContractBookings?.[0]?.slotContract?.slotId ?? "";
	const date = searchParams.get("date") ?? getBookingDate(booking);
	const slotCount =
		searchParams.get("slotCount") ?? booking?.slotContractBookings?.length?.toString() ?? "";
	const pathname = slotId
		? `/outlet/${slug}/${facilityId}/slot/${slotId}`
		: `/outlet/${slug}/${facilityId}`;
	const params = new URLSearchParams();

	if (date) {
		params.set("date", date);
	}

	if (slotCount) {
		params.set("slotCount", slotCount);
	}

	return (params.size > 0 ? `${pathname}?${params.toString()}` : pathname) as ResolvedPathname;
};
