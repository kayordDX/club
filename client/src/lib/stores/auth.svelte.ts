import { UserManager, type UserManagerSettings, User } from "oidc-client-ts";
import { type UserRoleBasicDTO } from "$lib/api";
import { PUBLIC_IDENTITY_URL, PUBLIC_APP_URL, PUBLIC_API_URL } from "$env/static/public";

export type KeycloakAction =
	| "UPDATE_PASSWORD"
	| "CONFIGURE_TOTP"
	| "UPDATE_PROFILE"
	| "UPDATE_EMAIL"
	| "VERIFY_EMAIL"
	| "TERMS_AND_CONDITIONS"
	| "delete_account";

class Auth {
	private userManager: UserManager;

	#user = $state<User | null>(null);
	#roles = $state<Array<UserRoleBasicDTO> | null>([]);
	#isLoading = $state(true);
	#isSyncing = $state(false);

	constructor(settings: UserManagerSettings) {
		this.userManager = new UserManager(settings);
		this.init();
	}

	private async init() {
		try {
			if (!this.userManager) return;
			const user = await this.userManager.getUser();
			this.#user = user;
			this.setupEventListeners();
			this.getRoles();
		} catch (error) {
			console.error("OIDC Initialization failed:", error);
		} finally {
			this.#isLoading = false;
		}
	}

	public async syncUser(force = false) {
		if (!this.accessToken || this.#isSyncing) return;

		this.#isSyncing = true;
		try {
			const response = await fetch(`${PUBLIC_API_URL}/account/sync`, {
				method: "POST",
				headers: {
					Authorization: `Bearer ${this.accessToken}`,
					"Content-Type": "application/json",
				},
				body: JSON.stringify({ force }),
			});

			if (!response.ok) {
				console.error("User sync failed", await response.text());
			}
		} catch (err) {
			console.error("Network error during JIT sync:", err);
		} finally {
			this.#isSyncing = false;
		}
	}

	private async getRoles() {
		try {
			const response = await fetch(`${PUBLIC_API_URL}/account/me`, {
				method: "GET",
				headers: {
					Authorization: `Bearer ${this.accessToken}`,
				},
			});
			if (response.ok) {
				const res = (await response.json()) as Array<UserRoleBasicDTO>;
				this.#roles = res;
			} else {
				console.error("Get Roles failed", await response.text());
			}
		} catch (err) {
			console.error("Network error during getRoles:", err);
		} finally {
			this.#isSyncing = false;
		}
	}

	private setupEventListeners() {
		this.userManager.events.addUserLoaded(async (user) => {
			this.#user = user;
			await this.syncUser();
		});

		this.userManager.events.addUserUnloaded(() => {
			this.#user = null;
			this.#roles = null;
		});

		this.userManager.events.addAccessTokenExpiring(() => {
			console.log("Token expiring soon...");
		});

		this.userManager.events.addAccessTokenExpired(() => {
			this.#user = null;
		});

		this.userManager.events.addSilentRenewError((error) => {
			console.error("Silent renew error:", error);
			// this.logout(); // Optional: force logout on renewal failure
		});
	}

	get user() {
		return this.#user;
	}
	get isLoading() {
		return this.#isLoading;
	}
	get isAuthenticated() {
		return !!this.#user && !this.#user.expired;
	}
	get accessToken() {
		return this.#user?.access_token;
	}
	get roles() {
		return this.#roles;
	}

	login = async () => {
		await this.userManager.signinRedirect({
			prompt: "select_account",
		});
	};

	logout = async () => {
		console.log("logging out", this.userManager);
		await this.userManager.signoutRedirect();
	};

	keycloakAction = async (action: KeycloakAction) => {
		try {
			await this.userManager.signinRedirect({
				extraQueryParams: { kc_action: action },
			});
		} catch (error) {
			console.error(`Failed to redirect for action: ${action}`, error);
			throw error;
		}
	};

	otpLevel = async () => {
		await this.userManager.signinRedirect({
			extraQueryParams: {
				prompt: "login", // Override global select_account; force full re-auth
			},
			state: { returnUrl: window.location.pathname },
		});
	};

	async handleCallback() {
		try {
			const user = await this.userManager.signinCallback();
			if (user == undefined) {
				throw new Error("No user returned from callback");
			}
			this.#user = user;
			return user;
		} catch (error) {
			console.error("Callback error:", error);
			throw error;
		}
	}
}

export const auth = new Auth({
	authority: PUBLIC_IDENTITY_URL,
	client_id: "public-client",
	redirect_uri: `${PUBLIC_APP_URL}/callback`,
	silent_redirect_uri: `${PUBLIC_APP_URL}/callback`,
	post_logout_redirect_uri: `${PUBLIC_APP_URL}/`,
	response_type: "code",
	scope: "openid profile email phone offline_access",
	automaticSilentRenew: true,
});
