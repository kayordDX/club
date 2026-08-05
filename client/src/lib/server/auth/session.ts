// In-memory session store — POC ONLY.
//
// Limitations: does not survive server restarts and is single-instance.
// For production, back this with Redis (already in the Aspire stack) keyed by
// the session id cookie. The API surface here maps 1:1 onto a Redis impl.
import type { OidcProfile, OidcTokens } from "./oidc";
import { randomToken } from "./oidc";
import type { SessionUser } from "$lib/types";

export interface Session {
	id: string;
	user: SessionUser;
	tokens: OidcTokens;
}

interface PendingLogin {
	verifier: string;
	next: string;
	createdAt: number;
}

const sessions = new Map<string, Session>();
const pending = new Map<string, PendingLogin>();

export function toSessionUser(p: OidcProfile): SessionUser {
	return {
		sub: p.sub,
		username: p.preferredUsername,
		email: p.email,
		firstName: p.givenName,
		lastName: p.familyName,
		picture: p.picture,
	};
}

export function createPendingLogin(verifier: string, next: string): string {
	const state = randomToken();
	pending.set(state, { verifier, next, createdAt: Date.now() });
	return state;
}

export function consumePendingLogin(state: string): PendingLogin | undefined {
	const entry = pending.get(state);
	if (entry) pending.delete(state);
	return entry;
}

export function createSession(profile: OidcProfile, tokens: OidcTokens): Session {
	const id = randomToken(32);
	const session: Session = { id, user: toSessionUser(profile), tokens };
	sessions.set(id, session);
	return session;
}

export function getSession(id: string): Session | undefined {
	return sessions.get(id);
}

export function deleteSession(id: string): void {
	sessions.delete(id);
}

// Drop stale pending logins (>10 min) so memory doesn't grow unbounded.
setInterval(() => {
	const cutoff = Date.now() - 10 * 60 * 1000;
	for (const [k, v] of pending) if (v.createdAt < cutoff) pending.delete(k);
}, 60_000).unref();
