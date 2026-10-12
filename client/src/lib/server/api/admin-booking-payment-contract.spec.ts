import { describe, expect, it } from "vitest";
import { AdminBookingInitiatePaymentBody } from "./schemas/admin";

describe("admin booking checkout remote contract", () => {
	it("accepts provider checkout without a customer booking ID or voucher grant", () => {
		expect(AdminBookingInitiatePaymentBody.safeParse({ providerName: "payfast", amount: 50 }).success).toBe(true);
	});

	it("allows the server to choose the available balance", () => {
		expect(AdminBookingInitiatePaymentBody.safeParse({ providerName: "payfast" }).success).toBe(true);
	});

	it("requires a supported-provider selection", () => {
		expect(AdminBookingInitiatePaymentBody.safeParse({ amount: 50 }).success).toBe(false);
	});
});
