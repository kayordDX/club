import { afterEach, expect, it, vi } from "vitest";
import type { RequestEvent } from "@sveltejs/kit";
import { handleError } from "./hooks.server";

vi.mock("$app/env/private", () => ({ APP_URL: "http://localhost:5173" }));
vi.mock("#lib/server/auth/oidc.js", () => ({ refreshTokens: vi.fn() }));
vi.mock("#lib/server/auth/session.js", () => ({ clearSession: vi.fn(), readSession: vi.fn(), writeSession: vi.fn() }));

afterEach(() => vi.restoreAllMocks());
const event = {} as RequestEvent;

it("keeps expected app errors unchanged without reporting them as unhandled", () => {
	const log = vi.spyOn(console, "error").mockImplementation(() => {});
	expect(handleError({ kind: "app", error: { status: 403, message: "Forbidden" }, event })).toBeUndefined();
	expect(log).not.toHaveBeenCalled();
});

it("reports unknown errors without exposing their details to clients", () => {
	const log = vi.spyOn(console, "error").mockImplementation(() => {});
	const error = new Error("private details");
	expect(handleError({ kind: "unknown", error, event })).toBeUndefined();
	expect(log).toHaveBeenCalledExactlyOnceWith(error);
});
