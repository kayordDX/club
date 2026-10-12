import { createCipheriv, createHash, randomBytes } from "node:crypto";
import type { Page } from "@playwright/test";

export const paymentApiUrl = "http://127.0.0.1:5194";
export const paymentAppUrl = "http://127.0.0.1:5174";

// Only the isolated fixture app knows this key; production has no test auth bypass.
export async function authenticatePaymentSession(page: Page, actor: "manager" | "owner" | "other" = "manager") {
	const iv = randomBytes(12);
	const cipher = createCipheriv("aes-256-gcm", createHash("sha256").update("isolated-payment-playwright-secret").digest(), iv);
	const payload = JSON.stringify({
		user:
			actor === "manager"
				? { sub: "payment-tester", username: "payment-tester", name: "Payment Tester", email: "tester@example.test" }
				: {
						sub: actor === "owner" ? "00000000-0000-4000-8000-000000000099" : "00000000-0000-4000-8000-000000000098",
						username: actor,
						name: actor === "owner" ? "Casey Customer" : "Other Customer",
						email: `${actor}@example.test`,
					},
		tokens: {
			accessToken: actor === "manager" ? "payment-test-token" : actor === "owner" ? "customer-test-token" : "other-customer-test-token",
			expiresAt: Date.now() + 3600000,
		},
	});
	const encrypted = Buffer.concat([cipher.update(payload, "utf8"), cipher.final()]);
	await page.context().addCookies([
		{
			name: "sid",
			value: `${iv.toString("base64url")}.${encrypted.toString("base64url")}.${cipher.getAuthTag().toString("base64url")}`,
			url: paymentAppUrl,
			httpOnly: true,
			sameSite: "Lax",
		},
	]);
}
