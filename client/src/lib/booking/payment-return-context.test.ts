import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { ResolvedPathname } from "$app/types";
import type { BookingPaymentDTO } from "$lib/api";
import { getPaymentReturn, matchesPaymentReturn, rememberPaymentReturn } from "./payment-return-context";

const admin = { facilityId: 1, returnUrl: "/outlet/test/1/admin/bookings/44" as ResolvedPathname };
const values = new Map<string, string>();
beforeEach(() => {
	values.clear();
	vi.stubGlobal("sessionStorage", {
		getItem: (key: string) => values.get(key) ?? null,
		setItem: (key: string, value: string) => values.set(key, value),
	});
});
afterEach(() => vi.unstubAllGlobals());

it("preserves the customer mapping and independently associates each transaction", () => {
	expect(rememberPaymentReturn("customer-tx", 123)).toBe(true);
	expect(rememberPaymentReturn("admin-tx", 44, admin)).toBe(true);
	expect(values.get("payment-booking:customer-tx")).toBe("123");
	expect(getPaymentReturn("customer-tx")).toEqual({ transactionId: "customer-tx", bookingId: 123 });
	expect(getPaymentReturn("admin-tx")).toEqual({ transactionId: "admin-tx", bookingId: 44, admin });
	expect(getPaymentReturn("unknown")).toBeUndefined();
});

it("supports previously stored customer transactions without trusting query-only booking IDs", () => {
	values.set("payment-booking:old-tx", "123");
	expect(getPaymentReturn("old-tx")).toEqual({ transactionId: "old-tx", bookingId: 123 });
	expect(getPaymentReturn("unknown", 123)).toBeUndefined();
	expect(getPaymentReturn("old-tx", 44)).toBeUndefined();
});

describe("internal management routes", () => {
	it.each([
		"https://evil.test/outlet/test/1/admin/bookings/44",
		"//evil.test/outlet/test/1/admin/bookings/44",
		"/outlet/test/2/admin/bookings/44",
		"/outlet/test/1/admin/bookings/45",
		"/outlet/test/1/booking/44/pay",
		"/outlet/test/1/admin/bookings/44?returnTo=https://evil.test",
		"/outlet/test/1/admin/bookings/44#fragment",
		"/outlet/test/1/admin/bookings/44/../45",
		"/outlet/test/01/admin/bookings/44",
		"/outlet/%2f%2fevil.test/1/admin/bookings/44",
		"/outlet/test\\evil/1/admin/bookings/44",
		"/outlet/test/1/admin/bookings/44\n",
	])("rejects unsafe or mismatched route %s on both write and read", (returnUrl) => {
		const invalid = { ...admin, returnUrl: returnUrl as ResolvedPathname };
		expect(rememberPaymentReturn("tx", 44, invalid)).toBe(false);
		values.set("payment-return:tx", JSON.stringify({ transactionId: "tx", bookingId: 44, admin: invalid }));
		values.set("payment-booking:tx", "44");
		expect(getPaymentReturn("tx")).toBeUndefined();
	});
});

it.each([0, -1, 1.5, NaN, Infinity, Number.MAX_SAFE_INTEGER + 1])("rejects invalid booking/facility IDs %s", (id) => {
	expect(rememberPaymentReturn("tx", id)).toBe(false);
	expect(rememberPaymentReturn("tx", 44, { ...admin, facilityId: id })).toBe(false);
});

it("rejects malformed, transaction-substituted and query-substituted contexts without customer fallback", () => {
	expect(rememberPaymentReturn("tx", 44, admin)).toBe(true);
	expect(getPaymentReturn("tx", 123)).toBeUndefined();
	expect(getPaymentReturn("tx", 44)?.admin).toEqual(admin);
	values.set("payment-return:tx", JSON.stringify({ transactionId: "other-tx", bookingId: 44, admin }));
	expect(getPaymentReturn("tx")).toBeUndefined();
	values.set("payment-return:tx", "{broken");
	expect(getPaymentReturn("tx")).toBeUndefined();
});

it("safely degrades when session storage is inaccessible", () => {
	vi.stubGlobal("sessionStorage", {
		getItem: () => {
			throw new Error("Blocked");
		},
		setItem: () => {
			throw new Error("Blocked");
		},
	});
	expect(rememberPaymentReturn("tx", 44, admin)).toBe(false);
	expect(getPaymentReturn("tx")).toBeUndefined();
});

it("requires both the booking ID and transaction in authorised history", () => {
	const context = { transactionId: "tx", bookingId: 44, admin };
	const balance: BookingPaymentDTO = { bookingId: 44, amountPaid: 0, amountOutstanding: 100, amountAvailable: 100, isPaid: false, payments: [] };
	expect(matchesPaymentReturn(context, balance)).toBe(false);
	const associated = {
		...balance,
		payments: [
			{
				id: 1,
				transactionId: "tx",
				amount: 100,
				paymentStatusId: 1,
				paymentStatus: "Pending",
				paymentTypeId: 1,
				paymentType: "Card",
				paymentStatusDate: "2026-08-02",
				providerName: "payfast",
			},
		],
	};
	expect(matchesPaymentReturn(context, associated)).toBe(true);
	expect(matchesPaymentReturn(context, { ...associated, bookingId: 123 })).toBe(false);
	expect(matchesPaymentReturn({ ...context, transactionId: "other-tx" }, associated)).toBe(false);
});
