import type { ResolvedPathname } from "$app/types";
import type { BookingPaymentDTO } from "$lib/api";

export type AdminPaymentReturn = { facilityId: number; returnUrl: ResolvedPathname };
export type PaymentReturnContext = {
	transactionId: string;
	bookingId: number;
	admin?: AdminPaymentReturn;
};

const validId = (id: unknown): id is number => typeof id === "number" && Number.isSafeInteger(id) && id > 0;
const validTransaction = (id: string) => typeof id === "string" && /^[a-zA-Z0-9_-]{1,256}$/.exec(id)?.[0] === id;
const contextKey = (transactionId: string) => `payment-return:${transactionId}`;

// This is a navigation hint, never an authorisation grant. Result pages must also
// find this transaction in the appropriate server-authorised booking history.
function validContext(value: unknown, transactionId: string): value is PaymentReturnContext {
	if (!value || typeof value !== "object") return false;
	const context = value as PaymentReturnContext;
	if (context.transactionId !== transactionId || !validId(context.bookingId)) return false;
	if (context.admin === undefined) return true;
	if (!context.admin || !validId(context.admin.facilityId) || typeof context.admin.returnUrl !== "string") return false;
	const route = /^\/outlet\/([a-zA-Z0-9][a-zA-Z0-9_-]*)\/([1-9]\d*)\/admin\/bookings\/([1-9]\d*)$/.exec(context.admin.returnUrl);
	return !!route && route[0] === context.admin.returnUrl && Number(route[2]) === context.admin.facilityId && Number(route[3]) === context.bookingId;
}

export function rememberPaymentReturn(transactionId: string, bookingId: number, admin?: AdminPaymentReturn): boolean {
	const context: PaymentReturnContext = { transactionId, bookingId, ...(admin ? { admin } : {}) };
	if (!validTransaction(transactionId) || !validContext(context, transactionId)) return false;
	try {
		sessionStorage.setItem(contextKey(transactionId), JSON.stringify(context));
		sessionStorage.setItem(`payment-booking:${transactionId}`, String(bookingId));
		return true;
	} catch {
		return false;
	}
}

export function getPaymentReturn(transactionId: string, requestedBookingId?: number): PaymentReturnContext | undefined {
	if (!validTransaction(transactionId)) return undefined;
	try {
		const stored = sessionStorage.getItem(contextKey(transactionId));
		// Do not fall back to the legacy customer mapping when a newer context is malformed.
		const context: unknown =
			stored === null ? { transactionId, bookingId: Number(sessionStorage.getItem(`payment-booking:${transactionId}`)) } : JSON.parse(stored);
		if (!validContext(context, transactionId)) return undefined;
		if (requestedBookingId !== undefined && requestedBookingId !== context.bookingId) return undefined;
		return context;
	} catch {
		return undefined;
	}
}

export function matchesPaymentReturn(context: PaymentReturnContext, balance: BookingPaymentDTO): boolean {
	return balance.bookingId === context.bookingId && balance.payments.some((payment) => payment.transactionId === context.transactionId);
}
