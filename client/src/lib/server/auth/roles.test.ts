import { beforeEach, describe, expect, it, vi } from "vitest";
import type { Cookies } from "@sveltejs/kit";
import { readSession, writeSession } from "./session";
import type { SessionPayload } from "./session";
import { getFacilityRoles } from "./roles";
import { accountRole } from "$lib/server/api/generated/account";

vi.mock("$app/env/private", () => ({
	APP_URL: "http://localhost:5173",
	IDENTITY_URL: "http://localhost:8088/realms/kayord",
	API_URL: "http://localhost:5000",
	SESSION_SECRET: "",
}));

vi.mock("$lib/server/api/generated/account", () => ({
	accountRole: vi.fn(),
}));

const accountRoleMock = vi.mocked(accountRole);

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

function session(): SessionPayload {
	return {
		user: {
			sub: "sub-1",
			username: "alice",
			name: "Alice",
			email: "a@b.c",
			firstName: "Alice",
			lastName: "Smith",
		},
		tokens: { accessToken: "tok", refreshToken: "rt", idToken: "id", expiresAt: Date.now() + 60_000 },
	};
}

describe("getFacilityRoles", () => {
	beforeEach(() => {
		accountRoleMock.mockReset();
	});

	it("returns [] without a session and never calls the API", async () => {
		expect(await getFacilityRoles(fakeCookies(), 1)).toEqual([]);
		expect(accountRoleMock).not.toHaveBeenCalled();
	});

	it("fetches on first call and serves subsequent calls from the session cache", async () => {
		accountRoleMock.mockResolvedValue([{ facilityId: 1, normalizedName: "MANAGER" }]);
		const cookies = fakeCookies();
		writeSession(cookies, session(), false);

		expect(await getFacilityRoles(cookies, 1)).toEqual(["MANAGER"]);
		expect(await getFacilityRoles(cookies, 1)).toEqual(["MANAGER"]);
		expect(accountRoleMock).toHaveBeenCalledTimes(1);
	});

	it("re-fetches once the cached entry expires", async () => {
		accountRoleMock.mockResolvedValue([{ facilityId: 1, normalizedName: "MANAGER" }]);
		const cookies = fakeCookies();
		writeSession(cookies, session(), false);

		await getFacilityRoles(cookies, 1);

		// Expire the entry, then rewrite the session so the helper sees it as stale.
		const cached = readSession(cookies)!;
		cached.rolesCache!["1"].expiresAt = Date.now() - 1;
		writeSession(cookies, cached, false);

		expect(await getFacilityRoles(cookies, 1)).toEqual(["MANAGER"]);
		expect(accountRoleMock).toHaveBeenCalledTimes(2);
	});

	it("caches per facility", async () => {
		accountRoleMock.mockImplementation(async (facilityId: number) => (facilityId === 1 ? [{ facilityId: 1, normalizedName: "MANAGER" }] : []));
		const cookies = fakeCookies();
		writeSession(cookies, session(), false);

		expect(await getFacilityRoles(cookies, 1)).toEqual(["MANAGER"]);
		expect(await getFacilityRoles(cookies, 2)).toEqual([]);
		expect(await getFacilityRoles(cookies, 1)).toEqual(["MANAGER"]);
		expect(accountRoleMock).toHaveBeenCalledTimes(2);
	});

	it("caches an API failure so it does not hammer the endpoint", async () => {
		accountRoleMock.mockRejectedValue(new Error("401"));
		const cookies = fakeCookies();
		writeSession(cookies, session(), false);

		expect(await getFacilityRoles(cookies, 1)).toEqual([]);
		expect(await getFacilityRoles(cookies, 1)).toEqual([]);
		expect(accountRoleMock).toHaveBeenCalledTimes(1);
	});
});
