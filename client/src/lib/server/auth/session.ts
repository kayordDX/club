// Stateless, encrypted-cookie session for the BFF.
//
// Why no server-side store? With Keycloak as the source of truth, the access +
// refresh tokens it issues ARE the session. Carrying them in an HttpOnly,
// encrypted, chunked cookie means:
//   • the hooks hot path is pure CPU (decrypt + compare) — no Redis/DB round-trip,
//     works across multiple instances with zero shared state, survives restarts;
//   • logout is instant (delete the cookie), no store to invalidate.
//
// The opaque `sid` cookie + in-memory/Redis map approach needs an I/O hop on
// every request; this avoids it. The refresh token never reaches the browser
// (only the encrypted, HttpOnly cookie does), so it stays server-side only.
import { SESSION_SECRET } from "$app/env/private";
import { createCipheriv, createDecipheriv, createHash, randomBytes } from "node:crypto";
import type { Cookies } from "@sveltejs/kit";
import type { SessionUser } from "$lib/types";
import type { OidcProfile, OidcTokens } from "./oidc";
import { PKCE_COOKIE, SESSION_COOKIE } from "./oidc";

export interface SessionPayload {
	user: SessionUser;
	tokens: OidcTokens;
}

/** Carried in a short-lived cookie between the authorize redirect and callback. */
export interface PendingLogin {
	state: string;
	nonce: string;
	verifier: string;
	next: string;
}

// Browsers cap one cookie at ~4KB; the token payload (Keycloak JWTs are large)
// is split into chunks and reassembled on read.
const CHUNK_SIZE = 3500;
const MAX_CHUNKS = 6;

const SESSION_MAX_AGE = 60 * 60 * 24 * 7; // 7 days, rolled forward on refresh
const PENDING_MAX_AGE = 10 * 60; // 10 minutes for the redirect dance

export interface CookieOptions {
	path: "/";
	httpOnly: true;
	sameSite: "lax";
	secure: boolean;
	maxAge: number;
}

export function toSessionUser(p: OidcProfile): SessionUser {
	return {
		sub: p.sub,
		username: p.preferredUsername,
		name: p.name || [p.givenName, p.familyName].filter(Boolean).join(" ") || p.preferredUsername,
		email: p.email,
		firstName: p.givenName,
		lastName: p.familyName,
		phone_number: p.phone_number,
		phone_number_verified: p.phone_number_verified,
		email_verified: p.email_verified,
		picture: p.picture,
	};
}

// --- symmetric key ---------------------------------------------------------

let cachedKey: Buffer | undefined;
let ephemeralDevKey: string | undefined;

function getKey(): Buffer {
	if (cachedKey) return cachedKey;

	if (SESSION_SECRET) {
		cachedKey = createHash("sha256").update(SESSION_SECRET).digest();
		return cachedKey;
	}

	// Dev convenience: a random per-process key so things work with zero config.
	// Sessions won't survive a restart — that's fine locally.
	if (!ephemeralDevKey) {
		ephemeralDevKey = randomBytes(32).toString("base64");
		console.warn("[auth] SESSION_SECRET not set — using an ephemeral dev key. Sessions will not survive a restart.");
	}
	cachedKey = createHash("sha256").update(ephemeralDevKey).digest();
	return cachedKey;
}

// --- AEAD seal/unseal (AES-256-GCM) ---------------------------------------

function seal(plaintext: string): string {
	const key = getKey();
	const iv = randomBytes(12);
	const cipher = createCipheriv("aes-256-gcm", key, iv);
	const ciphertext = Buffer.concat([cipher.update(plaintext, "utf8"), cipher.final()]);
	const tag = cipher.getAuthTag();
	// base64url contains no ".", so this round-trips cleanly.
	return `${iv.toString("base64url")}.${ciphertext.toString("base64url")}.${tag.toString("base64url")}`;
}

function unseal(value: string): string | undefined {
	const parts = value.split(".");
	if (parts.length !== 3) return undefined;
	const [ivB, ctB, tagB] = parts;
	try {
		const decipher = createDecipheriv("aes-256-gcm", getKey(), Buffer.from(ivB, "base64url"));
		decipher.setAuthTag(Buffer.from(tagB, "base64url"));
		const plaintext = Buffer.concat([decipher.update(Buffer.from(ctB, "base64url")), decipher.final()]);
		return plaintext.toString("utf8");
	} catch {
		// Tampered, truncated, or sealed with a different key → treat as no session.
		return undefined;
	}
}

// --- chunked cookie helpers ------------------------------------------------

function chunkName(base: string, i: number): string {
	return i === 0 ? base : `${base}.${i}`;
}

function readChunked(cookies: Cookies, base: string): string | undefined {
	let value = "";
	for (let i = 0; i < MAX_CHUNKS; i++) {
		const part = cookies.get(chunkName(base, i));
		if (part === undefined) return i === 0 ? undefined : value; // no cookie, or ran out of chunks
		value += part;
	}
	return value;
}

function clearChunked(cookies: Cookies, base: string): void {
	for (let i = 0; i < MAX_CHUNKS; i++) {
		const name = chunkName(base, i);
		if (cookies.get(name) === undefined && i > 0) break;
		cookies.delete(name, { path: "/" });
	}
}

function writeChunked(cookies: Cookies, base: string, value: string, opts: CookieOptions): void {
	// Remove any higher-index chunks left over from a previously larger value.
	clearChunked(cookies, base);
	const chunks: string[] = [];
	for (let i = 0; i < value.length; i += CHUNK_SIZE) chunks.push(value.slice(i, i + CHUNK_SIZE));
	if (chunks.length > MAX_CHUNKS) {
		throw new Error(`session payload too large (${value.length} bytes, ${chunks.length} chunks)`);
	}
	for (let i = 0; i < chunks.length; i++) cookies.set(chunkName(base, i), chunks[i], opts);
}

// --- session API -----------------------------------------------------------

export function readSession(cookies: Cookies): SessionPayload | undefined {
	const sealed = readChunked(cookies, SESSION_COOKIE);
	if (!sealed) return undefined;
	const json = unseal(sealed);
	if (!json) return undefined;
	try {
		return JSON.parse(json) as SessionPayload;
	} catch {
		return undefined;
	}
}

export function writeSession(cookies: Cookies, payload: SessionPayload, secure: boolean): void {
	writeChunked(cookies, SESSION_COOKIE, seal(JSON.stringify(payload)), {
		path: "/",
		httpOnly: true,
		sameSite: "lax",
		secure,
		maxAge: SESSION_MAX_AGE,
	});
}

export function clearSession(cookies: Cookies): void {
	clearChunked(cookies, SESSION_COOKIE);
}

// --- pending login (PKCE verifier + nonce) ---------------------------------

export function setPendingLogin(cookies: Cookies, data: PendingLogin, secure: boolean): void {
	cookies.set(PKCE_COOKIE, seal(JSON.stringify(data)), {
		path: "/",
		httpOnly: true,
		sameSite: "lax",
		secure,
		maxAge: PENDING_MAX_AGE,
	});
}

/** Reads and clears the pending-login cookie in one step. */
export function consumePendingLogin(cookies: Cookies): PendingLogin | undefined {
	const sealed = cookies.get(PKCE_COOKIE);
	if (!sealed) return undefined;
	cookies.delete(PKCE_COOKIE, { path: "/" });
	const json = unseal(sealed);
	if (!json) return undefined;
	try {
		return JSON.parse(json) as PendingLogin;
	} catch {
		return undefined;
	}
}
