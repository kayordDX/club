import type { BookingDTO } from "$lib/api";
import type { PlayerDraft } from "$lib/booking/players";
import type { SelectedExtra } from "../../../outlet/[slug]/[id]/slot/[slot]/schema";

export type PlayerFormValues = PlayerDraft;

/**
 * Maps the current booking's slot contract bookings to tanstack form player values.
 * Contract ids are kept as strings because the booking form schema stores them that way.
 */
export const buildPlayers = (booking?: BookingDTO | null): PlayerFormValues[] =>
	booking?.slotContractBookings.map((player) => ({
		name: player.name ?? "",
		cellNo: player.cellphone ?? "",
		email: player.email ?? "",
		contractId: player.slotContractId.toString(),
	})) ?? [];

/**
 * Maps the current booking's extras to the selected extras state used by the extras picker.
 */
export const buildExtras = (booking?: BookingDTO | null): SelectedExtra[] =>
	booking?.extraBookings.map((extra) => ({
		id: extra.extraId,
		name: extra.extra.name,
		price: extra.extra.price,
		amount: extra.amount,
	})) ?? [];
