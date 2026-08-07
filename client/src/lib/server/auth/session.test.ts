import { describe, expect, it } from "vitest";
import type { Cookies } from "@sveltejs/kit";
import { consumePendingLogin, readSession, setPendingLogin, writeSession, clearSession } from "./session";
import type { SessionPayload } from "./session";

/** Minimal in-memory Cookies double. */
function fakeCookies(): Cookies {
	const store = new Map<string, string>();
	const cookie: Cookies = {
		get(name) {
			return store.get(name);
		},
		getAll() {
			return [...store.entries()].map(([name, value]) => ({ name, value }));
		},
		set(name, value) {
			store.set(name, value);
		},
		delete(name) {
			store.delete(name);
		},
		serialize() {
			return "";
		},
	};
	return cookie;
}

function payload(accessToken = "tok"): SessionPayload {
	return {
		user: {
			sub: "sub-1",
			username: "alice",
			name: "Alice",
			email: "a@b.c",
			firstName: "Alice",
			lastName: "Smith",
		},
		tokens: { accessToken, refreshToken: "rt", idToken: "id", expiresAt: Date.now() + 60_000 },
	};
}

describe("session cookie", () => {
	it("round-trips a session through write/read", () => {
		const cookies = fakeCookies();
		writeSession(cookies, payload("abc"), false);
		expect(readSession(cookies)?.tokens.accessToken).toBe("abc");
		expect(readSession(cookies)?.user.username).toBe("alice");
	});

	it("survives chunking for large token payloads", () => {
		const cookies = fakeCookies();
		// Simulate a fat Keycloak access token (~5KB) to force multiple chunks.
		const big = "x".repeat(5_000);
		writeSession(cookies, payload(big), false);

		// More than the bare `sid` cookie should exist (chunked).
		const names = [...(cookies.getAll() as { name: string }[])].map((c) => c.name);
		expect(names.filter((n) => n === "sid" || n.startsWith("sid.")).length).toBeGreaterThan(1);

		expect(readSession(cookies)?.tokens.accessToken).toBe(big);
	});

	it("shrinks chunk count when rewritten smaller", () => {
		const cookies = fakeCookies();
		writeSession(cookies, payload("x".repeat(5_000)), false);
		writeSession(cookies, payload("tiny"), false);

		const names = (cookies.getAll() as { name: string }[]).map((c) => c.name).filter((n) => n === "sid" || n.startsWith("sid."));
		// Leftover higher-index chunks from the larger value must be gone.
		expect(names).toEqual(["sid"]);
		expect(readSession(cookies)?.tokens.accessToken).toBe("tiny");
	});

	it("fails closed when a chunk is tampered with", () => {
		const cookies = fakeCookies();
		writeSession(cookies, payload(), false);
		// Corrupt the ciphertext.
		cookies.set("sid", cookies.get("sid")!.slice(0, -2) + "ZZ", { path: "/" });
		expect(readSession(cookies)).toBeUndefined();
	});

	it("clearSession removes every chunk", () => {
		const cookies = fakeCookies();
		writeSession(cookies, payload("x".repeat(5_000)), false);
		clearSession(cookies);
		expect(readSession(cookies)).toBeUndefined();
	});

	it("pending login is consumed exactly once", () => {
		const cookies = fakeCookies();
		setPendingLogin(cookies, { state: "s", nonce: "n", verifier: "v", next: "/x" }, false);
		expect(consumePendingLogin(cookies)).toEqual({ state: "s", nonce: "n", verifier: "v", next: "/x" });
		// Cookie is cleared after consumption.
		expect(consumePendingLogin(cookies)).toBeUndefined();
	});
});
