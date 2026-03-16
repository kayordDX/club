import type { BookingCreateResponse } from "$lib/api";

export type BookingPlayer = {
	name: string;
	email: string;
};

export type BookingFlowState = {
	booking: BookingCreateResponse;
	players: BookingPlayer[];
};

class BookingFlowStore {
	state: BookingFlowState | null = $state(null);

	set(booking: BookingCreateResponse, players: BookingPlayer[]) {
		this.state = {
			booking,
			players,
		};
	}

	clear() {
		this.state = null;
	}

	updatePlayers(players: BookingPlayer[]) {
		if (!this.state) return;

		this.state = {
			...this.state,
			players,
		};
	}
}

export const bookingFlow = new BookingFlowStore();
