import { VoucherRedemptionKind, type WalletVoucherDTO } from "$lib/api/generated";

export function voucherStatus(voucher: WalletVoucherDTO, now: number): string {
	if (voucher.amountRemaining <= 0) return "Used up";
	if (Date.parse(voucher.expiryDate) <= now) return "Expired";
	if (Date.parse(voucher.grantedAt) > now) return "Not yet valid";
	if (!voucher.isWalletActive) return "Wallet inactive";
	if (voucher.currency !== "ZAR") return "Unsupported currency";
	return "Available";
}

export function groupVouchers(vouchers: WalletVoucherDTO[]) {
	const groups = new Map<string, WalletVoucherDTO[]>();
	for (const voucher of vouchers) {
		const key = voucher.redemptionKind === VoucherRedemptionKind.Entitlement ? `voucher-${voucher.voucherId}` : voucher.grantId;
		const grants = groups.get(key) ?? [];
		grants.push(voucher);
		groups.set(key, grants);
	}
	return Array.from(groups, ([key, grants]) => ({ key, voucher: grants[0], grants }));
}
