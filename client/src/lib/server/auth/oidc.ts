// Server-side OIDC built on the hardened `openid-client` library (BFF pattern).
//
// Reuses the existing Keycloak *public client* (`public-client`) with the
// authorization-code + PKCE flow — NO Keycloak changes and NO client secret.
//
// What `openid-client` gives us over hand-rolling:
//   • id_token signature verification against the issuer JWKS (cached)
//   • issuer / audience / expiry / nonce checks on the token response
//   • `iss` state-binding check on the authorization response
//   • PKCE + standard grant handling, clock-skew tolerance
//
// It still talks to the exact same Keycloak endpoints as before; we just stop
// trusting the responses and start validating them.
import * as oidc from "openid-client";
import { APP_URL, IDENTITY_URL } from "$app/env/private";
import { createHash, randomBytes } from "node:crypto";

const CLIENT_ID = "public-client";
const SCOPE = "openid profile email phone offline_access";
/** e.g. http://localhost:8088/realms/kayord */
const ISSUER = IDENTITY_URL;
const REDIRECT_URI = `${APP_URL}/auth/callback`;

export const SESSION_COOKIE = "sid";
/** Short-lived cookie holding the PKCE verifier + nonce between redirect & callback. */
export const PKCE_COOKIE = "pkce";

const CLIENT_METADATA: Partial<oidc.ClientMetadata> = {
	// Public client: authenticate at the token endpoint with `client_id` only.
	token_endpoint_auth_method: "none",
};

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
	phone_number_verified?: boolean;
	email_verified?: boolean;
	picture?: string;
}

// Discovery (+ JWKS) is resolved once and reused for the process lifetime. The
// promise is cached so concurrent startup requests share a single round-trip,
// and dropped on failure so the next call retries.
let configPromise: Promise<oidc.Configuration> | undefined;

export function getConfig(): Promise<oidc.Configuration> {
	if (!configPromise) {
		configPromise = initConfig();
		configPromise.catch(() => {
			configPromise = undefined;
		});
	}
	return configPromise;
}

async function initConfig(): Promise<oidc.Configuration> {
	const server = new URL(ISSUER);
	const config = await oidc.discovery(server, CLIENT_ID, CLIENT_METADATA, oidc.None());
	// openid-client refuses non-loopback http issuers unless explicitly allowed.
	// Dev points at http://localhost:8088; production is https.
	if (server.protocol !== "https:") oidc.allowInsecureRequests(config);
	return config;
}

function base64url(input: Buffer | string): string {
	return Buffer.from(input).toString("base64url");
}

export function createPkce(): { verifier: string; challenge: string } {
	const verifier = base64url(randomBytes(32));
	const challenge = base64url(createHash("sha256").update(verifier).digest());
	return { verifier, challenge };
}

export const randomState = oidc.randomState;
export const randomNonce = oidc.randomNonce;

export async function getAuthorizationUrl(state: string, challenge: string, opts: { nonce?: string; kcAction?: string | null } = {}): Promise<string> {
	const config = await getConfig();
	const params: Record<string, string> = {
		response_type: "code",
		client_id: CLIENT_ID,
		redirect_uri: REDIRECT_URI,
		scope: SCOPE,
		state,
		code_challenge: challenge,
		code_challenge_method: "S256",
		prompt: "select_account",
	};
	if (opts.nonce) params.nonce = opts.nonce;
	if (opts.kcAction) params.kc_action = opts.kcAction;
	return oidc.buildAuthorizationUrl(config, params).href;
}

export async function exchangeCode(
	callbackUrl: string | URL,
	checks: { expectedState: string; pkceCodeVerifier: string; expectedNonce?: string }
): Promise<{ tokens: OidcTokens; subject: string; claims: Record<string, unknown> }> {
	const config = await getConfig();
	const currentUrl = callbackUrl instanceof URL ? callbackUrl : new URL(callbackUrl, APP_URL);
	const res = await oidc.authorizationCodeGrant(config, currentUrl, {
		expectedState: checks.expectedState,
		pkceCodeVerifier: checks.pkceCodeVerifier,
		...(checks.expectedNonce ? { expectedNonce: checks.expectedNonce } : {}),
	});
	const expiresIn = res.expiresIn();
	const claims = (res.claims() ?? {}) as Record<string, unknown>;
	return {
		tokens: {
			accessToken: res.access_token,
			refreshToken: res.refresh_token,
			idToken: res.id_token,
			expiresAt: Date.now() + (expiresIn ?? 0) * 1000,
		},
		subject: String(claims.sub ?? ""),
		claims,
	};
}

export async function refreshTokens(refreshToken: string): Promise<OidcTokens> {
	const config = await getConfig();
	const res = await oidc.refreshTokenGrant(config, refreshToken);
	const expiresIn = res.expiresIn();
	return {
		accessToken: res.access_token,
		// Keycloak may rotate the refresh token; fall back to the previous one.
		refreshToken: res.refresh_token ?? refreshToken,
		idToken: res.id_token,
		expiresAt: Date.now() + (expiresIn ?? 0) * 1000,
	};
}

export async function getUserinfo(accessToken: string, expectedSubject: string): Promise<OidcProfile> {
	const config = await getConfig();
	const u = (await oidc.fetchUserInfo(config, accessToken, expectedSubject)) as Record<string, unknown>;
	return mapProfile(u);
}

/** Build a profile from verified id_token claims when userinfo can't be reached. */
export function profileFromClaims(claims: Record<string, unknown>): OidcProfile {
	return mapProfile(claims);
}

function mapProfile(u: Record<string, unknown>): OidcProfile {
	return {
		sub: String(u.sub ?? ""),
		preferredUsername: String(u.preferred_username ?? ""),
		name: String(u.name ?? ""),
		email: String(u.email ?? ""),
		givenName: String(u.given_name ?? ""),
		familyName: String(u.family_name ?? ""),
		phone_number: u.phone_number ? String(u.phone_number) : undefined,
		phone_number_verified: u.phone_number_verified === true,
		email_verified: u.email_verified === true,
		picture: u.picture ? String(u.picture) : undefined,
	};
}

export async function getEndSessionUrl(idTokenHint?: string): Promise<string> {
	const config = await getConfig();
	const params: Record<string, string> = {
		client_id: CLIENT_ID,
		post_logout_redirect_uri: `${APP_URL}/`,
	};
	if (idTokenHint) params.id_token_hint = idTokenHint;
	return oidc.buildEndSessionUrl(config, params).href;
}
