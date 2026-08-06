// Server-side OIDC client for the BFF pattern.
//
// Intentionally dependency-light: uses only `fetch` + `node:crypto` so there are
// no Vite/SSR bundling surprises in the POC. Swappable for `openid-client` later
// if you want hardened id_token signature verification.
//
// Reuses the existing Keycloak *public client* (`public-client`) with the
// authorization-code + PKCE flow — so NO Keycloak client changes or secret needed.
import { PUBLIC_APP_URL, PUBLIC_IDENTITY_URL } from "$env/static/public";
import { createHash, randomBytes } from "node:crypto";

const CLIENT_ID = "public-client";
const SCOPE = "openid profile email phone offline_access";
/** e.g. http://localhost:8088/realms/kayord */
const ISSUER = PUBLIC_IDENTITY_URL;
const REDIRECT_URI = `${PUBLIC_APP_URL}/auth/callback`;

export const SESSION_COOKIE = "sid";

export interface OidcTokens {
	accessToken: string;
	refreshToken?: string;
	idToken?: string;
	/** epoch ms when the access token expires */
	expiresAt: number;
}

export interface OidcProfile {
	sub: string;
	preferredUsername: string;
	name: string;
	email: string;
	givenName: string;
	familyName: string;
	phone_number?: string;
	picture?: string;
}

interface Discovery {
	authorization_endpoint: string;
	token_endpoint: string;
	userinfo_endpoint: string;
	end_session_endpoint: string;
}

let discoveryCache: Promise<Discovery> | undefined;

export async function discover(): Promise<Discovery> {
	if (!discoveryCache) {
		discoveryCache = fetch(`${ISSUER}/.well-known/openid-configuration`).then(async (r) => {
			if (!r.ok) throw new Error(`OIDC discovery failed: ${r.status} ${ISSUER}`);
			return (await r.json()) as Discovery;
		});
		// If discovery fails, clear the cache so the next call retries.
		discoveryCache.catch(() => {
			discoveryCache = undefined;
		});
	}
	return discoveryCache;
}

function base64url(input: Buffer | string): string {
	return Buffer.from(input).toString("base64url");
}

export function createPkce(): { verifier: string; challenge: string } {
	const verifier = base64url(randomBytes(32));
	const challenge = base64url(createHash("sha256").update(verifier).digest());
	return { verifier, challenge };
}

export function randomToken(bytes = 24): string {
	return randomBytes(bytes).toString("base64url");
}

export async function getAuthorizationUrl(state: string, challenge: string): Promise<string> {
	const d = await discover();
	const params = new URLSearchParams({
		response_type: "code",
		client_id: CLIENT_ID,
		redirect_uri: REDIRECT_URI,
		scope: SCOPE,
		state,
		code_challenge: challenge,
		code_challenge_method: "S256",
		prompt: "select_account",
	});
	return `${d.authorization_endpoint}?${params.toString()}`;
}

export async function exchangeCode(code: string, verifier: string): Promise<OidcTokens> {
	const d = await discover();
	const body = new URLSearchParams({
		grant_type: "authorization_code",
		code,
		redirect_uri: REDIRECT_URI,
		client_id: CLIENT_ID,
		code_verifier: verifier,
	});
	const res = await fetch(d.token_endpoint, {
		method: "POST",
		headers: { "content-type": "application/x-www-form-urlencoded" },
		body,
	});
	if (!res.ok) throw new Error(`token exchange failed: ${res.status} ${await res.text()}`);
	const tok = (await res.json()) as {
		access_token: string;
		refresh_token?: string;
		id_token?: string;
		expires_in: number;
	};
	return {
		accessToken: tok.access_token,
		refreshToken: tok.refresh_token,
		idToken: tok.id_token,
		expiresAt: Date.now() + tok.expires_in * 1000,
	};
}

export async function refreshTokens(refreshToken: string): Promise<OidcTokens> {
	const d = await discover();
	const body = new URLSearchParams({
		grant_type: "refresh_token",
		refresh_token: refreshToken,
		client_id: CLIENT_ID,
		scope: SCOPE,
	});
	const res = await fetch(d.token_endpoint, {
		method: "POST",
		headers: { "content-type": "application/x-www-form-urlencoded" },
		body,
	});
	if (!res.ok) throw new Error(`token refresh failed: ${res.status}`);
	const tok = (await res.json()) as {
		access_token: string;
		refresh_token?: string;
		id_token?: string;
		expires_in: number;
	};
	return {
		accessToken: tok.access_token,
		// Keycloak may rotate the refresh token; fall back to the old one.
		refreshToken: tok.refresh_token ?? refreshToken,
		idToken: tok.id_token,
		expiresAt: Date.now() + tok.expires_in * 1000,
	};
}

export async function getUserinfo(accessToken: string): Promise<OidcProfile> {
	const d = await discover();
	const res = await fetch(d.userinfo_endpoint, {
		headers: { authorization: `Bearer ${accessToken}` },
	});
	if (!res.ok) throw new Error(`userinfo failed: ${res.status}`);
	const u = (await res.json()) as Record<string, unknown>;
	return {
		sub: String(u.sub ?? ""),
		preferredUsername: String(u.preferred_username ?? ""),
		name: String(u.name ?? ""),
		email: String(u.email ?? ""),
		givenName: String(u.given_name ?? ""),
		familyName: String(u.family_name ?? ""),
		phone_number: u.phone_number ? String(u.phone_number) : undefined,
		picture: u.picture ? String(u.picture) : undefined,
	};
}

export async function getEndSessionUrl(): Promise<string> {
	const d = await discover();
	const params = new URLSearchParams({
		client_id: CLIENT_ID,
		post_logout_redirect_uri: `${PUBLIC_APP_URL}/`,
	});
	return `${d.end_session_endpoint}?${params.toString()}`;
}
