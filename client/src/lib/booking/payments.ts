import { VoucherRedemptionKind, type BookingVoucherDTO, type PaymentVoucherRequest, type VoucherTargetDTO } from "$lib/api";
import { formatCurrency } from "./format";

export function paymentErrorMessage(cause: unknown, fallback: string): string {
	if (cause instanceof Error) return cause.message;
	if (
		cause &&
		typeof cause === "object" &&
		"body" in cause &&
		cause.body &&
		typeof cause.body === "object" &&
		"message" in cause.body &&
		typeof cause.body.message === "string"
	) {
		return cause.body.message;
	}
	return fallback;
}

export function validatePaymentAmount(value: string, available: number): string | undefined {
	if (!/^\d+(\.\d{1,2})?$/.test(value) || Number(value) <= 0) {
		return "Enter a positive amount with at most two decimal places.";
	}
	if (Number(value) > available) return `You cannot pay more than the outstanding amount of ${formatCurrency(available)}.`;
}

export function paymentMessage(isPaid: boolean, outstanding: number): string {
	return isPaid
		? "Your booking is fully paid."
		: `Payment received. Remaining balance: ${formatCurrency(outstanding)}. You can pay the rest with another payment method.`;
}

export function voucherRequest(
	bookingId: number,
	voucher: BookingVoucherDTO,
	target: VoucherTargetDTO | undefined,
	quantity: number
): PaymentVoucherRequest | undefined {
	if (!voucher.isEligible) return;
	if (voucher.redemptionKind !== VoucherRedemptionKind.Entitlement) return { bookingId, grantId: voucher.grantId, quantity: 1 };
	const eligibleTarget =
		target &&
		voucher.targets.find((item) =>
			voucher.isExtra
				? target.extraId != null && item.extraId === target.extraId
				: target.slotContractBookingId != null && item.slotContractBookingId === target.slotContractBookingId
		);
	if (!eligibleTarget || !Number.isInteger(quantity) || quantity < 1 || quantity > Math.min(eligibleTarget.unitsAvailable, Math.floor(voucher.amountRemaining)))
		return;
	if (voucher.isExtra && eligibleTarget.extraId != null) return { bookingId, grantId: voucher.grantId, extraId: eligibleTarget.extraId, quantity };
	if (!voucher.isExtra && eligibleTarget.slotContractBookingId != null)
		return { bookingId, grantId: voucher.grantId, slotContractBookingId: eligibleTarget.slotContractBookingId, quantity };
}
