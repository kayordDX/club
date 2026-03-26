import type { SlotGetContractsResponse } from "$lib/api";

export type BookingPaymentMethod = "credit-card" | "pay-later";

export type BookingPlayer = {
	name: string;
	cellNo: string;
	email: string;
};

export type BookingDraft = {
	bookingId: number;
	slotId: string;
	facilityId: number;
	outletSlug: string;
	quantity: number;
	selectedDate?: string;
	selectedSlotLabel: string;
	selectedContract: SlotGetContractsResponse;
	players: BookingPlayer[];
	paymentMethod: BookingPaymentMethod;
	contactEmail: string;
	totalPrice: number;
	createdAt: string;
	updatedAt: string;
};

class BookingFlowStore {
	state: BookingDraft | null = $state(null);

	set(draft: BookingDraft) {
		this.state = draft;
	}

	clear() {
		this.state = null;
	}

	updatePlayers(players: BookingPlayer[]) {
		if (!this.state) return;

		this.state = {
			...this.state,
			players,
			contactEmail: players[0]?.email ?? this.state.contactEmail,
			updatedAt: new Date().toISOString(),
		};
	}

	updatePaymentMethod(paymentMethod: BookingPaymentMethod) {
		if (!this.state) return;

		this.state = {
			...this.state,
			paymentMethod,
			updatedAt: new Date().toISOString(),
		};
	}
	
	updateDraft(patch: Partial<BookingDraft>) {
		if (!this.state) return;

		this.state = {
			...this.state,
			...patch,
			updatedAt: new Date().toISOString(),
		};
	}
}

export const bookingFlow = new BookingFlowStore();
